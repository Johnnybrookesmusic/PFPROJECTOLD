using System;
using PlatformFighter.Core.Math;

namespace PlatformFighter.Core.Sim;

/// <summary>
/// Deterministic binary writer for sim state. Explicit little-endian
/// byte layout (no BitConverter, no endianness surprises) so the same
/// state produces the same bytes on every machine — which is what makes
/// state hashes comparable across peers and across runs.
/// One writer is reused per frame for hashing (see SimWorld) to avoid
/// per-frame GC pressure.
/// </summary>
public sealed class StateWriter
{
    private byte[] _buf = new byte[1024];
    private int _pos;

    public int Length => _pos;

    public void Reset() => _pos = 0;

    private void Ensure(int more)
    {
        if (_pos + more > _buf.Length)
            Array.Resize(ref _buf, System.Math.Max(_buf.Length * 2, _pos + more));
    }

    public void WriteByte(byte v) { Ensure(1); _buf[_pos++] = v; }
    public void WriteBool(bool v) => WriteByte(v ? (byte)1 : (byte)0);

    public void WriteUInt(uint v)
    {
        Ensure(4);
        _buf[_pos++] = (byte)v;
        _buf[_pos++] = (byte)(v >> 8);
        _buf[_pos++] = (byte)(v >> 16);
        _buf[_pos++] = (byte)(v >> 24);
    }

    public void WriteInt(int v) => WriteUInt((uint)v);

    public void WriteULong(ulong v)
    {
        WriteUInt((uint)v);
        WriteUInt((uint)(v >> 32));
    }

    public void WriteLong(long v) => WriteULong((ulong)v);

    public void WriteFx(Fx v) => WriteLong(v.Raw);

    public void WriteFxVec2(FxVec2 v)
    {
        WriteFx(v.X);
        WriteFx(v.Y);
    }

    public ReadOnlySpan<byte> AsSpan() => new(_buf, 0, _pos);

    public byte[] ToArray()
    {
        var copy = new byte[_pos];
        Array.Copy(_buf, copy, _pos);
        return copy;
    }
}

/// <summary>Mirror of StateWriter. Reads must occur in the exact order they were written.</summary>
public sealed class StateReader
{
    private readonly byte[] _data;
    private int _pos;

    public StateReader(byte[] data) => _data = data;

    private void Require(int n)
    {
        if (_pos + n > _data.Length)
            throw new InvalidOperationException(
                "StateReader: read past end of snapshot data — save/load order mismatch?");
    }

    public byte ReadByte() { Require(1); return _data[_pos++]; }
    public bool ReadBool() => ReadByte() != 0;

    public uint ReadUInt()
    {
        Require(4);
        uint v = _data[_pos]
               | (uint)_data[_pos + 1] << 8
               | (uint)_data[_pos + 2] << 16
               | (uint)_data[_pos + 3] << 24;
        _pos += 4;
        return v;
    }

    public int ReadInt() => (int)ReadUInt();

    public ulong ReadULong()
    {
        ulong lo = ReadUInt();
        ulong hi = ReadUInt();
        return lo | (hi << 32);
    }

    public long ReadLong() => (long)ReadULong();

    public Fx ReadFx() => Fx.FromRaw(ReadLong());

    public FxVec2 ReadFxVec2()
    {
        var x = ReadFx();
        var y = ReadFx();
        return new FxVec2(x, y);
    }
}
