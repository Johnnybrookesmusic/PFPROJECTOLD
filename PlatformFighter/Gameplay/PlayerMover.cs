using PlatformFighter.Characters;
using PlatformFighter.Characters.Fox;
using PlatformFighter.Core.Combat;
using PlatformFighter.Core.Input;
using PlatformFighter.Core.Math;
using PlatformFighter.Core.Sim;
using PlatformFighter.Core.Sim.Collision;

namespace PlatformFighter.Gameplay;

/// <summary>Ground-only sub-state; airborne is tracked separately via <see cref="PlayerMover.Grounded"/>.</summary>
public enum GroundState : int
{
    Idle = 0,
    Walk = 1,
    Dash = 2,
    Run = 3,
    /// <summary>Step 3: MeleeLight's TILTTURN — a slow turnaround from a gentle
    /// stick flick. Faces the new direction on frame 6, brakes with double
    /// traction meanwhile.</summary>
    TiltTurn = 4,
    /// <summary>Step 3: MeleeLight's SMASHTURN — the hard flick that produces a
    /// pivot. Faces immediately, brakes with double traction. This is the state
    /// dash-dancing bounces through.</summary>
    SmashTurn = 5,
}

/// <summary>
/// Phase 6: the player's externally-visible action state — what Phase 8
/// (Animation) will key its clip lookup off, and what Phase 7 (Combat)
/// will gate move availability on. Deliberately a DERIVED view over
/// PlayerMover's movement fields (Grounded, Ground, JumpSquatFramesRemaining,
/// FastFalling, Velocity), not a second source of truth — movement logic
/// stays exactly where it was in Phase 5, this just names the result.
///
/// Phase 9 adds <see cref="Attack"/>: a move is active (see CurrentMove);
/// derived the same way as everything else here, never assigned directly.
/// </summary>
public enum PlayerActionState : int
{
    Idle = 0,
    Walk = 1,
    Dash = 2,
    Run = 3,
    JumpSquat = 4,
    Jump = 5,   // airborne, rising (Velocity.Y < 0) or apex
    Fall = 6,   // airborne, falling, not fast-falling
    FastFall = 7,
    Landing = 8,
    Hitstun = 9,
    Attack = 10,
    /// <summary>Directive Phase 1: airborne-and-invincible window right after a
    /// blast-zone death teleports the fighter back to their respawn point — see
    /// PlayerMover.RespawnInvincibilityFramesRemaining. Real Melee's "star
    /// platform" hold/steer/drop sequence is NOT modeled (documented
    /// simplification, see PlayerMover's respawn-handling doc comment) — this
    /// engine drops the fighter with full normal airborne control immediately,
    /// just briefly unhittable.</summary>
    Respawning = 11,
    /// <summary>Directive Phase 1: out of stocks. Tick() returns immediately for
    /// an eliminated fighter (see its own doc comment) — no physics, no input,
    /// not hittable, doesn't move. Terminal; nothing clears this state.</summary>
    Eliminated = 12,
}

/// <summary>
/// Phase 5: a real player body. Dash/run ground movement, air control,
/// fast-fall, and jump-squat-gated short-hop/full-hop, all layered on top
/// of Phase 4's <see cref="CollisionResolver"/> — the same grounded/
/// platform-drop-through primitives <c>Debug/TestBody.cs</c> exercised,
/// now driving an actual movement state machine instead of falling
/// straight down forever.
///
/// Phase 6 adds <see cref="CurrentState"/>/<see cref="StateFrame"/>: a
/// named, derived <see cref="PlayerActionState"/> computed once per tick
/// from this class's own movement fields (see DeriveActionState).
///
/// Phase 7 adds <see cref="Percent"/>/<see cref="HitstunFramesRemaining"/>
/// and <see cref="ApplyHit"/>: getting hit interrupts whatever the mover
/// was doing (including mid-dash, mid-jump-squat, or mid-attack), overrides
/// Velocity with a knockback launch, and forces a fixed window of input-less
/// airborne physics (<c>TickHitstun</c>) before normal control returns.
///
/// Phase 9 adds real per-character data (<see cref="CharacterData"/>,
/// replacing the old shared <c>MovementConstants</c> — see that file's own
/// doc comment predicting exactly this) and a live attack dispatch:
/// <see cref="CurrentMove"/>/<see cref="MoveFrame"/>, hitbox spawning via
/// <see cref="TryGetActiveHitbox"/> (consumed by <c>Gameplay/CombatSystem.cs</c>),
/// and hitlag (<see cref="HitlagFramesRemaining"/>, both fighters freeze on a
/// connected hit). See Docs/COMBAT.md and Characters/Hybrid/FoxFalcoHybrid.cs
/// for the specific moveset and known simplifications (single representative
/// hit per move, hurtbox-overlap hit detection instead of real spatial
/// hitboxes, grounded attacks lock horizontal velocity, no L-cancel timing).
/// </summary>
public sealed class PlayerMover : ISimObject
{
    public const int TypeIdValue = 3;
    public int TypeId => TypeIdValue;

    public FxVec2 Position;
    public FxVec2 PreviousPosition;
    public FxVec2 Velocity;
    public bool Grounded;

    /// <summary>Step 2: WHICH surface this fighter is standing on. Must survive
    /// save/load and must be round-tripped into the next resolve — losing it turns
    /// "walk along this ledge" into "re-search the stage and maybe pick a different
    /// surface", i.e. fighters teleporting between platforms. See
    /// SegmentCollisionResolver.cs.</summary>
    public SurfaceKind Surface;
    public int SurfaceIndex = -1;

    public bool FacingRight = true;
    public bool FastFalling;

    public GroundState Ground;
    public int DashFramesRemaining;

    /// <summary>Step 3: MeleeLight's per-action-state <c>timer</c>, counting UP from
    /// 1 on the first frame of DASH/TILTTURN/SMASHTURN. Every dash decision in
    /// DASH.js is expressed against this (frame 2 impulse, &gt;4 smash-turn,
    /// &gt;dashFrameMin run, &gt;dashFrameMax re-dash), so counting up and comparing
    /// to the real thresholds is a direct transcription rather than a
    /// re-derivation of the old count-down DashFramesRemaining.</summary>
    public int GroundTimer;
    public int JumpSquatFramesRemaining;
    public int JumpsRemaining;

