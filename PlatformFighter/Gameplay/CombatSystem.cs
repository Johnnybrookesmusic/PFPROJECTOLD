using PlatformFighter.Core.Combat;
using PlatformFighter.Core.Sim;
using PlatformFighter.Core.Math;
using PlatformFighter.Core.Sim.Collision;
using PlatformFighter.Characters;
using PlatformFighter.Characters.Fox;

namespace PlatformFighter.Gameplay;

/// <summary>
/// Phase 9: the missing piece PlayerMover's own doc comment already named —
/// "consumed by Gameplay/CombatSystem.cs" (see TryGetActiveHitbox). Runs as
/// its own ISimObject, registered in SimWorld AFTER both PlayerMovers it
/// watches (Main.cs controls registration order, which IS tick order — see
/// SimWorld's "ordered list => deterministic tick order" comment), so by the
/// time this ticks, both movers have already advanced their own attack-state
/// for the frame and TryGetActiveHitbox reflects this tick's real state.
///
/// Hit detection: REAL per-move spatial hitboxes (Directive Phase 1 — was the
/// single biggest flagged accuracy gap, see Docs/CURRENT_GOAL.md's history).
/// Each MoveDef that has ported spatial data (Hitbox.OffsetX/OffsetY/Radius,
/// from MeleeLight's attributes.js `offsets[...]` tables — see Hitbox.cs and
/// Characters/Fox/FoxMoves.cs) gets a real circle-vs-defender-AABB check at
/// the attacker-relative offset. Moves that don't have spatial data ported
/// yet (Hitbox.Radius == Fx.Zero — currently just Fox's Illusion, see its own
/// doc comment in FoxMoves.cs) fall back to the old flat whole-body-plus-
/// AttackReach box rather than silently going undetectable.
///
/// Both attacker->defender checks run every tick (A hits B, then B hits A) so
/// two attacks can trade in the same frame, same as real Melee.
///
/// KNOWN GAP: registered in SimObjectTypes with a factory that throws — this
/// object's only state is two PlayerMover REFERENCES, which a from-scratch
/// snapshot rebuild (RestoreSnapshot growing the object list past what's
/// already live) has no way to supply generically yet. Every actual call site
/// today (Main.cs, F9 tests) constructs CombatSystem explicit-reference style
/// alongside its two PlayerMovers up front, so LoadState's "reuse the live
/// object at this index" path is what always fires in practice — this is a
/// documented gap for whenever real rollback/respawn (Phase 16) needs the
/// cold-rebuild path, not a silent one.
/// </summary>
public sealed class CombatSystem : ISimObject
{
    public const int TypeIdValue = 4;
    public int TypeId => TypeIdValue;

    private readonly PlayerMover _a;
    private readonly PlayerMover _b;

    public CombatSystem(PlayerMover a, PlayerMover b)
    {
        _a = a;
        _b = b;
    }

    public void Tick(SimWorld world)
    {
        ResolveAttack(_a, _b);
        ResolveAttack(_b, _a);
        TickGrabs();
        TickProjectiles();
    }

    /// <summary>
    /// Per-tick grab maintenance — the half of the grab/throw system that
    /// needs both PlayerMovers at once (PlayerMover's own Tick() already
    /// advanced each side's OWN half: victim's stuck-timer/mash in
    /// TickGrabbed, attacker's hold/pummel-count/throw-commit in
    /// TickGrabbing — see PlayerMover.cs's grab section doc comment).
    ///
    /// Runs AFTER both ResolveAttack calls (so a grab that connects THIS tick
    /// is pinned/resolved starting next tick, same one-tick-delay every other
    /// cross-player effect in this class already has) and BEFORE projectiles.
    /// </summary>
    private void TickGrabs()
    {
        MaintainGrabPair(_a, _b);
        MaintainGrabPair(_b, _a);
    }

