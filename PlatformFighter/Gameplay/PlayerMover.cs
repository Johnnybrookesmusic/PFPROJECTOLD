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
    /// <summary>Holding an opponent — CATCHWAIT/CATCHATTACK/THROW* in
    /// MeleeLight terms, all folded into one reported state (see
    /// PlayerMover.TickGrabbing's doc comment for why). Top priority, same
    /// tier as Hitstun/Grabbed — a grabbing fighter cannot be doing anything
    /// else.</summary>
    Grabbing = 12,
    /// <summary>Held by an opponent — CAPTUREPULLED/CAPTUREWAIT folded into
    /// one reported state. See PlayerMover.TickGrabbed.</summary>
    Grabbed = 13,
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

    /// <summary>
    /// Which specific hitbox instance has already connected, encoded as
    /// (specIndex &lt;&lt; 16) | hitGroup. -1 = none yet this move.
    ///
    /// A move now owns MANY hitboxes (see Characters/HitboxSpec.cs), and a
    /// multi-hit move is SUPPOSED to strike the same opponent repeatedly — the
    /// drill hits every 3 frames, fair hits five times. A single "has this move
    /// hit yet" bool cannot express that: it would let the drill land exactly
    /// one of its up-to-seven hits. Keying on the specific box AND its
    /// repetition index means a new hit is allowed exactly when the source
    /// re-arms the box, and no more often.
    /// </summary>
    private int _appliedHitKey = -1;

    /// <summary>
    /// Set on the frame a move spawns a projectile; read and cleared by
    /// CombatSystem, which owns the pool. PlayerMover deliberately does not
    /// hold the pool itself — a fighter should not be able to reach into the
    /// projectile system directly, and routing it through CombatSystem keeps
    /// every damage source going through one place.
    /// </summary>
    public bool PendingProjectileSpawn { get; private set; }
    public void ConsumeProjectileSpawn() => PendingProjectileSpawn = false;

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
    /// <summary>A fresh A press was seen during the current jab's own window, so
    /// the next jab in the chain should fire. Mirrors MeleeLight's
    /// <c>player[p].phys.jabCombo</c>.</summary>
    private bool _jabComboQueued;

    // ---- Grab/throw state (real MeleeLight port — see TickGrabbing/TickGrabbed) ----
    /// <summary>True while this fighter is holding a grabbed opponent
    /// (CATCHWAIT/CATCHATTACK/THROW* folded together — see TickGrabbing).</summary>
    public bool IsGrabbingOpponent { get; private set; }
    /// <summary>True while this fighter is held by an opponent's grab
    /// (CAPTUREPULLED/CAPTUREWAIT folded together — see TickGrabbed).</summary>
    public bool IsGrabbedByOpponent { get; private set; }
    /// <summary>Victim-side only. MeleeLight's phys.stuckTimer — counts down
    /// every tick, extra -3 on a fresh mash input (CAPTUREWAIT.js). Escape
    /// when it goes negative.</summary>
    private int _grabStuckTimer;
    /// <summary>Attacker-side only. >0 while a pummel (CATCHATTACK) is playing;
    /// the actual damage application at the real active frame is CombatSystem's
    /// job (it is the one that can reach the victim) — see PummelFrame.</summary>
    private int _pummelFrame;
    /// <summary>Attacker-side: current pummel sub-state frame, exposed so
    /// CombatSystem can apply CATCHATTACK's hit on its real active frame (10)
    /// without PlayerMover needing a reference to its victim.</summary>
    public int PummelFrame => _pummelFrame;
    /// <summary>Attacker-side: which throw direction has been committed, if any.
    /// Null while still in the CATCHWAIT hold pose.</summary>
    public MoveSlot? CommittedThrow { get; private set; }
    /// <summary>Attacker-side: real ticks elapsed since CommittedThrow was set.
    /// -1 when CommittedThrow is null. CombatSystem compares this against each
    /// throw's real release frame (FoxMoves.ThrowXReleaseFrame) to know exactly
    /// when to apply the throw's damage/knockback to the victim.</summary>
    public int ThrowFrame { get; private set; } = -1;

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
    /// <summary>The stage this fighter is simulating against. Exposed so
    /// CombatSystem can reach the same geometry for projectile despawn without
    /// needing it threaded through its constructor (which every caller would
    /// have to change).</summary>
    public StageGeometry Stage => _stage;
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
            _appliedHitKey = -1;
            PendingProjectileSpawn = false;
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
        else if (IsGrabbedByOpponent)
        {
            TickGrabbed(input, attackPressed, specialPressed, jumpPressed);
        }
        else if (IsGrabbingOpponent)
        {
            TickGrabbing(input, attackPressed);
        }
        else if (CurrentMove.HasValue)
        {
            // JAB1.js/JAB2.js watch for a fresh A press DURING their own window
            // and latch it; the chain fires a few frames later (see
            // TickAttacking). Latching here, where the rising edge is already
            // computed, means a press registers even on a frame the chain isn't
            // allowed to fire yet — which is what makes mashing jab feel
            // responsive instead of eating inputs.
            if (attackPressed && (CurrentMove == MoveSlot.Jab1 || CurrentMove == MoveSlot.Jab2))
                _jabComboQueued = true;

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
        if (IsGrabbedByOpponent) return PlayerActionState.Grabbed;
        if (IsGrabbingOpponent) return PlayerActionState.Grabbing;
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
        // dashes, or in-progress attacks carried into hitstun. Also always ends
        // any grab involvement: a throw's release goes through this exact path
        // (see CombatSystem's throw-release handling), and if a grabBING fighter
        // takes a hit from elsewhere, real Melee lets go of the grab too.
        JumpSquatFramesRemaining = 0;
        DashFramesRemaining = 0;
        Ground = GroundState.Idle;
        FastFalling = false;
        Grounded = false;
        CurrentMove = null;
        MoveFrame = 0;
        IsGrabbedByOpponent = false;
        IsGrabbingOpponent = false;
        CommittedThrow = null;
        ThrowFrame = -1;
        _pummelFrame = 0;
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
    /// <summary>How many hitbox slots the current move has. CombatSystem walks
    /// 0..this-1 and tests each; see TryGetActiveHitbox on why it must test them
    /// all rather than only the first.</summary>
    public int ActiveHitboxSlots
    {
        get
        {
            if (!CurrentMove.HasValue) return 0;
            if (!_character.TryGetMove(CurrentMove.Value, out MoveDef m)) return 0;
            return m.HasHitboxes ? m.Hitboxes!.Length : 1;
        }
    }

    /// <summary>
    /// The hitbox in slot <paramref name="index"/>, if it is live this frame and
    /// has not already connected.
    ///
    /// WHY THIS TAKES AN INDEX. An earlier version returned only the first live
    /// box. That is wrong twice over: Fox's up-tilt has FOUR boxes at four
    /// different offsets, so if box 0 misses spatially, boxes 1-3 would never be
    /// tested and the move would appear to whiff at ranges it should reach.
    /// CombatSystem therefore walks every slot and takes the first that actually
    /// OVERLAPS, which is the same thing MeleeLight's hit loop does.
    ///
    /// WHY THE KEY IS (FirstActiveFrame, HitGroup) AND NOT THE SLOT INDEX.
    /// MeleeLight tracks "already hit" per hitbox OBJECT (a whole
    /// createHitboxObject group shares one hitList), not per individual box.
    /// Keying on the slot index would let a two-box move alternate boxes and
    /// land a hit EVERY frame — the drill would deal 21 hits across its 5..25
    /// window instead of 7. Keying on the box's own window start plus its
    /// refresh cycle groups simultaneous boxes into one hit event (up-tilt's
    /// four boxes = one hit) while keeping genuinely sequential hits distinct
    /// (fair1..fair5 start on frames 6/9/12/15/18 = five separate hits).
    /// </summary>
    public bool TryGetActiveHitbox(int index, out Hitbox hitbox, out int hitKey)
    {
        hitbox = default;
        hitKey = -1;
        if (!CurrentMove.HasValue) return false;
        if (!_character.TryGetMove(CurrentMove.Value, out MoveDef move)) return false;

        if (move.HasHitboxes)
        {
            var boxes = move.Hitboxes!;
            if (index < 0 || index >= boxes.Length) return false;
            if (!boxes[index].IsActiveOnFrame(MoveFrame)) return false;

            hitKey = (boxes[index].FirstActiveFrame << 16)
                   | (boxes[index].HitGroup(MoveFrame) & 0xFFFF);
            if (hitKey == _appliedHitKey) return false;

            hitbox = boxes[index].ToHitbox();
            return true;
        }

        // Legacy flat single-hit path, for any move with no ported hitbox array.
        if (index != 0) return false;
        if (_hitApplied) return false;
        if (!move.IsActiveOnFrame(MoveFrame)) return false;
        hitKey = 0;
        hitbox = move.ToHitbox();
        return true;
    }

    public void MarkHitApplied(int hitKey)
    {
        _hitApplied = true;
        _appliedHitKey = hitKey;
    }

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

    // ---- Grab / throw (real MeleeLight port) -------------------------------
    // Sources: src/characters/shared/moves/{CAPTUREPULLED,CAPTUREWAIT,CATCHWAIT}.js,
    // src/characters/fox/moves/{GRAB,CATCHATTACK,THROWUP,THROWBACK,THROWFORWARD,
    // THROWDOWN}.js, src/physics/hitDetection.js:executeGrabHits,
    // src/physics/actionStateShortcuts.js:mashOut.
    //
    // ARCHITECTURE: PlayerMover never references its opponent directly (same
    // rule ApplyHit already follows — CombatSystem is the only thing that ever
    // holds both PlayerMovers at once). So the split is: each fighter's OWN
    // Tick() drives its OWN half of the grab (stuck-timer/mash for the victim,
    // hold/pummel/throw-commit for the attacker) exactly like every other
    // state in this class; CombatSystem is what actually reaches across to
    // apply damage, pin the victim's position, and keep both sides' flags in
    // sync (grab connect, pummel's real hit frame, a throw's release frame,
    // and "victim escaped -> release attacker too"). See CombatSystem.cs's
    // grab handling for that half.
    //
    // FOLDED STATES, on purpose: CAPTUREPULLED's 2-frame pull-in animation and
    // the periodic no-op self-reinit both CATCHWAIT and CAPTUREWAIT do every
    // framesData.CATCHWAIT/CAPTUREWAIT ticks (30/80 for Fox) are skipped —
    // neither has any gameplay effect in the source (the reinit just restarts
    // that state's own local animation timer; nothing reads it). CATCHATTACK
    // (pummel) and every THROW* are folded into the single "grabbing" state
    // rather than getting their own PlayerActionState entries, since none of
    // them change what the ATTACKER can do (nothing) — only PummelFrame/
    // CommittedThrow, which CombatSystem already reads directly, distinguish
    // them.
    //
    // NOT IMPLEMENTED: grab clank/tech (two opposing grabs connecting the same
    // tick) — CombatSystem's grab-connect guard just lets whichever resolves
    // first win, rather than porting executeGrabTech's mutual-bounce. Real
    // Melee's escape RNG jitter (CAPTUREWAIT.js's
    // `pos.x += 0.5*Math.sign(Math.random()-0.5)`) is also skipped — it's
    // cosmetic (position gets overwritten by the pin next frame regardless)
    // and a real RNG source is a bigger standing gap than this one call site.

    /// <summary>Real per-mash stuck-timer penalty (CAPTUREWAIT.js: `stuckTimer -= 3`
    /// in addition to the normal per-frame decrement) on top of whatever this
    /// tick's normal decrement already did.</summary>
    private const int GrabMashStuckPenalty = 3;
    /// <summary>Real mashOut() hard-stick-flick threshold (0.8 on a -1..1 axis) —
    /// converted to this engine's -100..100 FrameInput.MainX/Y scale.</summary>
    private const int GrabMashStickThreshold = 80;
    /// <summary>Real CATCHWAIT.js throw-direction stick threshold (0.7 on a
    /// -1..1 axis, e.g. `input[p][0].lsY > 0.7`) — NOT the same as
    /// InputDecode.DashThresholdUnits (0.79), which is a different verified
    /// constant for a different gesture (smash-turn). Converted to this
    /// engine's -100..100 scale.</summary>
    private const int GrabThrowStickThreshold = 70;

    /// <summary>Called by CombatSystem the instant a grab hitbox connects.
    /// stuckTimer's real formula (CAPTUREPULLED.js): 100 + 2*percent, using the
    /// victim's percent AT THE MOMENT OF CONNECT — grabs themselves deal no
    /// damage, so this is simply Percent as it already stands.</summary>
    public void EnterGrabbed()
    {
        IsGrabbedByOpponent = true;
        _grabStuckTimer = 100 + 2 * Percent.ToIntFloor();
        Velocity = FxVec2.Zero;
        Grounded = true;
        CurrentMove = null;
        MoveFrame = 0;
        JumpSquatFramesRemaining = 0;
        DashFramesRemaining = 0;
        FastFalling = false;
    }

    /// <summary>Called by CombatSystem on the attacker in the same instant.</summary>
    public void EnterGrabbing()
    {
        IsGrabbingOpponent = true;
        Velocity = FxVec2.Zero;
        CurrentMove = null;
        MoveFrame = 0;
        _pummelFrame = 0;
        CommittedThrow = null;
        ThrowFrame = -1;
    }

    /// <summary>Called by CombatSystem once it notices the victim is no longer
    /// grabbed (escaped via mash, or a throw just released them) — mirrors real
    /// Melee's CATCHCUT triggering on the attacker whenever CAPTURECUT triggers
    /// on the victim.</summary>
    public void ReleaseGrab()
    {
        IsGrabbingOpponent = false;
        _pummelFrame = 0;
        CommittedThrow = null;
        ThrowFrame = -1;
    }

    /// <summary>Pummel damage only — deliberately NOT routed through the normal
    /// ApplyHit pipeline. In the real source, CATCHATTACK's hit goes through the
    /// exact same hit pipeline as any other attack (including its SetKnockback
    /// of 30), but CAPTUREWAIT.main() unconditionally force-sets the victim's
    /// position back to the grab hold spot every single frame regardless of
    /// velocity — so any knockback the pummel formula computes is overwritten
    /// a frame later and has no observable effect. Reproducing "compute real
    /// knockback, then immediately stomp position" would cost real complexity
    /// for a behaviorally identical result, so this applies only the damage,
    /// which IS the entire observable effect.</summary>
    public void ApplyPummelDamage(Fx damage) => Percent += damage;

    /// <summary>Victim side. mashOut(): a FRESH press this frame on any action
    /// button this engine has (attack/special/jump — the real source's A/B/X/Y
    /// all map onto these three), OR a hard stick flick past 0.8. Escapes when
    /// stuckTimer goes negative — CombatSystem notices next tick and releases
    /// the attacker too.</summary>
    private void TickGrabbed(FrameInput input, bool attackPressed, bool specialPressed, bool jumpPressed)
    {
        Velocity = FxVec2.Zero;
        Grounded = true;

        bool mashedButton = attackPressed || specialPressed || jumpPressed;
        bool mashedStick = input.MainX > GrabMashStickThreshold || input.MainX < -GrabMashStickThreshold
                         || input.MainY > GrabMashStickThreshold || input.MainY < -GrabMashStickThreshold;

        _grabStuckTimer--;
        if (mashedButton || mashedStick) _grabStuckTimer -= GrabMashStuckPenalty;

        if (_grabStuckTimer < 0)
        {
            IsGrabbedByOpponent = false;
        }
    }

    /// <summary>Attacker side. Holding pose (CATCHWAIT) watches for a fresh
    /// pummel press or a fresh throw-direction flick; once a throw is
    /// committed it just counts real ticks (ThrowFrame) — CombatSystem is what
    /// compares that against each throw's real release frame and actually
    /// applies it, since only CombatSystem can reach the victim.</summary>
    private void TickGrabbing(FrameInput input, bool attackPressed)
    {
        Velocity = FxVec2.Zero;

        if (_pummelFrame > 0)
        {
            _pummelFrame++;
            // CATCHATTACK.js: interrupt at timer > 24 -> back to the hold pose.
            if (_pummelFrame > 24) _pummelFrame = 0;
            return;
        }

        if (CommittedThrow.HasValue)
        {
            ThrowFrame++;
            return;
        }

        if (attackPressed)
        {
            _pummelFrame = 1;
            return;
        }

        // Fresh stick flick, same rising-edge convention as everywhere else in
        // this file (compare this frame's raw input to last frame's, which
        // _prevStickX/_prevStickY already track for every state).
        bool freshUp = input.MainY > GrabThrowStickThreshold && _prevStickY <= GrabThrowStickThreshold;
        bool freshDown = input.MainY < -GrabThrowStickThreshold && _prevStickY >= -GrabThrowStickThreshold;
        bool freshBack = (FacingRight ? input.MainX < -GrabThrowStickThreshold : input.MainX > GrabThrowStickThreshold)
                       && (FacingRight ? _prevStickX >= -GrabThrowStickThreshold : _prevStickX <= GrabThrowStickThreshold);
        bool freshForward = (FacingRight ? input.MainX > GrabThrowStickThreshold : input.MainX < -GrabThrowStickThreshold)
                          && (FacingRight ? _prevStickX <= GrabThrowStickThreshold : _prevStickX >= -GrabThrowStickThreshold);

        // CATCHWAIT.js checks up/down/back/forward in that exact order.
        if (freshUp) { CommittedThrow = MoveSlot.ThrowUp; ThrowFrame = 0; }
        else if (freshDown) { CommittedThrow = MoveSlot.ThrowDown; ThrowFrame = 0; }
        else if (freshBack) { CommittedThrow = MoveSlot.ThrowBack; ThrowFrame = 0; }
        else if (freshForward) { CommittedThrow = MoveSlot.ThrowForward; ThrowFrame = 0; }
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
        _appliedHitKey = -1;

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

        // Blaster fires its projectile on one specific action frame
        // (NEUTRALSPECIALGROUND.js: `timer === 12`). The flag is raised here and
        // consumed by CombatSystem, which owns the pool — see
        // PendingProjectileSpawn's doc comment on why PlayerMover doesn't hold it.
        if (CurrentMove == MoveSlot.NeutralB && MoveFrame == Characters.Fox.FoxMoves.BlasterFireFrame)
            PendingProjectileSpawn = true;

        // JAB COMBO. JAB1.js/JAB2.js both watch for a fresh A press during their
        // own window (`timer > 2 && timer < 32 && input[0].a && !input[1].a`) and
        // set phys.jabCombo, then chain on the next frame past 5. JAB3 is Fox's
        // rapid jab and re-loops itself (timer 43 -> 7) while A keeps being
        // pressed. Chaining here rather than through TryStartAttack is
        // deliberate: TryStartAttack is gated on not already being in a move,
        // and the whole point of a jab combo is that it chains FROM one.
        if (_jabComboQueued && MoveFrame > 5)
        {
            MoveSlot? next = CurrentMove switch
            {
                MoveSlot.Jab1 => MoveSlot.Jab2,
                MoveSlot.Jab2 => MoveSlot.Jab3,
                _ => null,
            };
            if (next.HasValue && _character.TryGetMove(next.Value, out _))
            {
                _jabComboQueued = false;
                CurrentMove = next.Value;
                MoveFrame = 1;
                _hitApplied = false;
                _appliedHitKey = -1;
                return;
            }
            _jabComboQueued = false;
        }

        MoveFrame++;
        if (MoveFrame > move.TotalFrames)
        {
            bool wasUpB = CurrentMove == MoveSlot.UpB;
            CurrentMove = null;
            MoveFrame = 0;
            _appliedHitKey = -1;

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
        w.WriteInt(_appliedHitKey);
        w.WriteBool(_jabComboQueued);
        w.WriteBool(PendingProjectileSpawn);
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
        _appliedHitKey = r.ReadInt();
        _jabComboQueued = r.ReadBool();
        PendingProjectileSpawn = r.ReadBool();
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