    /// <summary>Phase 6: derived once per tick at the end of Tick(), after movement
    /// and collision are resolved. Never assigned anywhere else — see DeriveActionState.</summary>
    public PlayerActionState CurrentState { get; private set; } = PlayerActionState.Idle;
    /// <summary>Consecutive ticks CurrentState has held. Resets to 0 the tick it changes.
    /// Phase 8 (Animation) will use this to pick a clip frame; nothing reads it yet.</summary>
    public int StateFrame { get; private set; }
    private int _landingFramesRemaining;

    /// <summary>Phase 7: damage percent. Only ApplyHit increases it; nothing decreases
    /// it yet (no stocks/respawn system — that's Phase 14+).</summary>
    public Fx Percent { get; private set; } = Fx.Zero;
    public int HitstunFramesRemaining { get; private set; }

    /// <summary>Phase 9: which move slot is currently executing, if any. Null means
    /// "free to act" (subject to the usual grounded/airborne/jump-squat/hitstun gates).</summary>
    public MoveSlot? CurrentMove { get; private set; }
    /// <summary>1-indexed action frame of CurrentMove — matches MoveDef's own
    /// fightcore.gg-sourced frame numbering. 0 when CurrentMove is null.</summary>
    public int MoveFrame { get; private set; }
    private bool _hitApplied;

    /// <summary>Phase 11: current self-launch velocity for a move with
    /// MoveDef.LaunchSpeed > 0 (Up-B/Side-B). Set once in TryStartAttack (aim is
    /// locked in at button-press, matching this engine's existing "no live
    /// re-aim" simplification — see FoxMoves.cs's Fire Fox doc comment), then
    /// decayed toward zero each tick in TickAttacking. Zero/unused for every
    /// other move.</summary>
    private FxVec2 _selfLaunchVelocity;

    /// <summary>Phase 9: both fighters freeze (no input, no movement, no gravity)
    /// for this many remaining ticks after a hit connects — see CombatSystem.cs.
    /// Checked first thing in Tick(), ahead of even hitstun.</summary>
    public int HitlagFramesRemaining { get; private set; }

    /// <summary>Directive Phase 1: MeleeLight/real Melee don't fix a single
    /// universal stock count (it's a match-setup option); 4 is competitive
    /// standard and used here as a documented default, not transcribed data.</summary>
    public const int StartingStocks = 4;
    /// <summary>Directive Phase 1: how long a just-respawned fighter can't be
    /// hit. Real Melee's invincibility duration is tied to the star-platform
    /// hold/steer sequence this engine doesn't model (see PlayerActionState.
    /// Respawning's doc comment) — 120 ticks (~2s at 60fps) is a documented
    /// placeholder standing in for that, not a transcribed number.</summary>
    public const int RespawnInvincibilityFrames = 120;

    public int Stocks { get; private set; } = StartingStocks;
    /// <summary>Out of stocks — terminal, see PlayerActionState.Eliminated.</summary>
    public bool IsEliminated { get; private set; }
    private int _respawnInvincibilityFramesRemaining;
    /// <summary>Consumed by CombatSystem to skip ApplyHit entirely for a
    /// just-respawned fighter.</summary>
    public bool IsInvincible => _respawnInvincibilityFramesRemaining > 0;

    private readonly int _playerIndex;
    private readonly StageGeometry _stage;
    private readonly CharacterData _character;
    private readonly SmashTiltClassifier _smashTilt = new();

    // Derived-history, not raw input — same rationale as SmashTiltClassifier
    // (Docs/INPUT.md): needed for edge detection, but not itself part of
    // the wire format, so it lives here rather than on FrameInput.
    private sbyte _prevStickX;
    /// <summary>Stick X TWO frames ago. MeleeLight's smash-turn / re-dash checks
    /// read <c>input[p][2].lsX</c> — two frames back, not one — so detecting a
    /// fresh flick needs this as well as <see cref="_prevStickX"/>. Without it a
    /// held stick reads as a new flick every frame and dash-dance is impossible.</summary>
    private sbyte _prevStickX2;
    private sbyte _prevStickY;
    private bool _prevJumpHeld;
    private bool _prevAttackHeld;
    private bool _prevSpecialHeld;
    private bool _jumpHeldDuringSquat;

    public PlayerMover(FxVec2 start, StageGeometry stage, int playerIndex = 0, CharacterData? character = null)
    {
        Position = PreviousPosition = start;
        _stage = stage;
        _playerIndex = playerIndex;
        _character = character ?? FoxCharacter.Instance;
        JumpsRemaining = _character.Physics.ExtraJumps;
    }

    /// <summary>Defender's weight for KnockbackMath — see ApplyHit's overload.</summary>
    public Fx Weight => _character.Physics.Weight;
    public FxVec2 HalfSize => _character.Physics.HalfSize;
    /// <summary>Step 2: the real collision shape. Position is the FEET.</summary>
    public Ecb Ecb => _character.Physics.Ecb;