    private static void MaintainGrabPair(PlayerMover attacker, PlayerMover defender)
    {
        if (!attacker.IsGrabbingOpponent) return;

        // Victim escaped (mash) or was released by a throw already processed
        // this tick — real Melee's CATCHCUT triggering on the attacker
        // whenever CAPTURECUT triggers on the victim.
        if (!defender.IsGrabbedByOpponent)
        {
            attacker.ReleaseGrab();
            return;
        }

        // Real CAPTUREWAIT.js: victim.face = -attacker.face (set once, at
        // connect, in the real source's CAPTUREPULLED.init — applied here
        // every tick instead, which is harmless since neither side's facing
        // changes again while held) and victim.pos.x is force-set to
        // attacker.pos.x + 9.04298*attacker.face every single frame
        // (-9.04298*victim.face, and victim.face == -attacker.face).
        defender.FacingRight = !attacker.FacingRight;
        Fx holdOffset = Fx.Ratio(9_042_980, 1_000_000);
        Fx faceSign = attacker.FacingRight ? Fx.FromInt(1) : Fx.FromInt(-1);
        defender.Position = new FxVec2(attacker.Position.X + holdOffset * faceSign, attacker.Position.Y);

        // Pummel's real active frame (CATCHATTACK.js: timer === 10).
        if (attacker.PummelFrame == 10)
        {
            defender.ApplyPummelDamage(FoxMoves.Pummel.Damage);
        }

        // Throw release: compare ThrowFrame against each throw's real,
        // verified release frame (see FoxMoves.cs's ThrowXReleaseFrame
        // constants for the derivation from THROW*.js).
        if (attacker.CommittedThrow.HasValue)
        {
            int releaseFrame = attacker.CommittedThrow.Value switch
            {
                MoveSlot.ThrowUp => FoxMoves.ThrowUpReleaseFrame,
                MoveSlot.ThrowBack => FoxMoves.ThrowBackReleaseFrame,
                MoveSlot.ThrowForward => FoxMoves.ThrowForwardReleaseFrame,
                MoveSlot.ThrowDown => FoxMoves.ThrowDownReleaseFrame,
                _ => int.MaxValue,
            };
            if (attacker.ThrowFrame >= releaseFrame)
            {
                MoveDef throwMove = attacker.CommittedThrow.Value switch
                {
                    MoveSlot.ThrowUp => FoxMoves.ThrowUp,
                    MoveSlot.ThrowBack => FoxMoves.ThrowBack,
                    MoveSlot.ThrowForward => FoxMoves.ThrowForward,
                    MoveSlot.ThrowDown => FoxMoves.ThrowDown,
                    _ => FoxMoves.ThrowForward,
                };
                Hitbox throwHit = throwMove.ToHitbox();
                // ApplyHit clears IsGrabbedByOpponent on the defender itself;
                // this same tick's earlier-in-the-loop check on the ATTACKER
                // side won't see that until next tick, so release explicitly
                // here too rather than waiting a tick.
                defender.ApplyHit(in throwHit, attacker.FacingRight, defender.Weight);
                attacker.ReleaseGrab();
            }
        }
    }

    /// <summary>
    /// Step 2: how far in FRONT of the attacker's own body the LEGACY fallback
    /// hitbox reaches, in MeleeLight units. Still used for any move whose
    /// Hitbox.Radius is Fx.Zero (no real spatial data ported yet — see
    /// Hitbox.cs's doc comment) so those moves stay detectable instead of
    /// silently doing nothing; moves with real ported offset/radius data no
    /// longer touch this constant at all. 8 units is roughly Fox's jab range
    /// and is a placeholder, not transcribed data — kept only as a fallback.
    /// </summary>
    public static readonly Fx AttackReach = Fx.FromInt(8);

    /// <summary>
    /// Live projectiles. Owned here rather than by PlayerMover so that every
    /// damage source — fighter hitboxes and projectiles alike — resolves in one
    /// place and in one deterministic order. See Gameplay/Projectile.cs.
    /// </summary>
    public readonly ProjectilePool Projectiles = new();

    /// <summary>
    /// Spawn any projectile a fighter asked for this tick, advance the ones
    /// already in flight, then resolve them against the opposing fighter.
    /// Called after both fighters' own attacks resolve, so a shot fired this
    /// frame does not also travel and connect on the same frame — matching
    /// MeleeLight, where article.LASER.init pushes the article and its main()
    /// runs from the next tick's article loop.
    /// </summary>
    private void TickProjectiles()
    {
        TrySpawnFor(_a, 0);
        TrySpawnFor(_b, 1);

        Projectiles.Tick(_a.Stage);

        for (int i = 0; i < ProjectilePool.Capacity; i++)
        {
            ref Projectile shot = ref Projectiles[i];
            if (!shot.Active) continue;

            PlayerMover target = shot.OwnerIndex == 0 ? _b : _a;
            if (target.IsInvincible || target.IsEliminated) continue;

            var targetBox = FxAabb.FromMinMax(
                new FxVec2(target.Position.X - target.HalfSize.X, target.Position.Y - target.Ecb.TopHeight),
                new FxVec2(target.Position.X + target.HalfSize.X, target.Position.Y));

            if (!CircleOverlapsAabb(shot.Position, ProjectileSpec.FoxLaser.Radius, targetBox)) continue;

            Hitbox laserHit = ProjectileSpec.FoxLaser.ToHitbox();
            target.ApplyHit(in laserHit, shot.MovingRight, target.Weight);

            // Fox's laser causes no hitlag either — with zero knockback there is
            // nothing to freeze for, and adding hitlag would interrupt the
            // shooter's own follow-up, which is precisely what makes Fox's
            // laser different from Falco's. See ProjectileSpec.FoxLaser.
            if (ProjectileSpec.FoxLaser.DestroyOnHit) shot.Deactivate();
        }
    }

    private void TrySpawnFor(PlayerMover shooter, int ownerIndex)
    {
        if (!shooter.PendingProjectileSpawn) return;
        shooter.ConsumeProjectileSpawn();
        Projectiles.TrySpawn(in ProjectileSpec.FoxLaser, shooter.Position, shooter.FacingRight, ownerIndex);
    }

