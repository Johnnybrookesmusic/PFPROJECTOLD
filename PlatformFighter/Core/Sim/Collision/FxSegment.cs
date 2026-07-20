using PlatformFighter.Core.Math;

namespace PlatformFighter.Core.Sim.Collision;

/// <summary>
/// A single 2-point line segment in fixed-point sim space. Added for Step 1
/// of the Melee Lite Translation Directive (Battlefield collision + blast
/// zones): real MeleeLight stage geometry (<c>src/stages/vs-stages/
/// battlefield.js</c>) represents ground/ceiling/wallL/wallR as arrays of
/// exactly this shape — <c>[Vec2D, Vec2D]</c> pairs, most of them NOT
/// axis-aligned (e.g. Battlefield's underside chamfers are all diagonal) —
/// which <see cref="FxAabb"/> cannot represent at all. One-way platforms are
/// the one piece of real stage geometry that IS always a flat horizontal
/// line in Melee (confirmed: all three of Battlefield's platform entries
/// share the same Y for both endpoints), so those still use the existing
/// <see cref="OneWayPlatform"/> struct rather than this one — no need to
/// generalize what real data never asks for.
///
/// SCOPE NOTE (see Stages/Battlefield.cs and the Phase roadmap entry for
/// this pass): this struct and the segment lists on <see cref="StageGeometry"/>
/// that use it are DATA only. The real MeleeLight ground-collision algorithm
/// (src/physics/physics.js: dealWithGround/fallOffGround/moveAlongGround) is
/// an ECB-corner-vs-segment resolver with edge-to-edge connectivity, teetering,
/// and ledge cancels — a fundamentally different model from this engine's
/// current CollisionResolver (an AABB-vs-solid box resolver). Porting that
/// resolution algorithm is Step 2's job (Core physics), not this one. Storing
/// the real segments now means Step 2 has real data to resolve against
/// instead of inventing placeholder geometry first and fixing it twice.
/// </summary>
public readonly struct FxSegment
{
    public readonly FxVec2 A;
    public readonly FxVec2 B;

    public FxSegment(FxVec2 a, FxVec2 b)
    {
        A = a;
        B = b;
    }

    /// <summary>Smaller-X endpoint's X coordinate.</summary>
    public Fx MinX => Fx.Min(A.X, B.X);
    /// <summary>Larger-X endpoint's X coordinate.</summary>
    public Fx MaxX => Fx.Max(A.X, B.X);

    /// <summary>True if both endpoints share the same Y (a flat segment —
    /// every real Melee ground/platform piece is one of these; ceiling and
    /// wall pieces generally are not).</summary>
    public bool IsHorizontal => A.Y == B.Y;
}