    public void Tick(SimWorld world)
    {
        // Directive Phase 1: eliminated is terminal — no physics, no input,
        // not hittable (CombatSystem never sees an active hitbox because
        // TryGetActiveHitbox requires CurrentMove, which this never sets again).
        if (IsEliminated)
        {
            PreviousPosition = Position;
            return;
        }

        // Directive Phase 1: blast zone check runs before hitlag/hitstun/anything
        // else — going past the blast zone always wins, same as real Melee (you
        // can die out of hitstun, mid-attack, mid-hitlag). Checked against the
        // position this fighter actually ended LAST tick (this tick's movement
        // hasn't happened yet), which is what a "did I go past the blast zone"
        // check should compare — resolving on stale data would let one extra
        // tick of off-screen movement through.
        if (_stage.IsPastBlastZone(Position))
        {
            Stocks--;
            if (Stocks <= 0)
            {
                IsEliminated = true;
                CurrentState = PlayerActionState.Eliminated;
                StateFrame = 0;
                PreviousPosition = Position;
                return;
            }

            var respawn = _stage.RespawnPoints.Count > 0
                ? _stage.RespawnPoints[_playerIndex % _stage.RespawnPoints.Count]
                : new FacingPoint(FxVec2.Zero, 1);
            Position = PreviousPosition = respawn.Position;
            Velocity = FxVec2.Zero;
            FacingRight = respawn.Face > 0;
            Grounded = false;
            Surface = SurfaceKind.None;
            SurfaceIndex = -1;
            Percent = Fx.Zero;
            HitstunFramesRemaining = 0;
            CurrentMove = null;
            MoveFrame = 0;
            _hitApplied = false;
            HitlagFramesRemaining = 0;
            JumpSquatFramesRemaining = 0;
            DashFramesRemaining = 0;
            FastFalling = false;
            Ground = GroundState.Idle;
            JumpsRemaining = _character.Physics.ExtraJumps;
            _respawnInvincibilityFramesRemaining = RespawnInvincibilityFrames;
            CurrentState = PlayerActionState.Respawning;
            StateFrame = 0;
            return; // Directive Phase 1: respawn teleport is its own tick, same
                     // convention as ApplyHit fully owning the tick a hit lands.
        }

        if (_respawnInvincibilityFramesRemaining > 0) _respawnInvincibilityFramesRemaining--;

        if (HitlagFramesRemaining > 0)
        {
            // Frozen: no movement, no input processing, no gravity — both
            // fighters just sit for the computed hitlag window. PreviousPosition
            // tracks Position 1:1 so render interpolation doesn't jump once
            // control returns.
            HitlagFramesRemaining--;
            PreviousPosition = Position;
            return;
        }

        bool wasGrounded = Grounded;
        PreviousPosition = Position;

        FrameInput input = world.GetInput(_playerIndex);
        bool jumpHeld = (input.Buttons & (ButtonFlags.X | ButtonFlags.Y)) != 0;
        bool jumpPressed = jumpHeld && !_prevJumpHeld;
        bool attackHeld = (input.Buttons & ButtonFlags.A) != 0;
        bool attackPressed = attackHeld && !_prevAttackHeld;
        bool specialHeld = (input.Buttons & ButtonFlags.B) != 0;
        bool specialPressed = specialHeld && !_prevSpecialHeld;

        // See TestBody.cs: stick Y follows the INPUT convention (+up), the
        // opposite sign of FxVec2 position space (+Y down). Holding down
        // both fast-falls (if airborne and already falling) and triggers
        // one-way-platform drop-through — that's one real Melee input
        // gesture serving both purposes, not a coincidence.
        bool holdingDown = input.MainY < -InputDecode.StickDeadzoneUnits;

        if (HitstunFramesRemaining > 0)
        {
            TickHitstun();
        }
        else if (CurrentMove.HasValue)
        {
            TickAttacking(input);
        }
        else if (JumpSquatFramesRemaining > 0)
        {
            TickJumpSquat(jumpHeld);
        }
        else if (Grounded)
        {
            if (TryStartAttack(input, attackPressed, specialPressed, grounded: true))
                TickAttacking(input);
            else
                TickGrounded(input, jumpPressed);
        }
        else
        {
            if (TryStartAttack(input, attackPressed, specialPressed, grounded: false))
                TickAttacking(input);
            else
                TickAirborne(input, jumpPressed, holdingDown);
        }

        // Step 2: real segment collision. Position is the FEET origin (see
        // Core/Sim/Collision/Ecb.cs) and Surface/SurfaceIndex are round-tripped
        // so a grounded fighter follows the surface it is actually standing on
        // rather than re-searching the stage every tick.
        FxVec2 movedPosition = Position + Velocity;
        var result = SegmentCollisionResolver.Resolve(
            PreviousPosition, movedPosition, Velocity, _character.Physics.Ecb, _stage,
            wasGrounded, Surface, SurfaceIndex, holdingDown);

        Position = result.Position;
        Velocity = result.Velocity;
        bool justLanded = result.Grounded && !wasGrounded;
        Grounded = result.Grounded;
        Surface = result.Surface;
        SurfaceIndex = result.SurfaceIndex;

        if (justLanded)
        {
            JumpsRemaining = _character.Physics.ExtraJumps;
            FastFalling = false;
            Ground = GroundState.Idle;
            DashFramesRemaining = 0;

            // Phase 9: landing mid-aerial-attack uses that move's own landing
            // lag instead of the default LandingFrames, and cancels the move
            // (no true auto-cancel-window/L-cancel timing yet — see the
            // class-level doc comment's list of known simplifications).
            if (CurrentMove.HasValue && _character.TryGetMove(CurrentMove.Value, out MoveDef landedMove)
                && landedMove.Category == MoveCategory.Aerial)
            {
                _landingFramesRemaining = landedMove.LandingLagFrames;
                CurrentMove = null;
                MoveFrame = 0;
            }
            else
            {
                _landingFramesRemaining = _character.Physics.LandingFrames;
            }
        }
        else if (Grounded && _landingFramesRemaining > 0)
        {
            _landingFramesRemaining--;
        }

        // Step 3: shift the 2-frame stick history BEFORE overwriting frame-1.
        // Order matters — assigning _prevStickX first would make _prevStickX2 a
        // copy of this frame, and every smash-turn check would read as "held",
        // silently killing dash-dance.
        _prevStickX2 = _prevStickX;
        _prevStickX = input.MainX;
        _prevStickY = input.MainY;
        _prevJumpHeld = jumpHeld;
        _prevAttackHeld = attackHeld;
        _prevSpecialHeld = specialHeld;

        PlayerActionState newState = DeriveActionState();
        StateFrame = newState == CurrentState ? StateFrame + 1 : 0;
        CurrentState = newState;
    }

    /// <summary>Phase 6: pure function from this tick's already-resolved movement
    /// fields to the externally-visible action state. No side effects, no reads of
    /// input — every branch here is something Phase 7/8 can key off later.</summary>
    private PlayerActionState DeriveActionState()
    {
        if (IsEliminated) return PlayerActionState.Eliminated;
        if (HitstunFramesRemaining > 0) return PlayerActionState.Hitstun;
        if (IsInvincible) return PlayerActionState.Respawning;
        if (CurrentMove.HasValue) return PlayerActionState.Attack;
        if (JumpSquatFramesRemaining > 0) return PlayerActionState.JumpSquat;
        if (!Grounded)
        {
            if (FastFalling) return PlayerActionState.FastFall;
            return Velocity.Y < Fx.Zero ? PlayerActionState.Jump : PlayerActionState.Fall;
        }
        if (_landingFramesRemaining > 0) return PlayerActionState.Landing;
        return Ground switch
        {
            GroundState.Walk => PlayerActionState.Walk,
            GroundState.Dash => PlayerActionState.Dash,
            GroundState.Run => PlayerActionState.Run,
            _ => PlayerActionState.Idle,
        };
    }

    /// <summary>Phase 7: apply a landed hit. Interrupts anything in progress (dash,
    /// jump-squat, an existing hitstun, an in-progress attack) and starts a fresh
    /// input-less airborne window. attackerFacingRight mirrors Hitbox.DirX — a hit
    /// thrown by a left-facing attacker sends the defender the other way.</summary>
    public void ApplyHit(in Hitbox hit, bool attackerFacingRight)
        => ApplyHit(in hit, attackerFacingRight, defenderWeight: null);

