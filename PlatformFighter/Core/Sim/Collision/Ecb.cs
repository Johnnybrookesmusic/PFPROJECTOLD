using PlatformFighter.Core.Math;

namespace PlatformFighter.Core.Sim.Collision;

/// <summary>
/// Step 2 of the Melee Lite Translation Directive: the Environmental Collision
/// Box — the diamond Melee actually collides against, replacing this engine's
/// centre+halfsize <see cref="FxAabb"/> for fighter-vs-stage resolution.
///
/// SHAPE, transcribed from MeleeLight's own construction in
/// <c>src/physics/physics.js</c> (the <c>player[i].phys.ECBp = [...]</c> block,
/// ~line 1097) — four points, NOT a box:
///   [0] bottom = (pos.x,                pos.y + offset[0])   // offset[0] used only while airborne
///   [1] right  = (pos.x + offset[1],    pos.y + offset[2])
///   [2] top    = (pos.x,                pos.y + offset[3])
///   [3] left   = (pos.x - offset[1],    pos.y + offset[2])
/// so a raw ECB entry <c>[bottomY, halfWidth, midY, topY]</c> — Fox's WAIT is
/// <c>[4,3,9,13]</c> (see Characters/Fox/FoxEcb.cs).
///
/// TWO CONVENTION ADAPTATIONS, both consistent with what this codebase already
/// does elsewhere rather than new inventions:
///
///  1. ORIGIN IS THE FEET, not the centre. MeleeLight's <c>phys.pos</c> is the
///     ECB's bottom point when grounded (the block above adds 0 to pos.y for
///     the bottom point while grounded), which is why its ground snap is
///     "set pos.y so the bottom point sits on the surface." This engine's
///     previous <see cref="FxAabb"/> bodies were centre-origin. Feet-origin is
///     what the real algorithm assumes at every step, and it is also what makes
///     sloped-ground following expressible at all, so this type adopts it.
///     PlayerMover.Position therefore means "where the feet are" from Step 2 on.
///
///  2. Y IS DOWN, as everywhere else in this engine (see FxVec2.cs's doc
///     comment) — MeleeLight is Y-up. Every Y offset below is therefore
///     SUBTRACTED from the feet position rather than added, exactly the same
///     negation Stages/Battlefield.cs applied to the stage data in Step 1.
///     Concretely: a fighter standing at Y=0 has its head at Y=-13, not +13.
///
/// SCOPE, stated plainly: this struct is the ECB's SHAPE and its derived query
/// points. It is not the full MeleeLight collision algorithm — see
/// SegmentCollisionResolver.cs's own header for exactly which parts of
/// <c>environmentalCollision.js</c> are and are not ported in Step 2.
///
/// PER-FRAME ECB ANIMATION IS NOT PORTED. MeleeLight has a distinct ECB per
/// action state PER FRAME (Fox's ecb.js is one array of 4-tuples per state);
/// this engine has no animation system yet (Phase 8), so there is no frame
/// cursor to index with. Step 2 uses one static ECB per character —
/// deliberately Fox's WAIT/FALL value, which is his most common shape — and
/// flags the rest as Phase 8's job. The struct is shaped to accept per-frame
/// values later without changing its callers.
/// </summary>
public readonly struct Ecb
{
    /// <summary>
    /// How far the ECB's bottom point RISES above the feet while airborne —
    /// MeleeLight's offset[0], applied only when airborne. Melee characters tuck
    /// their legs up off the ground when airborne, so this moves the collision
    /// point UP, not down.
    ///
    /// SIGN CORRECTION (this was shipped backwards once — do not re-flip it):
    /// MeleeLight is Y-UP and builds the point as <c>pos.y + offset[0]</c>, which
    /// in Y-UP means ABOVE the origin. Translating "+" literally into this
    /// engine's Y-DOWN space put the airborne bottom BELOW the feet, which meant a
    /// fighter spawned level with the ground started already through the floor and
    /// could never register a downward crossing — it fell forever.
    ///
    /// NOT CURRENTLY USED for ground crossing — see <see cref="Bottom"/>.
    /// </summary>
    public readonly Fx AirborneBottomRise;

    /// <summary>Half-width — MeleeLight's offset[1]. The left/right points sit
    /// this far either side of the origin's X.</summary>
    public readonly Fx HalfWidth;

    /// <summary>Height of the left/right points above the feet — MeleeLight's
    /// offset[2]. This is the ECB's widest line, not its middle by height.</summary>
    public readonly Fx SideHeight;

    /// <summary>Height of the top point above the feet — MeleeLight's offset[3].</summary>
    public readonly Fx TopHeight;

    public Ecb(Fx airborneBottomRise, Fx halfWidth, Fx sideHeight, Fx topHeight)
    {
        AirborneBottomRise = airborneBottomRise;
        HalfWidth = halfWidth;
        SideHeight = sideHeight;
        TopHeight = topHeight;
    }

    /// <summary>
    /// The collision bottom point: the FEET, grounded or airborne.
    ///
    /// <see cref="AirborneBottomRise"/> is deliberately NOT applied here. It is a
    /// refinement that only pays off inside the full ECB-corner sweep
    /// (environmentalCollision.js), which this engine has not ported — see
    /// SegmentCollisionResolver.cs's header. Applying it in isolation buys
    /// nothing and costs something real: landing would trigger while the feet
    /// were still short of the surface, then snap them down onto it, i.e. a
    /// visible pop on every touchdown. Using the feet is exact, has no pop, and
    /// makes "spawn standing on the ground" work without special-casing.
    ///
    /// The parameter is kept so callers keep declaring their grounded state at
    /// the call site, and so wiring the rise back in (when the sweep lands) is a
    /// one-line change here rather than a hunt through every caller.
    /// </summary>
    public FxVec2 Bottom(FxVec2 origin, bool grounded) => origin;

    /// <summary>Top point — head. Y-down, so this is ABOVE the origin.</summary>
    public FxVec2 Top(FxVec2 origin) => new(origin.X, origin.Y - TopHeight);

    public FxVec2 Right(FxVec2 origin) => new(origin.X + HalfWidth, origin.Y - SideHeight);
    public FxVec2 Left(FxVec2 origin) => new(origin.X - HalfWidth, origin.Y - SideHeight);

    /// <summary>
    /// The ECB's bounding box, in the same feet-origin space. Used by the
    /// parts of Step 2's resolver that are deliberately box-approximate
    /// (walls and ceilings — see SegmentCollisionResolver.cs's header), and by
    /// combat's hurtbox overlap until real per-hitbox offsets land.
    /// Ground resolution does NOT use this — it uses the true bottom point,
    /// because "which surface am I standing on" is a point question, not a
    /// box question, and answering it with a box is what makes fighters
    /// hover at ledges.
    /// </summary>
    public FxAabb BoundingBox(FxVec2 origin, bool grounded)
    {
        Fx bottomY = origin.Y;
        Fx topY = origin.Y - TopHeight;
        var half = new FxVec2(HalfWidth, (bottomY - topY) / (Fx.One + Fx.One));
        return new FxAabb(new FxVec2(origin.X, topY + half.Y), half);
    }
}
