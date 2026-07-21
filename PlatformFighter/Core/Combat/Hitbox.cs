using PlatformFighter.Core.Math;

namespace PlatformFighter.Core.Combat;

/// <summary>
/// Phase 7: one active hitbox. Deliberately minimal — no active-frame
/// windows, no shield-stun, no multi-hit, no hitlag. Those are Phase
/// 9/10 (Character framework / first fighter) concerns once there's an
/// actual move to attach them to; this struct is just "what happens if
/// this box connects with a hurtbox this tick."
///
/// DirX/DirY are a pre-normalized launch direction, NOT a Melee-style
/// angle-in-degrees. Fx has no sin/cos (see Core/Math/Fx.cs — sim math
/// is fixed-point only, and a correctly-rounded fixed-point trig table
/// is its own chunk of work), so instead of an angle this stores the
/// direction directly, e.g. (1,0) horizontal, (0,-1) straight up,
/// (Fx.Ratio(7,10), -Fx.Ratio(7,10)) diagonal up-and-away. DirX is
/// mirrored by the attacker's facing at apply-time; DirY is not
/// (negative is always up, per FxVec2's +Y-down convention).
///
/// OffsetX/OffsetY/Radius: real per-move spatial hitbox data, ported
/// directly from MeleeLight's attributes.js `offsets[...]` tables and
/// each hitbox's own `size` field (a circle radius, not a box — real
/// Melee/MeleeLight hitboxes are spheres in 3D, circles once flattened
/// to this engine's 2D). Offset is attacker-position-relative, in the
/// same sim units as everything else; OffsetX mirrors by facing exactly
/// like DirX; OffsetY is NOT mirrored (matches DirY's convention) and is
/// the source MeleeLight Y value NEGATED, same Y-up-to-Y-down adaptation
/// Stages/Battlefield.cs already documents and applies. See
/// Gameplay/CombatSystem.cs for how this replaces the old flat
/// whole-body-plus-AttackReach box.
///
/// Radius == Fx.Zero is the explicit "no real spatial data ported for
/// this move yet" sentinel (e.g. Characters/Fox/FoxMoves.cs's Illusion) —
/// CombatSystem falls back to the old flat-reach box for those rather
/// than a fabricated radius, per Directive Rule 4 (never invent values).
/// </summary>
public readonly struct Hitbox
{
	public readonly Fx Damage;
	public readonly Fx KnockbackBase;
	public readonly Fx KnockbackGrowth;

	/// <summary>
	/// MeleeLight's <c>sk</c> (set knockback). NONZERO means this hitbox ignores
	/// the damage/percent-scaling formula entirely and uses a fixed-magnitude
	/// branch instead — see KnockbackMath.ComputeMagnitude. Zero (the common
	/// case) selects the normal growth/base formula.
	///
	/// This field is why Fox's Down Air could not be ported before now: every
	/// one of dair's real hitboxes has sk=30, and there was no way to express
	/// that. Fox hitboxes that use it: dair (30), upair1 (30), pummel (30),
	/// downspecial (80), ledgegetupquick/slow (90).
	/// </summary>
	public readonly Fx SetKnockback;
	public readonly Fx DirX;
	public readonly Fx DirY;
	public readonly Fx OffsetX;
	public readonly Fx OffsetY;
	public readonly Fx Radius;

	/// <summary>MeleeLight's <c>type</c>: 0 normal, 1 slash, 2 grab, 3 fire,
	/// 4 electric, 5 sleep, 6 reactOnClank, 7 reflect, 8 inert. Carried so the
	/// data is 1:1 with the source; only <see cref="IsGrab"/> and
	/// <see cref="IsReflector"/> are consumed today.</summary>
	public readonly int HitType;

	/// <summary>MeleeLight's <c>hG</c>/<c>hA</c>: whether this box can hit a
	/// grounded / an airborne target. Every Fox hitbox has both set except the
	/// throw boxes, which is exactly why they must be carried rather than
	/// assumed.</summary>
	public readonly bool HitsGrounded;
	public readonly bool HitsAirborne;

	public const int TypeGrab = 2;
	public const int TypeReflect = 7;
	public const int TypeInert = 8;

	public bool IsGrab => HitType == TypeGrab;
	public bool IsReflector => HitType == TypeReflect;

	public Hitbox(Fx damage, Fx knockbackBase, Fx knockbackGrowth, Fx dirX, Fx dirY,
		Fx offsetX = default, Fx offsetY = default, Fx radius = default,
		Fx setKnockback = default, int hitType = 0,
		bool hitsGrounded = true, bool hitsAirborne = true)
	{
		Damage = damage;
		KnockbackBase = knockbackBase;
		KnockbackGrowth = knockbackGrowth;
		DirX = dirX;
		DirY = dirY;
		OffsetX = offsetX;
		OffsetY = offsetY;
		Radius = radius;
		SetKnockback = setKnockback;
		HitType = hitType;
		HitsGrounded = hitsGrounded;
		HitsAirborne = hitsAirborne;
	}
}