    /// <summary>Overload taking an explicit defender weight now that
    /// KnockbackMath uses the real Melee formula (weight-dependent). Pass null
    /// to fall back to KnockbackMath's default.</summary>
    public void ApplyHit(in Hitbox hit, bool attackerFacingRight, Fx? defenderWeight)
    {
        // CORRECTED: knockback must use the percent BEFORE this hit's damage is
        // added (see KnockbackMath's file-level correction note) — the previous
        // pass of this method had Percent += hit.Damage happening BEFORE the
        // magnitude calculation, which is backwards versus MeleeLight's real
        // getKnockback/percent-increment ordering.
        Fx percentBeforeHit = Percent;
        Fx magnitude = defenderWeight.HasValue
            ? KnockbackMath.ComputeMagnitude(percentBeforeHit, in hit, defenderWeight.Value)
            : KnockbackMath.ComputeMagnitude(percentBeforeHit, in hit);
        Percent += hit.Damage;
        Fx dirX = attackerFacingRight ? hit.DirX : -hit.DirX;

        // magnitude is a knockback MAGNITUDE, not a velocity — see
        // KnockbackMath.VelocityScale's doc comment for why this multiply is
        // required (its previous absence was the "flies off the map on any
        // hit" bug: every launch was ~33x too fast). HitstunFramesRemaining
        // deliberately uses the raw, unscaled magnitude — see the same doc
        // comment on why hitstun and velocity read different units here.
        Fx launchSpeed = magnitude * KnockbackMath.VelocityScale;
        Velocity = new FxVec2(dirX * launchSpeed, hit.DirY * launchSpeed);
        HitstunFramesRemaining = KnockbackMath.ComputeHitstunFrames(magnitude);

        // A hit always interrupts cleanly — no partial jump-squats, half-finished
        // dashes, or in-progress attacks carried into hitstun.
        JumpSquatFramesRemaining = 0;
        DashFramesRemaining = 0;
        Ground = GroundState.Idle;
        FastFalling = false;
        Grounded = false;
        CurrentMove = null;
        MoveFrame = 0;
    }

    /// <summary>Phase 9: freeze this fighter (no input, no movement) for the
    /// computed hitlag window — see HitlagFramesRemaining's doc comment. Called
    /// on both attacker and defender by CombatSystem the instant a hit connects.</summary>
    public void ApplyHitlag(int frames)
    {
        if (frames > HitlagFramesRemaining) HitlagFramesRemaining = frames;
    }

    /// <summary>Phase 9: does this mover currently have a live, not-yet-applied
    /// hitbox out? Consumed by Gameplay/CombatSystem.cs once per tick per mover.
    /// Facing/position are NOT baked in here — CombatSystem reads Position/
    /// FacingRight/HalfSize directly to build the actual overlap test, since this
    /// engine has no separate hitbox spatial data (see MoveDef's doc comment).</summary>
    public bool TryGetActiveHitbox(out Hitbox hitbox)
    {
        hitbox = default;
        if (!CurrentMove.HasValue || _hitApplied) return false;
        if (!_character.TryGetMove(CurrentMove.Value, out MoveDef move)) return false;
        if (!move.IsActiveOnFrame(MoveFrame)) return false;
        hitbox = move.ToHitbox();
        return true;
    }

    /// <summary>Called by CombatSystem once this mover's active hitbox actually
    /// connects, so the same activation can't hit twice (real Melee moves with a
    /// multi-frame active window only need to land once per activation here,
    /// since MoveDef collapses multi-hit moves to one representative hit anyway
    /// — see MoveData.cs).</summary>
    public void MarkHitApplied() => _hitApplied = true;

    /// <summary>No stick input, no air control, no double jump, no fast-fall — just
    /// gravity (same cap as normal falling) plus knockback velocity decay. Real DI
    /// (directional influence) is still a later concern: MeleeLight's real DI
    /// algorithm (src/physics/hitDetection.js:getLaunchAngle) needs atan/sin/cos,
    /// and Fx has no fixed-point trig table yet (see Hitbox.cs's doc comment) —
    /// that's real work, not something to fake here.</summary>
    private void TickHitstun()
    {
        HitstunFramesRemaining--;

        Fx velX = DecayTowardZero(Velocity.X, KnockbackMath.KnockbackDecayPerTick);
        Fx velYAfterDecay = DecayTowardZero(Velocity.Y, KnockbackMath.KnockbackDecayPerTick);
        Fx velY = Fx.Min(velYAfterDecay + _character.Physics.Gravity, _character.Physics.FallSpeedCap);
        Velocity = new FxVec2(velX, velY);
    }

    private static Fx DecayTowardZero(Fx value, Fx amount)
    {
        if (value > Fx.Zero) return Fx.Max(Fx.Zero, value - amount);
        if (value < Fx.Zero) return Fx.Min(Fx.Zero, value + amount);
        return value;
    }

    private void TickJumpSquat(bool jumpHeld)
    {
        _jumpHeldDuringSquat &= jumpHeld;
        JumpSquatFramesRemaining--;
        if (JumpSquatFramesRemaining == 0)
        {
            Fx magnitude = _jumpHeldDuringSquat
                ? _character.Physics.FullHopVelocity
                : _character.Physics.ShortHopVelocity;
            Velocity = new FxVec2(Velocity.X, -magnitude);
        }
        else
        {
            // Same trick TestBody.cs relies on: CollisionResolver only re-confirms
            // Grounded when Velocity.Y is downward THIS tick (it's an event — "fell
            // onto it this frame" — not a continuous resting-contact check). Without
            // this nudge, sitting at rest with Velocity.Y == 0 would silently drop
            // Grounded to false the tick after landing.
            Velocity = new FxVec2(Velocity.X, Velocity.Y + _character.Physics.Gravity);
        }
    }

