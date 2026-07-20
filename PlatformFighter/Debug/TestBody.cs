using PlatformFighter.Core.Input;
using PlatformFighter.Core.Math;
using PlatformFighter.Core.Sim;
using PlatformFighter.Core.Sim.Collision;

namespace PlatformFighter.Debug;

/// <summary>
/// Phase 4 test rig: a gravity-affected AABB that falls onto TestStage's
/// floor/platform and reports Grounded — the same role TestMover played
/// for Phase 1/2, now exercising CollisionResolver and, for the first
/// time in this project, actually reading player input via
/// world.GetInput() rather than following a scripted path.
///
/// NOTE: gravity/speed here are placeholder debug values, not tuned feel.
/// Real movement curves (dash, air control, fast-fall) are Phase 5.
/// </summary>
public sealed class TestBody : ISimObject
{
    public const int TypeIdValue = 2;
    public int TypeId => TypeIdValue;

    private static readonly Fx Gravity = Fx.Ratio(1, 2);
    private static readonly Fx MoveSpeed = Fx.FromInt(4);
    private static readonly FxVec2 HalfSize = new(Fx.FromInt(20), Fx.FromInt(30));

    public FxVec2 Position;
    public FxVec2 PreviousPosition;
    public FxVec2 Velocity;
    public bool Grounded;

    private readonly StageGeometry _stage;

    public TestBody(FxVec2 start, StageGeometry stage)
    {
        Position = PreviousPosition = start;
        _stage = stage;
    }

    public void Tick(SimWorld world)
    {
        PreviousPosition = Position;

        FrameInput input = world.GetInput(0);

        // Stick Y here follows the INPUT convention (+up), which is the
        // opposite sign of FxVec2 position space (+Y down, see FxVec2.cs)
        // — two different axes that happen to share a letter. Holding
        // down (negative stick Y) is the drop-through-platform gesture.
        Fx moveX = MoveSpeed * Fx.Ratio(input.MainX, 100);
        bool passThroughPlatforms = input.MainY < -InputDecode.StickDeadzoneUnits;

        Velocity = new FxVec2(moveX, Velocity.Y + Gravity);

        FxVec2 movedPosition = Position + Velocity;
        var result = CollisionResolver.Resolve(
            PreviousPosition, movedPosition, Velocity, HalfSize, _stage, passThroughPlatforms);

        Position = result.Position;
        Velocity = result.Velocity;
        Grounded = result.Grounded;
    }

    public void SaveState(StateWriter w)
    {
        w.WriteFxVec2(Position);
        w.WriteFxVec2(PreviousPosition);
        w.WriteFxVec2(Velocity);
        w.WriteBool(Grounded);
    }

    public void LoadState(StateReader r)
    {
        Position = r.ReadFxVec2();
        PreviousPosition = r.ReadFxVec2();
        Velocity = r.ReadFxVec2();
        Grounded = r.ReadBool();
    }
}
