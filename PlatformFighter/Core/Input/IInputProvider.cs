namespace PlatformFighter.Core.Input;

/// <summary>
/// Anything that can produce one FrameInput per tick for one player slot.
/// This is the seam future phases plug into without touching SimDriver's
/// tick loop: LocalInputProvider today, plus later a NetworkInputProvider
/// (Phase 6) and a ReplayInputProvider (reads a recorded match back frame
/// by frame) — all interchangeable from SimDriver's point of view.
/// </summary>
public interface IInputProvider
{
    /// <summary>
    /// Called exactly once per sim tick, from SimDriver, right before that
    /// tick runs. Implementations MAY touch Godot APIs (this runs outside
    /// Tick(), on the view side of the input -> sim -> render boundary).
    /// </summary>
    FrameInput Sample();
}
