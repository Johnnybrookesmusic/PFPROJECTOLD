using System.Collections.Generic;
using PlatformFighter.Core.Math;

namespace PlatformFighter.Characters;

/// <summary>
/// Phase 9: converts a Melee-style knockback angle (degrees, standard
/// convention — 0 = pure forward/horizontal, 90 = straight up, 180 = pure
/// backward, 270 = straight down, measured as if the attacker faces right)
/// into a pre-normalized Hitbox.DirX/DirY pair, since Fx has no sin/cos
/// (see Hitbox.cs's doc comment). Values are computed OFFLINE in Python to
/// 6 decimal digits and hardcoded as Fx.Ratio literals — never derived at
/// runtime — matching FoxAttributes.cs's existing convention for baking in
/// non-integer constants.
///
/// FxVec2 uses +Y-down, so a "straight up" launch (angle 90) is stored as
/// dirY = -1, not +1 — DirY's own doc comment on Hitbox already states this
/// convention; this table produces vectors that already match it.
///
/// SAKURAI ANGLE (361): real Melee's angle 361 means "wait until knockback
/// magnitude is known, then pick 45 or 40/44 depending on whether the hit
/// is grounded/aerial and the actual damage/knockback of the specific hit."
/// That decision tree needs info this table doesn't have (a magnitude,
/// which is computed in KnockbackMath, downstream of building the Hitbox
/// this table's caller is constructing). Simplified to a flat 45° for every
/// Sakurai-angle move — every MoveDef built with angle 361 (Fox forward
/// tilt/forward smash/reflector hit, Falco nair/bair/fair/blaster) uses this
/// approximation. Documented here rather than silently baked in, per the
/// same placeholder-honesty convention as FoxMoves.cs's Reflector-angle note.
/// </summary>
public static class AngleTable
{
	public const int SakuraiAngle = 361;
	private const int SakuraiApproximationDegrees = 45;

	private static readonly Dictionary<int, (Fx X, Fx Y)> Table = new()
	{
		[0]   = (Fx.One, Fx.Zero),
		[25]  = (Fx.Ratio(906_308, 1_000_000), -Fx.Ratio(422_618, 1_000_000)),
		[40]  = (Fx.Ratio(766_044, 1_000_000), -Fx.Ratio(642_788, 1_000_000)),
		[45]  = (Fx.Ratio(707_107, 1_000_000), -Fx.Ratio(707_107, 1_000_000)),
		[65]  = (Fx.Ratio(422_618, 1_000_000), -Fx.Ratio(906_308, 1_000_000)),
		[70]  = (Fx.Ratio(342_020, 1_000_000), -Fx.Ratio(939_693, 1_000_000)),
		[72]  = (Fx.Ratio(309_017, 1_000_000), -Fx.Ratio(951_057, 1_000_000)),
		[75]  = (Fx.Ratio(258_819, 1_000_000), -Fx.Ratio(965_926, 1_000_000)),
		[78]  = (Fx.Ratio(207_912, 1_000_000), -Fx.Ratio(978_148, 1_000_000)),
		[80]  = (Fx.Ratio(173_648, 1_000_000), -Fx.Ratio(984_808, 1_000_000)),
		[84]  = (Fx.Ratio(104_528, 1_000_000), -Fx.Ratio(994_522, 1_000_000)),
		[85]  = (Fx.Ratio(87_156, 1_000_000), -Fx.Ratio(996_195, 1_000_000)),
		[90]  = (Fx.Zero, -Fx.One),
		[92]  = (-Fx.Ratio(34_899, 1_000_000), -Fx.Ratio(999_391, 1_000_000)),
		[97]  = (-Fx.Ratio(121_869, 1_000_000), -Fx.Ratio(992_546, 1_000_000)),
		[100] = (-Fx.Ratio(173_648, 1_000_000), -Fx.Ratio(984_808, 1_000_000)),
		[110] = (-Fx.Ratio(342_020, 1_000_000), -Fx.Ratio(939_693, 1_000_000)),
		[124] = (-Fx.Ratio(559_193, 1_000_000), -Fx.Ratio(829_038, 1_000_000)),
		[180] = (-Fx.One, Fx.Zero),
		[270] = (Fx.Zero, Fx.One),
		[290] = (Fx.Ratio(342_020, 1_000_000), Fx.Ratio(939_693, 1_000_000)),
		[300] = (Fx.Ratio(500_000, 1_000_000), Fx.Ratio(866_025, 1_000_000)),
		[315] = (Fx.Ratio(707_107, 1_000_000), Fx.Ratio(707_107, 1_000_000)),
	};

	/// <summary>Looks up (or approximates, for the Sakurai angle) a direction
	/// vector for the given degree value. Throws on any angle not present in
	/// the table and not 361 — that's a transcription gap to fix by adding the
	/// angle here, not something to silently default.</summary>
	public static (Fx X, Fx Y) Get(int angleDegrees)
	{
		int lookup = angleDegrees == SakuraiAngle ? SakuraiApproximationDegrees : angleDegrees;
		if (Table.TryGetValue(lookup, out var dir)) return dir;
		throw new System.ArgumentOutOfRangeException(nameof(angleDegrees),
			angleDegrees, "No AngleTable entry for this angle — add one rather than guessing.");
	}
}
