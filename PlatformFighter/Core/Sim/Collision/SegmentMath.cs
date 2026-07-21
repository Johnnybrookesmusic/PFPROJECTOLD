using PlatformFighter.Core.Math;

namespace PlatformFighter.Core.Sim.Collision;

/// <summary>
/// Step 2: fixed-point geometry on <see cref="FxSegment"/>. Step 1 added the
/// segment TYPE but nothing that could ask questions of it; a resolver needs
/// exactly these three, so they live together here rather than being scattered
/// as private helpers inside the resolver.
///
/// This is the fixed-point counterpart of MeleeLight's <c>coordinateIntercept</c>
/// / <c>extremePoint</c> / <c>lineAngle</c> family in
/// <c>src/main/util/</c> + <c>physics/environmentalCollision.js</c>. It is NOT
/// a port of their trig: <see cref="Fx"/> has no sin/cos/atan2 (stated in
/// Fx.cs), so anything MeleeLight expresses as an angle is expressed here as a
/// sign test or a slope ratio instead. Where that changes behaviour rather than
/// just notation, the calling code says so.
///
/// DETERMINISM: every operation below is Fx add/sub/mul/div only — no floats,
/// no epsilons, no tolerance fudging. Division is the only lossy step and it is
/// lossy identically on every machine, which is the property that matters.
/// </summary>
public static class SegmentMath
{
    /// <summary>True if <paramref name="x"/> lies within the segment's X span,
    /// endpoints included. Mirrors the <c>ECBp[0].x &lt; leftmostGroundPoint.x</c>
    /// / <c>&gt; rightmostGroundPoint.x</c> bounds test that drives MeleeLight's
    /// walk-off-the-edge decision in <c>dealWithGround</c>.</summary>
    public static bool SpansX(in FxSegment seg, Fx x) => x >= seg.MinX && x <= seg.MaxX;

    /// <summary>
    /// The segment's Y at a given X — i.e. "how high is the floor under me."
    /// This is MeleeLight's <c>coordinateIntercept</c> specialised to a vertical
    /// probe line, which is the only way <c>dealWithGround</c> ever uses it
    /// (it intersects the ground against <c>[ecbpBottom, ecbpBottom + (0,1)]</c>).
    ///
    /// Returns false for a vertical segment (A.X == B.X): "the Y at this X" is
    /// not a single value there, and a wall is never something you stand on, so
    /// callers should be querying <see cref="Ground"/>-list segments only.
    /// Guarding rather than dividing by zero is deliberate — Fx division by a
    /// zero raw would throw, and a crash at a ledge is a worse failure than a
    /// miss.
    /// </summary>
    public static bool TryYAt(in FxSegment seg, Fx x, out Fx y)
    {
        Fx dx = seg.B.X - seg.A.X;
        if (dx == Fx.Zero)
        {
            y = Fx.Zero;
            return false;
        }

        Fx t = (x - seg.A.X) / dx;
        y = seg.A.Y + (seg.B.Y - seg.A.Y) * t;
        return true;
    }

    /// <summary>
    /// Twice the signed area of triangle (a, b, p) — the 2D cross product
    /// (b-a) × (p-a). Sign tells which side of the directed line a→b the point
    /// p falls on. In this engine's Y-DOWN space a POSITIVE result means p is
    /// below-or-right of the line as drawn; callers should compare signs
    /// between two points rather than reasoning about absolute sign, which is
    /// the part that survives the Y-flip without needing to be re-derived.
    ///
    /// Used instead of MeleeLight's angle comparisons (see class header): its
    /// "is the ECB on the blocking side of this wall" checks reduce to a sign
    /// test, which is exact in fixed point where an angle would not be.
    /// </summary>
    public static Fx Side(in FxVec2 a, in FxVec2 b, in FxVec2 p) =>
        (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
}
