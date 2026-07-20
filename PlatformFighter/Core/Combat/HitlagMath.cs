using PlatformFighter.Core.Math;

namespace PlatformFighter.Core.Combat;

/// <summary>
/// Phase 7 addition: hitlag (the brief freeze both fighters experience the
/// instant a hit connects). Nothing in the engine computed this before —
/// COMBAT.md's "What's explicitly NOT here yet" list named it explicitly.
/// Ported from MeleeLight's <c>src/physics/hitDetection.js</c>, where it's
/// applied identically on every regular-hit code path:
///
///   hitlag = floor(damage / 3 + 3)
///
/// This is frames of INPUT-FROZEN game state for both attacker and defender
/// (attacker's hitlag can differ slightly on some hit types in real Melee —
/// e.g. multi-hit moves, projectiles — MeleeLight's own comment block flags
/// "TODO: STALING + KNOCKBACK STACKING" right above this formula, so treat
/// this as the base case, not the full picture).
///
/// WIRED IN as of Phase 9: `Gameplay/CombatSystem.cs`'s `ApplyHit` calls this
/// directly on connect and freezes both movers via `PlayerMover.ApplyHitlag`
/// for the result — this doc comment previously said "nothing calls this,"
/// true when written (Phase 7, before CombatSystem existed) but stale since;
/// left the wrong claim in place instead of silently deleting it, per this
/// codebase's own convention of flagging corrections rather than erasing the
/// history of what was true when. Freezing happens via `HitlagFramesRemaining`
/// gating `Tick()`, not by CombatSystem calling `Tick()` differently.
/// </summary>
public static class HitlagMath
{
    private static readonly Fx Three = Fx.FromInt(3);

    public static int ComputeHitlagFrames(Fx damage)
    {
        Fx raw = damage / Three + Three;
        int frames = raw.ToIntFloor();
        return frames < 0 ? 0 : frames;
    }
}
