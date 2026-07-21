using PlatformFighter.Core.Math;
using PlatformFighter.Core.Sim.Collision;

namespace PlatformFighter.Characters.Fox;

/// <summary>
/// Fox's real Environmental Collision Box, transcribed from MeleeLight's
/// <c>src/characters/fox/ecb.js</c>. Raw entries there are 4-tuples
/// <c>[bottomDrop, halfWidth, sideHeight, topHeight]</c> — see Ecb.cs for how
/// MeleeLight's own physics.js turns those into the four diamond points, and
/// for this engine's feet-origin / Y-down adaptations.
///
/// Fox's <c>ecbScale</c> is 1 in attributes.js (the commented-out 2.5 above it
/// is dead), so these are used as-is with no scaling.
///
/// WHY THIS FILE EXISTS AT ALL — the Step 2 headline fix:
/// <c>CharacterPhysics.FromFox()</c> shipped <c>HalfSize = (20, 30)</c>, a
/// pixel-scale placeholder inherited from before real stage data existed. Once
/// Step 1 transcribed Battlefield in MeleeLight's native units (ground spans
/// ±68.4), that placeholder made Fox <b>40 units wide on a 136.8-wide stage —
/// 29% of the entire stage</b>, against a true figure of 4.4%. Every distance
/// question in the engine was wrong by roughly 6.7×: spacing, hit reach,
/// how much of a platform a fighter covers, whether a ledge is even reachable.
///
/// The previous pass diagnosed this correctly and then reverted, because
/// swapping HalfSize alone broke DeterminismTest's combat check (P1/P2 spawn a
/// fixed 30 apart, which only lands a hit at the inflated size). That reasoning
/// was right — a half-measure IS worse — but the conclusion should have been
/// "fix the spawn distance too," not "keep the wrong body size." Velocities,
/// gravity, and the stage were ALREADY in MeleeLight units and internally
/// consistent (Fox dashes across Battlefield in ~68 ticks, which is correct);
/// body size was the sole outlier, so there is no whole-engine rescale to do.
/// The spawn distances in DeterminismTest/Main are the thing that must move.
///
/// RENDER SCALE, since this makes the sim numerically small: MeleeLight draws
/// Battlefield with <c>scale: 4.5, offset: [600, 480]</c> (battlefield.js —
/// real fields, confirmed unused by physics.js and consumed only by
/// stagerender.js). That is the authoritative sim-units-to-screen-pixels
/// transform for this stage; the render layer should multiply by it rather
/// than the sim inflating itself to suit a camera.
/// </summary>
public static class FoxEcb
{
    /// <summary>MeleeLight <c>WAIT</c>: [4,3,9,13]. Fox's standing/neutral shape
    /// and by far his most common one — used as the single static ECB for all
    /// states in Step 2, since no animation system exists yet to index the real
    /// per-frame arrays (see Ecb.cs's note on per-frame ECB not being ported).</summary>
    public static readonly Ecb Wait = new(
        airborneBottomRise: Fx.FromInt(4),
        halfWidth: Fx.FromInt(3),
        sideHeight: Fx.FromInt(9),
        topHeight: Fx.FromInt(13));

    /// <summary>MeleeLight <c>FALL</c>: [3,3,8,13]. Kept transcribed and ready
    /// for whenever state-dependent ECBs land; nothing selects it yet, and it
    /// is close enough to <see cref="Wait"/> that using one for both is not
    /// what will be wrong with this engine's feel.</summary>
    public static readonly Ecb Fall = new(
        airborneBottomRise: Fx.FromInt(3),
        halfWidth: Fx.FromInt(3),
        sideHeight: Fx.FromInt(8),
        topHeight: Fx.FromInt(13));

    /// <summary>The shape Step 2 actually uses everywhere.</summary>
    public static readonly Ecb Default = Wait;
}
