namespace PlatformFighter.Characters;

/// <summary>
/// Phase 9: every attack slot a CharacterData's moveset table can be keyed
/// by. Deliberately covers more slots than PlayerMover.TryStartAttack can
/// currently reach (Grab, the four throws, SideB, UpB, DownB) — see that
/// method's doc comment for exactly which ones are wired to input today
/// (grounded normals, the four aerials, and NeutralB only). Having the
/// slot — and MoveDef data behind it, where extracted — costs nothing and
/// means the next phase that adds grab/throw/directional-special dispatch
/// isn't also blocked on transcribing frame data from scratch.
/// </summary>
public enum MoveSlot
{
    Jab1,
    /// <summary>Second hit of the jab combo (JAB2.js).</summary>
    Jab2,
	/// <summary>Fox's rapid jab (JAB3.js) — five cycling hitbox positions,
	/// jab3_1..jab3_5, which loop while A is held.</summary>
	Jab3,
	ForwardTilt,
	UpTilt,
	DownTilt,
	ForwardSmash,
	UpSmash,
	DownSmash,
	DashAttack,

	NeutralAir,
	ForwardAir,
	BackAir,
	UpAir,
	DownAir,

	NeutralB,
	SideB,
	UpB,
	DownB,

	Grab,
	/// <summary>CATCHATTACK.js — the grab pummel.</summary>
	Pummel,
	ThrowForward,
	ThrowBack,
	ThrowUp,
	ThrowDown,

	/// <summary>DOWNATTACK.js — the attack from a knocked-down state. Two
	/// stages (downattack1 hits behind, downattack2 in front).</summary>
	GetUpAttack,
	/// <summary>CLIFFATTACKQUICK.js — ledge attack under 100%.</summary>
	LedgeAttackQuick,
	/// <summary>CLIFFATTACKSLOW.js — ledge attack at 100% or above.</summary>
	LedgeAttackSlow,
}