    /// <summary>Phase 9/11: does this frame's input start a new move? Grounded
    /// normals are chosen via stick direction + SmashTiltClassifier (neutral =>
    /// Jab1, side => Ftilt/Fsmash, up => Utilt/Usmash, down => Dtilt/Dsmash),
    /// EXCEPT while actually dashing/running, where attack always means
    /// DashAttack regardless of stick (Phase 11 — matches real Melee's "you're
    /// already committed to the dash" priority; DashAttack data existed in
    /// FoxMoves.cs since Phase 9 but was never reachable until now). Aerials via
    /// stick direction relative to facing (neutral/forward/back/up/down).
    ///
    /// Phase 11: the special button now reads stick direction too — up => UpB,
    /// down => DownB, side => SideB, neutral => NeutralB — instead of always
    /// going to NeutralB. Returns false (does nothing, button press "eaten") if
    /// the resolved slot has no data for this character — e.g. aiming Side-B
    /// while airborne, since the hybrid's Side-B (Falco's Phantasm, per
    /// FoxFalcoHybrid.cs's override table) is grounded-only (see
    /// FalcoMoves.cs's doc comment) — rather than silently falling back to some
    /// other move.</summary>
    private bool TryStartAttack(FrameInput input, bool attackPressed, bool specialPressed, bool grounded)
    {
        if (!attackPressed && !specialPressed) return false;

        bool pastWalkX = System.Math.Abs((int)input.MainX) >= InputDecode.StickDeadzoneUnits;
        bool pastWalkY = System.Math.Abs((int)input.MainY) >= InputDecode.StickDeadzoneUnits;

        MoveSlot slot;
        if (specialPressed)
        {
            if (pastWalkY && input.MainY > 0) slot = MoveSlot.UpB;
            else if (pastWalkY && input.MainY < 0) slot = MoveSlot.DownB;
            else if (pastWalkX) slot = MoveSlot.SideB;
            else slot = MoveSlot.NeutralB;
        }
        else if (grounded)
        {
            if (Ground == GroundState.Dash || Ground == GroundState.Run)
            {
                slot = MoveSlot.DashAttack;
            }
            else
            {
                AttackStrength strengthX = _smashTilt.ClassifyHorizontal(_prevStickX, input.MainX);
                AttackStrength strengthY = _smashTilt.ClassifyVertical(_prevStickY, input.MainY);

                if (pastWalkY && input.MainY > 0)
                    slot = strengthY == AttackStrength.Smash ? MoveSlot.UpSmash : MoveSlot.UpTilt;
                else if (pastWalkY && input.MainY < 0)
                    slot = strengthY == AttackStrength.Smash ? MoveSlot.DownSmash : MoveSlot.DownTilt;
                else if (pastWalkX)
                    slot = strengthX == AttackStrength.Smash ? MoveSlot.ForwardSmash : MoveSlot.ForwardTilt;
                else
                    slot = MoveSlot.Jab1;
            }
        }
        else
        {
            bool towardFacing = pastWalkX && (System.Math.Sign((int)input.MainX) > 0) == FacingRight;

            if (pastWalkY && input.MainY > 0) slot = MoveSlot.UpAir;
            else if (pastWalkY && input.MainY < 0) slot = MoveSlot.DownAir;
            else if (pastWalkX && towardFacing) slot = MoveSlot.ForwardAir;
            else if (pastWalkX) slot = MoveSlot.BackAir;
            else slot = MoveSlot.NeutralAir;
        }

        // Phase 11: Side-B is only modeled grounded for the hybrid's current
        // move in this slot (Falco's Phantasm — see FalcoMoves.cs's doc
        // comment; Fox's own Illusion, still in FoxMoves.cs but unused by the
        // hybrid, is grounded-only for the same reason).
        if (slot == MoveSlot.SideB && !grounded) return false;

        if (!_character.TryGetMove(slot, out MoveDef move)) return false;

        CurrentMove = slot;
        MoveFrame = 1;
        _hitApplied = false;

        // Phase 11: moves with real self-propulsion (Up-B/Side-B) lock in their
        // launch direction/magnitude right here, once, at button-press — this
        // engine has no continuous re-aim (see FoxMoves.cs's Fire Fox doc comment
        // on why that's a documented simplification vs. real Melee).
        if (move.LaunchSpeed > Fx.Zero)
        {
            if (slot == MoveSlot.UpB)
            {
                // Two-way aim only (straight up, or a 45-degree diagonal toward
                // held stick X) — Fx has no arbitrary-angle trig, see MoveData.cs's
                // LaunchSpeed doc comment.
                bool aimSide = pastWalkX;
                Fx diag = Fx.Ratio(707_107, 1_000_000);
                Fx dirX = aimSide ? diag * Fx.FromInt(System.Math.Sign((int)input.MainX)) : Fx.Zero;
                Fx dirY = aimSide ? -diag : -Fx.One;
                _selfLaunchVelocity = new FxVec2(dirX, dirY) * move.LaunchSpeed;
            }
            else // SideB: pure horizontal burst in the direction Fox is facing.
            {
                Fx dirX = FacingRight ? Fx.One : -Fx.One;
                _selfLaunchVelocity = new FxVec2(dirX * move.LaunchSpeed, Fx.Zero);
            }
        }

        return true;
    }

    /// <summary>Phase 9: advance the active move by one frame. GroundedNormal
    /// moves lock horizontal velocity to zero for the move's duration (a real
    /// simplification — some real Melee grounded attacks retain some momentum;
    /// see the class-level doc comment); Aerial moves keep normal airborne
    /// gravity/fall-cap physics running underneath (no air control while
    /// attacking, and no new jumps — TryStartAttack already only fires when not
    /// mid-jump-squat/hitstun, and this branch doesn't check jump input at all).
    /// The move ends after TotalFrames ticks; there is no early-cancel via IASA
    /// yet (IasaFrame is recorded on MoveDef but unused) — another explicit gap,
    /// not a silent one.</summary>
    private void TickAttacking(FrameInput input)
    {
        if (!_character.TryGetMove(CurrentMove!.Value, out MoveDef move))
        {
            // Shouldn't happen (TryStartAttack already validated this), but fail
            // safe rather than getting stuck in a move with no data.
            CurrentMove = null;
            MoveFrame = 0;
            return;
        }

        if (move.LaunchSpeed > Fx.Zero)
        {
            // Phase 11: Up-B/Side-B — ride the decaying self-launch velocity
            // instead of normal gravity/locked-in-place physics. No air control,
            // no gravity, while it's still thrusting (matches MeleeLight's own
            // UPSPECIALLAUNCH.js/SIDESPECIALGROUND.js not applying gravity during
            // their own active windows either).
            Fx speed = _selfLaunchVelocity.X * _selfLaunchVelocity.X + _selfLaunchVelocity.Y * _selfLaunchVelocity.Y;
            if (speed > Fx.Zero)
            {
                Fx decayedX = DecayTowardZero(_selfLaunchVelocity.X, move.LaunchDecayPerTick);
                Fx decayedY = DecayTowardZero(_selfLaunchVelocity.Y, move.LaunchDecayPerTick);
                _selfLaunchVelocity = new FxVec2(decayedX, decayedY);
            }
            Velocity = _selfLaunchVelocity;
        }
        else if (move.Category == MoveCategory.Aerial)
        {
            Fx velY = Velocity.Y;
            Fx cap = FastFalling ? _character.Physics.FastFallSpeedCap : _character.Physics.FallSpeedCap;
            velY = Fx.Min(velY + _character.Physics.Gravity, cap);
            Velocity = new FxVec2(Velocity.X, velY);
        }
        else
        {
            // GroundedNormal and Special (neutral-B/reflector): locked in place.
            // Still needs the same per-tick gravity nudge TickGrounded applies,
            // for the same CollisionResolver-re-triggering reason.
            Velocity = new FxVec2(Fx.Zero, Velocity.Y + _character.Physics.Gravity);
        }

        MoveFrame++;
        if (MoveFrame > move.TotalFrames)
        {
            bool wasUpB = CurrentMove == MoveSlot.UpB;
            CurrentMove = null;
            MoveFrame = 0;

            // Phase 11: real Melee makes Fox helpless after Fire Fox — no more
            // jumps or specials until landing. This engine has no dedicated
            // Helpless action state yet, so it's approximated by zeroing
            // JumpsRemaining (refreshed on next landing, same as any other jump
            // use) rather than adding a new state enum for one move.
            if (wasUpB && !Grounded) JumpsRemaining = 0;
        }
    }

