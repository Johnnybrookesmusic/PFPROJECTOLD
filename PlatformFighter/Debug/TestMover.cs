using PlatformFighter.Core.Math;
using PlatformFighter.Core.Sim;

namespace PlatformFighter.Debug;

/// <summary>
/// Phase 1 test object, now also the first ISimObject implementing the
/// full save/load contract — the pattern every fighter/projectile will
/// follow: every field Tick() touches appears in SaveState AND LoadState,
/// same order.
/// </summary>
public sealed class TestMover : ISimObject
{
    public const int TypeIdValue = 1;
    public int TypeId => TypeIdValue;

    public FxVec2 Position;
    public FxVec2 PreviousPosition;

    private Fx _velocityX = Fx.FromInt(300); // 300 px/s, exact.

    private static readonly Fx MinX = Fx.FromInt(100);
    private static readonly Fx MaxX = Fx.FromInt(900);

    public TestMover(FxVec2 start)
    {
        Position = PreviousPosition = start;
    }

    public void Tick(SimWorld world)
    {
        PreviousPosition = Position;
        Position += new FxVec2(_velocityX * SimWorld.DeltaTime, Fx.Zero);

        if (Position.X > MaxX || Position.X < MinX)
            _velocityX = -_velocityX;
    }

    public void SaveState(StateWriter w)
    {
        w.WriteFxVec2(Position);
        w.WriteFxVec2(PreviousPosition);
        w.WriteFx(_velocityX);
    }

    public void LoadState(StateReader r)
    {
        Position = r.ReadFxVec2();
        PreviousPosition = r.ReadFxVec2();
        _velocityX = r.ReadFx();
    }
}
