using System;

namespace PlatformFighter.Core.Sim;

/// <summary>
/// FNV-1a 64-bit over serialized sim state. Not cryptographic — it's a
/// determinism tripwire. Two runs (or two netplay peers) with identical
/// inputs must produce identical hashes every frame; the first frame they
/// don't is exactly where a desync was born.
/// </summary>
public static class StateHash
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public static ulong Compute(ReadOnlySpan<byte> data)
    {
        ulong h = OffsetBasis;
        for (int i = 0; i < data.Length; i++)
        {
            h ^= data[i];
            h *= Prime;
        }
        return h;
    }
}