    // ---- Step 3: Fox locomotion, transcribed from MeleeLight ---------------
    // Sources: src/characters/shared/moves/{WAIT,WALK,DASH,RUN,TILTTURN,SMASHTURN}.js
    // and src/physics/actionStateShortcuts.js (reduceByTraction, checkForSmashTurn).
    //
    // UNITS: MeleeLight's lsX is -1..1; this engine's FrameInput.MainX is -100..100
    // (Docs/Gameplay/AnalogInputRules.md). Every threshold below is therefore the
    // source's decimal x100 -- 0.79 -> 79, 0.62 -> 62, 0.3 -> 30 -- and StickX()
    // converts the raw byte to an Fx fraction for the accel formulas.

    /// <summary>Stick X as MeleeLight's lsX: a -1..1 fraction.</summary>
    private static Fx StickX(FrameInput input) => Fx.Ratio(input.MainX, 100);

    /// <summary>
    /// MeleeLight's <c>reduceByTraction</c>. Bleeds |Velocity.X| toward zero by
    /// traction, clamping through zero rather than overshooting into a reversal.
    /// <paramref name="applyDouble"/> doubles it while above walk speed — that is
    /// what makes a pivot out of a run brake hard instead of mushy.
    /// </summary>
    private void ReduceByTraction(bool applyDouble)
    {
        Fx traction = _character.Physics.GroundTraction;
        Fx vx = Velocity.X;
        Fx walkMax = _character.Physics.WalkSpeed;

        if (vx > Fx.Zero)
        {
            vx -= (applyDouble && vx > walkMax) ? traction + traction : traction;
            if (vx < Fx.Zero) vx = Fx.Zero;
        }
        else if (vx < Fx.Zero)
        {
            vx += (applyDouble && vx < -walkMax) ? traction + traction : traction;
            if (vx > Fx.Zero) vx = Fx.Zero;
        }

        Velocity = new FxVec2(vx, Velocity.Y);
    }

    /// <summary>
    /// MeleeLight's <c>checkForSmashTurn</c>: the stick is hard the OPPOSITE way
    /// to our facing right now, and was not two frames ago. Two frames, not one —
    /// that is what distinguishes a deliberate flick from a stick already held
    /// over, and it is the mechanism dash-dancing runs on.
    /// </summary>
    private bool CheckForSmashTurn(sbyte x)
    {
        int face = FacingRight ? 1 : -1;
        return x * face < -InputDecode.DashThresholdUnits
            && _prevStickX2 * face > -30;
    }

    private void TickGrounded(FrameInput input, bool jumpPressed)
    {
        if (jumpPressed)
        {
            JumpSquatFramesRemaining = _character.Physics.JumpSquatFrames;
            _jumpHeldDuringSquat = true;
            TickJumpSquat(jumpHeld: true); // consume this frame too
            return;
        }

        sbyte x = input.MainX;
        int absX = System.Math.Abs((int)x);
        int dir = System.Math.Sign((int)x);
        int face = FacingRight ? 1 : -1;
        var physics = _character.Physics;

        switch (Ground)
        {
            // ---- WAIT / WALK ------------------------------------------------
            case GroundState.Idle:
            case GroundState.Walk:
                if (CheckForSmashTurn(x))
                {
                    // SMASHTURN.js: face flips immediately, then brake hard.
                    EnterGroundState(GroundState.SmashTurn);
                    FacingRight = dir > 0;
                    ReduceByTraction(true);
                }
                else if (dir != 0 && dir != face && absX >= InputDecode.StickDeadzoneUnits)
                {
                    // TILTTURN.js: a gentle flick the other way. Face does NOT
                    // flip until frame 6 (handled in the TiltTurn case).
                    EnterGroundState(GroundState.TiltTurn);
                    ReduceByTraction(true);
                }
                else if (absX >= InputDecode.DashThresholdUnits && dir == face)
                {
                    StartDash(x, absX, face);
                }
                else if (absX >= InputDecode.StickDeadzoneUnits)
                {
                    Ground = GroundState.Walk;
                    FacingRight = dir > 0;
                    TickWalk(x);
                }
                else
                {
                    Ground = GroundState.Idle;
                    ReduceByTraction(false);
                }
                break;

            // ---- DASH -------------------------------------------------------
            case GroundState.Dash:
                GroundTimer++;
                TickDash(x, absX, face);
                break;

            // ---- RUN --------------------------------------------------------
            case GroundState.Run:
                if (CheckForSmashTurn(x))
                {
                    EnterGroundState(GroundState.SmashTurn);
                    FacingRight = dir > 0;
                    ReduceByTraction(true);
                }
                else if (absX < 30)
                {
                    // RUNBRAKE, collapsed to Idle + double traction: this engine
                    // has no separate brake action state yet (it needs an
                    // animation to be worth naming -- Phase 8).
                    Ground = GroundState.Idle;
                    ReduceByTraction(true);
                }
                else
                {
                    TickRun(x);
                }
                break;

            // ---- TILTTURN / SMASHTURN --------------------------------------
            case GroundState.TiltTurn:
                GroundTimer++;
                // TILTTURN.js: facing flips on frame 6 exactly, and frame 6 is
                // also the only frame it can dash out of (source additionally
                // gates that on a dash buffer this engine doesn't model yet).
                if (GroundTimer == 6)
                {
                    FacingRight = !FacingRight;
                    if (x * (FacingRight ? 1 : -1) > InputDecode.DashThresholdUnits)
                    {
                        StartDash(x, absX, FacingRight ? 1 : -1);
                        break;
                    }
                }
                ReduceByTraction(true);
                if (GroundTimer > 11) EnterGroundState(GroundState.Idle);
                break;

            case GroundState.SmashTurn:
                GroundTimer++;
                // SMASHTURN.js: dash-out is allowed on frame 2 EXACTLY -- not a
                // window, a single frame (`timer === 2 && lsX*face > 0.79`). That
                // one-frame gate is what makes real dash-dancing timing-sensitive
                // rather than something you get for free by waggling the stick;
                // an earlier draft of this used a >=5 window and produced a
                // dash-dance that drifted across the stage instead of holding
                // position. Otherwise it runs to frame 11 and returns to WAIT.
                if (GroundTimer == 2 && x * face > InputDecode.DashThresholdUnits)
                {
                    StartDash(x, absX, face);
                    break;
                }
                ReduceByTraction(true);
                if (GroundTimer > 11) EnterGroundState(GroundState.Idle);
                break;
        }

        // See TickJumpSquat's comment: the resolver only re-confirms Grounded when
        // Velocity.Y is downward this tick, so resting contact needs re-triggering
        // every tick rather than staying latched from the frame we landed.
        Velocity = new FxVec2(Velocity.X, Velocity.Y + physics.Gravity);
    }

