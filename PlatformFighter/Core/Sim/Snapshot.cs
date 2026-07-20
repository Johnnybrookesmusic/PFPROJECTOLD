namespace PlatformFighter.Core.Sim;

/// <summary>
/// A complete, self-contained copy of sim state at one frame: frame
/// counter, RNG state, latched inputs, and every object (type + state)
/// in tick order. This is the "save" half of rollback's
/// save → resimulate loop.
/// </summary>
public sealed class Snapshot
{
    public readonly int FrameNumber;
    public readonly ulong Hash;
    public readonly byte[] Data;

    public Snapshot(int frameNumber, byte[] data, ulong hash)
    {
        FrameNumber = frameNumber;
        Data = data;
        Hash = hash;
    }
}
