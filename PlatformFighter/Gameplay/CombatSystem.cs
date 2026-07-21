using PlatformFighter.Core.Combat;
using PlatformFighter.Core.Sim;
using PlatformFighter.Core.Math;
using PlatformFighter.Core.Sim.Collision;

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
/// Hit detection is whole-body-AABB-overlap, not real per-hitbox spatial
/// placement — PlayerMover's class doc comment already flags this as a known
/// simplification ("hurtbox-overlap hit detection instead of real spatial
/// hitboxes... hitbox position and size per move aren't extracted from the
/// data yet"). Good enough to prove hits connect and knockback/hitstun/hitlag
/// flow correctly end-to-end; real per-move hitbox offsets are follow-up work
/// (the offset data DOES exist in MeleeLight's attributes.js `offsets[...]`
/// tables this phase pulled from — just not wired through MoveDef yet).
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
    }

    /// <summary>
    /// Step 2: how far in FRONT of the attacker's own body a hit reaches, in
    /// MeleeLight units. A single flat constant standing in for real per-move
    /// hitbox offsets, which genuinely exist in MeleeLight's <c>offsets[...]</c>
    /// tables but are still not wired through MoveDef (the same gap Phase 9/10's
    /// notes already flag as the biggest remaining accuracy hole).
    ///
    /// WHY IT HAD TO EXIST NOW rather than later: before Step 2 the "hitbox" was
    /// the attacker's whole body, which was 40 units wide — accidentally about
    /// the right reach, for entirely the wrong reason. With the real ECB the body
    /// is 6 wide, so whole-body overlap would mean fighters had to be practically
    /// inside each other to connect. 8 units is roughly Fox's jab range and is a
    /// placeholder, not transcribed data.
    ///
    /// It is also strictly more correct than what it replaces in one way: reach
    /// now extends in the direction the attacker FACES, instead of symmetrically,
    /// so a jab no longer hits someone standing behind you.
    /// </summary>
    public static readonly Fx AttackReach = Fx.FromInt(8);

    private static void ResolveAttack(PlayerMover attacker, PlayerMover defender)
    {
        if (!attacker.TryGetActiveHitbox(out Hitbox hit)) return;
        // Directive Phase 1: a just-respawned fighter can't be re-hit — see
        // PlayerMover.IsInvincible / RespawnInvincibilityFrames.
        if (defender.IsInvincible) return;

        // Attacker's body, extended forward by AttackReach in its facing direction.
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
        var defenderBox = FxAabb.FromMinMax(
            new FxVec2(defender.Position.X - defender.HalfSize.X, defender.Position.Y - defender.Ecb.TopHeight),
            new FxVec2(defender.Position.X + defender.HalfSize.X, defender.Position.Y));
        if (!attackerBox.Overlaps(defenderBox)) return;

        attacker.MarkHitApplied();
        defender.ApplyHit(in hit, attacker.FacingRight, defender.Weight);

        int hitlagFrames = HitlagMath.ComputeHitlagFrames(hit.Damage);
        attacker.ApplyHitlag(hitlagFrames);
        defender.ApplyHitlag(hitlagFrames);
    }

    /// <summary>No independent state — everything ApplyHit/ApplyHitlag touched
    /// already lives in (and is saved by) the two PlayerMovers themselves.</summary>
    public void SaveState(StateWriter w) { }

    public void LoadState(StateReader r) { }
}