    /// <summary>
    /// Begin a dash AND run its first frame immediately. MeleeLight's
    /// <c>init()</c> always calls <c>main()</c> on the same frame, so a dash
    /// entered this tick is already on timer 1 — not 0. Splitting them would push
    /// the frame-2 impulse a tick late, which is exactly the kind of one-frame
    /// drift that makes dash-dance feel wrong without ever looking broken.
    /// </summary>
    private void StartDash(sbyte x, int absX, int face)
    {
        EnterGroundState(GroundState.Dash);
        GroundTimer++;
        TickDash(x, absX, face);
    }

    /// <summary>Enter a ground state and restart its frame timer, so every
    /// "timer == n" comparison below means the same thing MeleeLight's does.</summary>
    private void EnterGroundState(GroundState state)
    {
        Ground = state;
        GroundTimer = 0;
        if (state == GroundState.Dash) DashFramesRemaining = _character.Physics.DashFrameMax;
    }

    /// <summary>
    /// DASH.js's main + interrupt, in source order. The dash-dance behaviour
    /// everything else in Step 3 exists for lives in the two timer thresholds at
    /// the bottom: past dashFrameMax a fresh flick re-dashes, past dashFrameMin a
    /// held stick becomes a run.
    /// </summary>
    private void TickDash(sbyte x, int absX, int face)
    {
        var physics = _character.Physics;

        // interrupt(): smash-turn is allowed only after frame 4.
        if (GroundTimer > 4 && CheckForSmashTurn(x))
        {
            Velocity = new FxVec2(Velocity.X * Fx.Ratio(1, 4), Velocity.Y);
            EnterGroundState(GroundState.SmashTurn);
            FacingRight = System.Math.Sign((int)x) > 0;
            return;
        }

        // Re-dash (the dash-dance): past dashFrameMax, stick freshly flicked
        // forward again. Source uses input[2] for "freshly" -- same two-frame
        // rule as the smash-turn check.
        if (GroundTimer > physics.DashFrameMax
            && x * face > InputDecode.DashThresholdUnits
            && _prevStickX2 * face < 30)
        {
            StartDash(x, absX, face);
            return;
        }

        // Held forward past dashFrameMin -> RUN.
        if (GroundTimer > physics.DashFrameMin && x * face > 62)
        {
            EnterGroundState(GroundState.Run);
            return;
        }

        // Animation over -> WAIT.
        if (GroundTimer > physics.DashTotalFrames)
        {
            EnterGroundState(GroundState.Idle);
            return;
        }

        // main(): frame 2 applies the dash impulse, clamped to dMaxV.
        if (GroundTimer == 2)
        {
            Fx vx = Velocity.X + physics.DashInitialSpeed * Fx.FromInt(face);
            if (Fx.Abs(vx) > physics.RunSpeed) vx = physics.RunSpeed * Fx.FromInt(face);
            Velocity = new FxVec2(vx, Velocity.Y);
        }

        if (GroundTimer > 1)
        {
            if (absX < 30)
            {
                ReduceByTraction(false);
            }
            else
            {
                Fx lsX = Fx.Ratio(x, 100);
                Fx tempMax = lsX * physics.RunSpeed;
                Fx tempAcc = lsX * physics.DashAccelA;
                Fx vx = Velocity.X + tempAcc;

                bool overshot = (tempMax > Fx.Zero && vx > tempMax) || (tempMax < Fx.Zero && vx < tempMax);
                if (overshot)
                {
                    Velocity = new FxVec2(vx, Velocity.Y);
                    ReduceByTraction(false);
                    vx = Velocity.X;
                    if ((tempMax > Fx.Zero && vx < tempMax) || (tempMax < Fx.Zero && vx > tempMax))
                        vx = tempMax;
                }
                else
                {
                    // NOT a transcription slip: DASH.js really does add tempAcc a
                    // SECOND time on this branch (it adds once before the check,
                    // then again inside the else). Kept faithful -- if this ever
                    // looks wrong, check the source before "fixing" it.
                    vx += tempAcc;
                    if ((tempMax > Fx.Zero && vx > tempMax) || (tempMax < Fx.Zero && vx < tempMax))
                        vx = tempMax;
                }

                Velocity = new FxVec2(vx, Velocity.Y);
            }
        }
    }

    /// <summary>
    /// WALK.js. The acceleration is proportional to the gap to target, not a flat
    /// per-tick step — which is why walking has that soft ramp in Melee and why a
    /// MoveToward() approximation never felt right.
    /// Source: <c>(tempMax - vx) * (1/(walkMaxV*2)) * (walkInitV + walkAcc)</c>.
    /// </summary>
    private void TickWalk(sbyte x)
    {
        var physics = _character.Physics;
        Fx tempMax = physics.WalkSpeed * Fx.Ratio(x, 100);

        if (Fx.Abs(Velocity.X) > Fx.Abs(tempMax))
        {
            ReduceByTraction(true);
            return;
        }

        Fx denom = physics.WalkSpeed * Fx.FromInt(2);
        Fx tempAcc = (tempMax - Velocity.X) / denom * (physics.WalkInitialSpeed + physics.GroundAccel);
        Fx vx = Velocity.X + tempAcc;

        int face = FacingRight ? 1 : -1;
        Fx faceFx = Fx.FromInt(face);
        if (vx * faceFx > tempMax * faceFx) vx = tempMax;

        Velocity = new FxVec2(vx, Velocity.Y);
    }

