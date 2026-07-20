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

    public Fx Left   => Center.X - HalfSize.X;
    public Fx Right  => Center.X + HalfSize.X;
    public Fx Top    => Center.Y - HalfSize.Y;
    public Fx Bottom => Center.Y + HalfSize.Y;

    /// <summary>Standard separating-axis overlap test.</summary>
    public bool Overlaps(FxAabb other) =>
        Left < other.Right && Right > other.Left &&
        Top < other.Bottom && Bottom > other.Top;
}
