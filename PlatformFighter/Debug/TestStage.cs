using PlatformFighter.Core.Math;
using PlatformFighter.Core.Sim.Collision;

namespace PlatformFighter.Debug;

/// <summary>
/// Minimal FD-style debug stage: one wide solid floor, no walls or
/// ceiling, plus a single Battlefield-style one-way platform above it.
/// Exists purely to exercise CollisionResolver in TestBody and
/// DeterminismTest — NOT a real Stages/ asset (that's Phase 12).
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

        return stage;
    }
}
