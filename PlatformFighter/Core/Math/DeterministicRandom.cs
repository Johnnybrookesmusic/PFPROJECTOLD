namespace PlatformFighter.Core.Math;

/// <summary>
/// xorshift64* PRNG. System.Random is forbidden inside the simulation:
/// its algorithm is an implementation detail and its state can't be
/// snapshotted for rollback. This one is 8 bytes of state, trivially
/// serializable, and identical on every platform.
/// </summary>
public struct DeterministicRandom
{
    private ulong _state;

    public DeterministicRandom(ulong seed) => _state = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;

    /// <summary>Snapshot/restore this for save states.</summary>
    public ulong State { readonly get => _state; set => _state = value; }

    public ulong NextULong()
    {
        _state ^= _state >> 12;
        _state ^= _state << 25;
        _state ^= _state >> 27;
        return _state * 0x2545F4914F6CDD1DUL;
    }

    /// <summary>Uniform integer in [0, exclusiveMax).</summary>
    public int NextInt(int exclusiveMax) => (int)(NextULong() % (ulong)exclusiveMax);
}
