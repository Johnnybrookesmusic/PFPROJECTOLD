using System;

namespace PlatformFighter.Core.Sim;

public enum DivergenceResult
{
    Match,
    Diverged,
    /// <summary>Frame too old — already overwritten in the ring.</summary>
    Unknown,
}

/// <summary>
/// Ring buffer of per-frame state hashes, same shape as InputRingBuffer.
/// Today it feeds the debug overlay and the determinism tests; in Phase 16
/// peers exchange recent hashes and Check() is the desync detector.
/// </summary>
public sealed class HashHistory
{
    public const int DefaultCapacity = 256; // ~4.3s at 60Hz, power of two.

    private readonly ulong[] _hashes;
    private readonly int[] _frames;
    private readonly int _mask;

    public HashHistory(int capacity = DefaultCapacity)
    {
        if (capacity <= 0 || (capacity & (capacity - 1)) != 0)
            throw new ArgumentException("capacity must be a power of two", nameof(capacity));
        _hashes = new ulong[capacity];
        _frames = new int[capacity];
        Array.Fill(_frames, -1);
        _mask = capacity - 1;
    }

    public void Record(int frame, ulong hash)
    {
        int i = frame & _mask;
        _hashes[i] = hash;
        _frames[i] = frame;
    }

    public bool TryGet(int frame, out ulong hash)
    {
        int i = frame & _mask;
        if (_frames[i] == frame) { hash = _hashes[i]; return true; }
        hash = 0;
        return false;
    }

    /// <summary>Compare an externally produced hash (other run, peer, replay) against ours.</summary>
    public DivergenceResult Check(int frame, ulong expected)
    {
        if (!TryGet(frame, out ulong ours)) return DivergenceResult.Unknown;
        return ours == expected ? DivergenceResult.Match : DivergenceResult.Diverged;
    }
}
