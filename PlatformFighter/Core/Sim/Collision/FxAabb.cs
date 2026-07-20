using PlatformFighter.Core.Math;

namespace PlatformFighter.Core.Sim.Collision;

/// <summary>
/// Axis-aligned bounding box in fixed-point sim space. Center + half-size,
/// matching FxVec2's convention: +X right, +Y DOWN. "Top" is therefore
/// the smaller-Y edge (visually higher on screen) and "Bottom" is the
/// larger-Y edge — where a grounded character's feet rest.
/// </summary>
public readonly struct FxAabb
{
    public readonly FxVec2 Center;
    public readonly FxVec2 HalfSize;

    public FxAabb(FxVec2 center, FxVec2 halfSize)
    {
        Center = center;
        HalfSize = halfSize;
    }

    /// <summary>Builds from two opposite corners rather than center+halfsize —
    /// convenient for data that's naturally authored as min/max, like a stage's
    /// blast zone (MeleeLight's <c>Box2D(min, max)</c>; see Stages/Battlefield.cs).
    /// Works regardless of which corner is passed as which argument.</summary>
    public static FxAabb FromMinMax(FxVec2 cornerA, FxVec2 cornerB)
    {
        Fx left = Fx.Min(cornerA.X, cornerB.X);
        Fx right = Fx.Max(cornerA.X, cornerB.X);
        Fx top = Fx.Min(cornerA.Y, cornerB.Y);
        Fx bottom = Fx.Max(cornerA.Y, cornerB.Y);
        var half = new FxVec2((right - left) / (Fx.One + Fx.One), (bottom - top) / (Fx.One + Fx.One));
        var center = new FxVec2(left + half.X, top + half.Y);
        return new FxAabb(center, half);
    }

    public Fx Left   => Center.X - HalfSize.X;
    public Fx Right  => Center.X + HalfSize.X;
    public Fx Top    => Center.Y - HalfSize.Y;
    public Fx Bottom => Center.Y + HalfSize.Y;

    /// <summary>Standard separating-axis overlap test.</summary>
    public bool Overlaps(FxAabb other) =>
        Left < other.Right && Right > other.Left &&
        Top < other.Bottom && Bottom > other.Top;

    /// <summary>Point-in-box test (inclusive of the edges) — e.g. "is this
    /// fighter's position still inside the blast zone box."</summary>
    public bool Contains(FxVec2 point) =>
        point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
}