    /// <summary>
    /// RUN.js. Same gap-proportional shape as walk but with the two-stage dash
    /// acceleration, where the second stage is divided by |lsX| — so a partly
    /// tilted stick accelerates HARDER per unit of input, not softer.
    /// Source: <c>vx += (dMaxV*lsX - vx) * (1/(dMaxV*2.5)) * (dAccA + dAccB/|lsX|)</c>.
    /// </summary>
    private void TickRun(sbyte x)
    {
        var physics = _character.Physics;
        Fx lsX = Fx.Ratio(x, 100);
        Fx absLsX = Fx.Abs(lsX);
        if (absLsX == Fx.Zero) return; // guarded: the source divides by this

        Fx tempMax = lsX * physics.RunSpeed;
        Fx denom = physics.RunSpeed * Fx.Ratio(25, 10);
        Fx accel = physics.DashAccelA + physics.DashAccelB / absLsX;
        Fx vx = Velocity.X + (tempMax - Velocity.X) / denom * accel;

        int face = FacingRight ? 1 : -1;
        Fx faceFx = Fx.FromInt(face);
        if (vx * faceFx > tempMax * faceFx) vx = tempMax;

        Velocity = new FxVec2(vx, Velocity.Y);
    }

    private void TickAirborne(FrameInput input, bool jumpPressed, bool holdingDown)
    {
        sbyte x = input.MainX;
        bool pastWalk = System.Math.Abs((int)x) >= InputDecode.StickDeadzoneUnits;
        Fx targetX = pastWalk ? _character.Physics.AirSpeedMax * Fx.Ratio(x, 100) : Fx.Zero;
        Fx velX = MoveToward(Velocity.X, targetX, _character.Physics.AirAccel);

        if (jumpPressed && JumpsRemaining > 0)
        {
            JumpsRemaining--;
            FastFalling = false;
            Velocity = new FxVec2(velX, -_character.Physics.DoubleJumpVelocity);
            return;
        }

        Fx velY = Velocity.Y;
        if (!FastFalling && holdingDown && velY > Fx.Zero)
        {
            FastFalling = true;
            velY = _character.Physics.FastFallSpeedCap;
        }

        Fx cap = FastFalling ? _character.Physics.FastFallSpeedCap : _character.Physics.FallSpeedCap;
        velY = Fx.Min(velY + _character.Physics.Gravity, cap);

        Velocity = new FxVec2(velX, velY);
    }

    private static Fx MoveToward(Fx current, Fx target, Fx maxDelta)
    {
        Fx diff = target - current;
        if (diff > Fx.Zero) return Fx.Min(current + maxDelta, target);
        if (diff < Fx.Zero) return Fx.Max(current - maxDelta, target);
        return current;
    }

    public void SaveState(StateWriter w)
    {
        w.WriteFxVec2(Position);
        w.WriteFxVec2(PreviousPosition);
        w.WriteFxVec2(Velocity);
        w.WriteBool(Grounded);
        w.WriteInt((int)Surface);
        w.WriteInt(SurfaceIndex);
        w.WriteBool(FacingRight);
        w.WriteBool(FastFalling);
        w.WriteInt((int)Ground);
        w.WriteInt(GroundTimer);
        w.WriteInt(DashFramesRemaining);
        w.WriteInt(JumpSquatFramesRemaining);
        w.WriteInt(JumpsRemaining);
        w.WriteByte((byte)_prevStickX);
        w.WriteByte((byte)_prevStickX2);
        w.WriteByte((byte)_prevStickY);
        w.WriteBool(_prevJumpHeld);
        w.WriteBool(_prevAttackHeld);
        w.WriteBool(_prevSpecialHeld);
        w.WriteBool(_jumpHeldDuringSquat);
        w.WriteInt(_landingFramesRemaining);
        w.WriteInt((int)CurrentState);
        w.WriteInt(StateFrame);
        w.WriteFx(Percent);
        w.WriteInt(HitstunFramesRemaining);
        w.WriteInt(CurrentMove.HasValue ? (int)CurrentMove.Value : -1);
        w.WriteInt(MoveFrame);
        w.WriteBool(_hitApplied);
        w.WriteInt(HitlagFramesRemaining);
        var (smashX, smashY) = _smashTilt.SaveRaw();
        w.WriteInt(smashX);
        w.WriteInt(smashY);
        w.WriteInt(Stocks);
        w.WriteBool(IsEliminated);
        w.WriteInt(_respawnInvincibilityFramesRemaining);
    }

    public void LoadState(StateReader r)
    {
        Position = r.ReadFxVec2();
        PreviousPosition = r.ReadFxVec2();
        Velocity = r.ReadFxVec2();
        Grounded = r.ReadBool();
        Surface = (SurfaceKind)r.ReadInt();
        SurfaceIndex = r.ReadInt();
        FacingRight = r.ReadBool();
        FastFalling = r.ReadBool();
        Ground = (GroundState)r.ReadInt();
        GroundTimer = r.ReadInt();
        DashFramesRemaining = r.ReadInt();
        JumpSquatFramesRemaining = r.ReadInt();
        JumpsRemaining = r.ReadInt();
        _prevStickX = (sbyte)r.ReadByte();
        _prevStickX2 = (sbyte)r.ReadByte();
        _prevStickY = (sbyte)r.ReadByte();
        _prevJumpHeld = r.ReadBool();
        _prevAttackHeld = r.ReadBool();
        _prevSpecialHeld = r.ReadBool();
        _jumpHeldDuringSquat = r.ReadBool();
        _landingFramesRemaining = r.ReadInt();
        CurrentState = (PlayerActionState)r.ReadInt();
        StateFrame = r.ReadInt();
        Percent = r.ReadFx();
        HitstunFramesRemaining = r.ReadInt();
        int moveRaw = r.ReadInt();
        CurrentMove = moveRaw < 0 ? null : (MoveSlot)moveRaw;
        MoveFrame = r.ReadInt();
        _hitApplied = r.ReadBool();
        HitlagFramesRemaining = r.ReadInt();
        int smashX = r.ReadInt();
        int smashY = r.ReadInt();
        _smashTilt.LoadRaw(smashX, smashY);
        Stocks = r.ReadInt();
        IsEliminated = r.ReadBool();
        _respawnInvincibilityFramesRemaining = r.ReadInt();
    }
}
