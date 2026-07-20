using System;
using System.Runtime.CompilerServices;

namespace PlatformFighter.Core.Math;

/// <summary>
/// Q32.32 fixed-point number. The ONLY numeric type allowed inside the
/// deterministic simulation. 32 integer bits, 32 fractional bits.
///
/// WHY: floating-point results can differ across CPUs, compilers, and JIT
/// decisions. Integer math is bit-identical everywhere, which is mandatory
/// for lockstep/rollback netcode and for replay files that never desync.
///
/// RULES:
///  - Rendering code MAY call ToFloat() to position sprites.
///  - Simulation code MUST NEVER convert to float and back.
/// </summary>
public readonly struct Fx : IEquatable<Fx>, IComparable<Fx>
{
	public const int FractionBits = 32;
	public const long OneRaw = 1L << FractionBits;

	/// <summary>Raw Q32.32 bits. Serialize THIS for save states / netcode.</summary>
	public readonly long Raw;

	private Fx(long raw) => Raw = raw;

	// ---- Construction ------------------------------------------------
	public static readonly Fx Zero = new(0);
	public static readonly Fx One  = new(OneRaw);
	public static readonly Fx Half = new(OneRaw >> 1);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Fx FromInt(int value) => new((long)value << FractionBits);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Fx FromRaw(long raw) => new(raw);

	/// <summary>
	/// Build a fraction deterministically, e.g. Fx.Ratio(1, 3) for 1/3.
	/// Prefer this over any float literal — Fx.Ratio(15, 10) instead of 1.5f.
	/// </summary>
	public static Fx Ratio(int numerator, int denominator)
		=> new(((long)numerator << FractionBits) / denominator);

	// ---- Arithmetic ---------------------------------------------------
	public static Fx operator +(Fx a, Fx b) => new(a.Raw + b.Raw);
	public static Fx operator -(Fx a, Fx b) => new(a.Raw - b.Raw);
	public static Fx operator -(Fx a)       => new(-a.Raw);

	/// <summary>
	/// Multiply via 128-bit intermediate so we never overflow mid-calculation.
	/// (a * b) >> 32, computed exactly.
	/// </summary>
	public static Fx operator *(Fx a, Fx b)
	{
		long hi = System.Math.BigMul(a.Raw, b.Raw, out long lo);
		return new((hi << (64 - FractionBits)) | (long)((ulong)lo >> FractionBits));
	}

	public static Fx operator /(Fx a, Fx b)
	{
		// Shift the dividend up 32 bits (via 128-bit math) before dividing.
		System.Int128 n = (System.Int128)a.Raw << FractionBits;
		return new((long)(n / b.Raw));
	}

	// ---- Comparison ---------------------------------------------------
	public static bool operator ==(Fx a, Fx b) => a.Raw == b.Raw;
	public static bool operator !=(Fx a, Fx b) => a.Raw != b.Raw;
	public static bool operator <(Fx a, Fx b)  => a.Raw < b.Raw;
	public static bool operator >(Fx a, Fx b)  => a.Raw > b.Raw;
	public static bool operator <=(Fx a, Fx b) => a.Raw <= b.Raw;
	public static bool operator >=(Fx a, Fx b) => a.Raw >= b.Raw;

	public bool Equals(Fx other) => Raw == other.Raw;
	public override bool Equals(object? obj) => obj is Fx f && f.Raw == Raw;
	public override int GetHashCode() => Raw.GetHashCode();
	public int CompareTo(Fx other) => Raw.CompareTo(other.Raw);

	// ---- Utility ------------------------------------------------------
	public static Fx Abs(Fx v) => v.Raw < 0 ? new(-v.Raw) : v;
	public static Fx Min(Fx a, Fx b) => a.Raw < b.Raw ? a : b;
	public static Fx Max(Fx a, Fx b) => a.Raw > b.Raw ? a : b;
	public static Fx Clamp(Fx v, Fx lo, Fx hi) => Max(lo, Min(hi, v));

	/// <summary>Truncated integer part (rounds toward negative infinity for negatives via arithmetic shift).</summary>
	public int ToIntFloor() => (int)(Raw >> FractionBits);

	/// <summary>RENDER-ONLY. Never feed the result back into simulation state.</summary>
	public float ToFloat() => (float)Raw / OneRaw;

	public override string ToString() => ToFloat().ToString("0.####");
}