    private static void ResolveAttack(PlayerMover attacker, PlayerMover defender)
    {
        // Directive Phase 1: a just-respawned fighter can't be re-hit — see
        // PlayerMover.IsInvincible / RespawnInvincibilityFrames.
        if (defender.IsInvincible) return;

        var defenderBox = FxAabb.FromMinMax(
            new FxVec2(defender.Position.X - defender.HalfSize.X, defender.Position.Y - defender.Ecb.TopHeight),
            new FxVec2(defender.Position.X + defender.HalfSize.X, defender.Position.Y));

        // Walk EVERY live hitbox of the current move, not just the first. Fox's
        // up-tilt has four boxes at four offsets; if only box 0 were tested the
        // move would whiff at ranges the other three cover. First box that
        // actually overlaps wins, which is what MeleeLight's own hit loop does.
        int slots = attacker.ActiveHitboxSlots;
        for (int i = 0; i < slots; i++)
        {
            if (!attacker.TryGetActiveHitbox(i, out Hitbox hit, out int hitKey)) continue;

            // NON-DAMAGING HITBOX TYPES. MeleeLight tags every hitbox with a
            // `type` (see Hitbox.HitType); three of them are not attacks:
            //   2 grab    — now handled for real, see below (was skipped).
            //   7 reflect — the reflector's reflect field, not its hit.
            //   8 inert   — explicitly does nothing.
            if (hit.IsGrab)
            {
                // executeGrabHits' real guard: neither side already grabbing/
                // grabbed. Grab-tech (two opposing grabs same tick) is NOT
                // ported — see PlayerMover's grab-machinery doc comment.
                if (!attacker.IsGrabbingOpponent && !defender.IsGrabbedByOpponent
                    && !attacker.IsGrabbedByOpponent && !defender.IsGrabbingOpponent)
                {
                    attacker.MarkHitApplied(hitKey);
                    defender.EnterGrabbed();
                    attacker.EnterGrabbing();
                    // Real Melee: grabs cause no hitlag (executeGrabHits never
                    // touches hit.hitlag — only executeRegularHit does).
                }
                return;
            }
            if (hit.IsReflector || hit.HitType == Hitbox.TypeInert) continue;

            bool hitConnects;
            if (hit.Radius > Fx.Zero)
            {
                // Real per-move spatial hitbox: attacker.Position + the ported
                // offset (X mirrored by facing, same convention as DirX — see
                // Hitbox.cs), checked as a circle of the ported Radius against
                // the defender's existing hurtbox AABB.
                Fx mirroredOffsetX = attacker.FacingRight ? hit.OffsetX : -hit.OffsetX;
                var hitboxCenter = new FxVec2(
                    attacker.Position.X + mirroredOffsetX,
                    attacker.Position.Y + hit.OffsetY);
                hitConnects = CircleOverlapsAabb(hitboxCenter, hit.Radius, defenderBox);
            }
            else
            {
                // Legacy fallback (see AttackReach's doc comment): attacker's
                // whole body extended forward by AttackReach.
                Fx half = attacker.HalfSize.X;
                Fx front = attacker.FacingRight
                    ? attacker.Position.X + half + AttackReach
                    : attacker.Position.X - half - AttackReach;
                Fx back = attacker.FacingRight
                    ? attacker.Position.X - half
                    : attacker.Position.X + half;

                var attackerBox = FxAabb.FromMinMax(
                    new FxVec2(Fx.Min(front, back), attacker.Position.Y - attacker.Ecb.TopHeight),
                    new FxVec2(Fx.Max(front, back), attacker.Position.Y));
                hitConnects = attackerBox.Overlaps(defenderBox);
            }

            if (!hitConnects) continue;

            attacker.MarkHitApplied(hitKey);
            defender.ApplyHit(in hit, attacker.FacingRight, defender.Weight);

            int hitlagFrames = HitlagMath.ComputeHitlagFrames(hit.Damage);
            attacker.ApplyHitlag(hitlagFrames);
            defender.ApplyHitlag(hitlagFrames);
            return;
        }
    }

    /// <summary>Standard circle-vs-AABB overlap: clamp the circle's center into
    /// the box, then compare the (squared) distance to that clamped point
    /// against the (squared) radius — avoids needing an Fx square-root.</summary>
    private static bool CircleOverlapsAabb(FxVec2 center, Fx radius, FxAabb box)
    {
        Fx closestX = Fx.Clamp(center.X, box.Left, box.Right);
        Fx closestY = Fx.Clamp(center.Y, box.Top, box.Bottom);
        Fx dx = center.X - closestX;
        Fx dy = center.Y - closestY;
        Fx distSq = dx * dx + dy * dy;
        return distSq <= radius * radius;
    }

    /// <summary>No independent state — everything ApplyHit/ApplyHitlag touched
    /// already lives in (and is saved by) the two PlayerMovers themselves.</summary>
    /// <summary>The projectile pool IS sim state — a laser in flight has a
    /// position and a lifetime that a rollback must restore exactly. Every slot
    /// serializes unconditionally so the layout is a fixed size regardless of
    /// how many shots are live; see ProjectilePool's doc comment.</summary>
    public void SaveState(StateWriter w) => Projectiles.SaveState(w);

    public void LoadState(StateReader r) => Projectiles.LoadState(r);
}