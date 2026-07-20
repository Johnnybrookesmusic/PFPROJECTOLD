using System;

namespace PlatformFighter.Core.Input;

[Flags]
public enum ButtonFlags : ushort
{
	None      = 0,
	A         = 1 << 0,  // Attack / confirm
	B         = 1 << 1,  // Special
	X         = 1 << 2,  // Jump (primary)
	Y         = 1 << 3,  // Jump (secondary)
	Z         = 1 << 4,  // Grab
	Start     = 1 << 5,
	DPadUp    = 1 << 6,
	DPadDown  = 1 << 7,
	DPadLeft  = 1 << 8,
	DPadRight = 1 << 9,
	LDigital  = 1 << 10, // full click, not analog travel
	RDigital  = 1 << 11,
	// Bits 12-15 reserved. Never renumber a shipped id — it's part of the
	// snapshot/replay format the moment it ships.

	// Aliases kept for the Phase 2.1 debug rig (DeterminismTest).
	Attack = A,
	Jump   = X,
}

/// <summary>
/// Complete per-frame input for one player slot. This IS the wire/replay/
/// snapshot format as of Phase 2.3 — every bit here is permanent the
/// moment it ships. Pack()/Unpack() must stay inverses of each other and
/// must never change bit positions; add new fields only in the reserved
/// button bits or by growing FrameInputFormat.Version and defining a new
/// packed shape for readers that understand it.
/// </summary>
public readonly struct FrameInput : IEquatable<FrameInput>
{
	public static readonly FrameInput None = default;

	public readonly ButtonFlags Buttons;
	public readonly sbyte MainX;   // -100..100, 0 = neutral
	public readonly sbyte MainY;
	public readonly sbyte CX;
	public readonly sbyte CY;
	public readonly byte LAnalog;  // 0..255, 0 = untouched
	public readonly byte RAnalog;

	public FrameInput(
		ButtonFlags buttons,
		sbyte mainX, sbyte mainY,
		sbyte cx = 0, sbyte cy = 0,
		byte lAnalog = 0, byte rAnalog = 0)
	{
		Buttons = buttons;
		MainX = mainX;
		MainY = mainY;
		CX = cx;
		CY = cy;
		LAnalog = lAnalog;
		RAnalog = rAnalog;
	}

	public ulong Pack()
	{
		ulong v = (ushort)Buttons;
		v |= (ulong)(byte)MainX << 16;
		v |= (ulong)(byte)MainY << 24;
		v |= (ulong)(byte)CX    << 32;
		v |= (ulong)(byte)CY    << 40;
		v |= (ulong)LAnalog     << 48;
		v |= (ulong)RAnalog     << 56;
		return v;
	}

	public static FrameInput Unpack(ulong v) => new(
		buttons: (ButtonFlags)(ushort)(v & 0xFFFF),
		mainX:   (sbyte)((v >> 16) & 0xFF),
		mainY:   (sbyte)((v >> 24) & 0xFF),
		cx:      (sbyte)((v >> 32) & 0xFF),
		cy:      (sbyte)((v >> 40) & 0xFF),
		lAnalog: (byte)((v >> 48) & 0xFF),
		rAnalog: (byte)((v >> 56) & 0xFF));

	public bool Equals(FrameInput other) => Pack() == other.Pack();
	public override bool Equals(object? obj) => obj is FrameInput fi && Equals(fi);
	public override int GetHashCode() => Pack().GetHashCode();
	public override string ToString() =>
		$"[{Buttons}] stick({MainX},{MainY}) cstick({CX},{CY}) L{LAnalog} R{RAnalog}";
}

/// <summary>
/// Version tag for the FrameInput wire/replay format — NOT stored in the
/// packed ulong itself (every bit there is already spoken for). Belongs in
/// replay file headers and the netplay handshake so an old replay/peer on
/// a narrower format is rejected instead of silently misread.
/// </summary>
public static class FrameInputFormat
{
	public const int Version = 3; // bump only when Pack()/Unpack() layout changes
}
