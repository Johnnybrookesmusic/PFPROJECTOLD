using PlatformFighter.Core.Math;
using PlatformFighter.Core.Sim.Collision;

namespace PlatformFighter.Debug;

/// <summary>
/// Minimal FD-style debug stage: one wide solid floor, no walls or
/// ceiling, plus a single Battlefield-style one-way platform above it.
/// Exists purely to exercise CollisionResolver in TestBody and
/// DeterminismTest — NOT a real Stages/ asset (Stages/Battlefield.cs is the
/// real one, and Main.cs uses it as of Step 2).
///
/// STILL IN PIXEL UNITS, deliberately: the Phase 4 TestBody tests assert exact
/// resting positions against these numbers (y=450 floor, y=300 platform), and
/// rescaling them to MeleeLight units would invalidate a set of tests that
/// currently pass and are not what Step 2 is changing. Anything that needs real
/// units uses Battlefield.Geometry.
/// </summary>
public static class TestStage
{
    /// <summary>Same instance every call — deterministic across snapshot rebuilds.</summary>
    public static readonly StageGeometry Default = Build();

    private static StageGeometry Build()
    {
        var stage = new StageGeometry();

        // Floor: wide solid block, top surface at y=450.
        stage.AddSolid(new FxAabb(
            new FxVec2(Fx.FromInt(500), Fx.FromInt(500)),
            new FxVec2(Fx.FromInt(2000), Fx.FromInt(50))));

        // One platform floating above, spanning x=[350,650] at y=300.
        stage.AddPlatform(new OneWayPlatform(
            Fx.FromInt(300), Fx.FromInt(350), Fx.FromInt(650)));

        // STEP 2: the same floor expressed as a GROUND SEGMENT. Added, not
        // substituted -- Solids stays exactly as it was so Debug/TestBody.cs and
        // the Phase 4 F9 tests (which use the old CollisionResolver) keep passing
        // untouched, while anything on the new SegmentCollisionResolver has real
        // segment data to stand on. Matches the solid's top edge (centre 500 minus
        // half-height 50 = 450) and its full X span, so both resolvers agree on
        // where the floor is.
        stage.AddGround(new FxSegment(
            new FxVec2(Fx.FromInt(-1500), Fx.FromInt(450)),
            new FxVec2(Fx.FromInt(2500), Fx.FromInt(450))));

        return stage;
    }
}
