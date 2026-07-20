using PlatformFighter.Core.Math;

namespace PlatformFighter.Core.Sim.Collision;

/// <summary>Result of one axis-separated collision resolve pass.</summary>
public struct CollisionResult
{
    public FxVec2 Position;
    public FxVec2 Velocity;
    public bool Grounded;
}

/// <summary>
/// Deterministic AABB-vs-stage resolution. Axis-separated (resolve X,
/// then Y against the X-resolved position) rather than swept — the
/// standard, simpler approach for fixed-timestep platformer physics, and
/// exact in fixed-point since there's no epsilon fudging: every compare
/// is an integer compare under the hood.
///
/// One-way platforms are only ever tested on the Y pass, and only when
/// the body's bottom edge was at-or-above the platform's surface last
/// tick and is at-or-below it this tick (i.e. it fell onto it this
/// frame) — standing on it, walking off the edge, or jumping up through
/// it from below all correctly fall through that single check.
///
/// KNOWN LIMITATION: this is discrete, not swept/continuous. A body
/// moving further than its own half-size in one tick can tunnel through
/// a thin solid. Fine for gravity/movement speeds; revisit if a future
/// phase introduces very high-speed knockback through solids.
/// </summary>
public static class CollisionResolver
{
    public static CollisionResult Resolve(
        FxVec2 previousPosition,
        FxVec2 movedPosition,
        FxVec2 velocity,
        FxVec2 halfSize,
        StageGeometry stage,
        bool passThroughPlatforms)
    {
        var result = new CollisionResult { Position = movedPosition, Velocity = velocity, Grounded = false };

        // ---- X axis (resolved against the OLD Y, X hasn't moved yet on Y) ----
        for (int i = 0; i < stage.Solids.Count; i++)
        {
            var solid = stage.Solids[i];
            var boxX = new FxAabb(new FxVec2(result.Position.X, previousPosition.Y), halfSize);
            if (!boxX.Overlaps(solid)) continue;

            if (result.Velocity.X > Fx.Zero && previousPosition.X + halfSize.X <= solid.Left)
            {
                result.Position = new FxVec2(solid.Left - halfSize.X, result.Position.Y);
                result.Velocity = new FxVec2(Fx.Zero, result.Velocity.Y);
            }
            else if (result.Velocity.X < Fx.Zero && previousPosition.X - halfSize.X >= solid.Right)
            {
                result.Position = new FxVec2(solid.Right + halfSize.X, result.Position.Y);
                result.Velocity = new FxVec2(Fx.Zero, result.Velocity.Y);
            }
        }

        // ---- Y axis (resolved against the X-resolved position) ----
        Fx prevBottom = previousPosition.Y + halfSize.Y;
        Fx prevTop    = previousPosition.Y - halfSize.Y;

        for (int i = 0; i < stage.Solids.Count; i++)
        {
            var solid = stage.Solids[i];
            var boxY = new FxAabb(result.Position, halfSize);
            if (!boxY.Overlaps(solid)) continue;

            if (result.Velocity.Y > Fx.Zero && prevBottom <= solid.Top)
            {
                result.Position = new FxVec2(result.Position.X, solid.Top - halfSize.Y);
                result.Velocity = new FxVec2(result.Velocity.X, Fx.Zero);
                result.Grounded = true;
            }
            else if (result.Velocity.Y < Fx.Zero && prevTop >= solid.Bottom)
            {
                result.Position = new FxVec2(result.Position.X, solid.Bottom + halfSize.Y);
                result.Velocity = new FxVec2(result.Velocity.X, Fx.Zero);
            }
        }

        if (!passThroughPlatforms)
        {
            for (int i = 0; i < stage.Platforms.Count; i++)
            {
                var plat = stage.Platforms[i];
                Fx bottom = result.Position.Y + halfSize.Y;
                bool withinX = result.Position.X + halfSize.X > plat.XMin && result.Position.X - halfSize.X < plat.XMax;
                bool fellOntoIt = result.Velocity.Y >= Fx.Zero && prevBottom <= plat.Y && bottom >= plat.Y;

                if (withinX && fellOntoIt)
                {
                    result.Position = new FxVec2(result.Position.X, plat.Y - halfSize.Y);
                    result.Velocity = new FxVec2(result.Velocity.X, Fx.Zero);
                    result.Grounded = true;
                }
            }
        }

        return result;
    }
}
