using PlatformFighter.Core.Math;

namespace PlatformFighter.Core.Combat;

/// <summary>
/// Real Melee knockback formula, re-derived directly from MeleeLight's actual
/// source (<c>src/physics/hitDetection.js:getKnockback</c>, sk==0 branch — the
/// non-set-knockback path, which is all this engine's extracted Fox/Falco
/// data uses so far), not from the commonly-quoted SmashWiki formula text.
///
/// PREVIOUS PASS OF THIS FILE WAS WRONG. It matched the SmashWiki-style
/// formula shape (`p/10 + (p*d)/20`) using p = percent BEFORE this hit, on
/// the theory that MeleeLight calls getKnockback with the pre-hit percent
/// (true) and adds damage to the running total afterward (also true). But
/// that reasoning missed that MeleeLight's own formula body computes
/// `damagestaled + Math.floor(percent)` — i.e. it adds this hit's damage to
/// the pre-hit percent ITSELF, inside the formula, before doing anything
/// else with it. In other words the quantity the formula actually operates
/// on is the POST-hit percent; MeleeLight just computes that sum internally
/// instead of receiving it as an argument. Confirmed by re-reading the exact
/// source lines (both the getKnockback body and its call site) this pass,
/// not assumed. The old code silently dropped the `+ d` term, i.e. every hit
/// on 0% defenders (and generally undershooting at all percents) computed
/// meaningfully less knockback than real Melee.
///
/// Real formula (sk==0 branch, ds = du = d since nothing here uses staled vs.
/// unstaled damage separately yet):
///
///   pAfter = percentBeforeHit + d
///   bracket = (pAfter/10 + (pAfter*d)/20) * (200/(w+100)) * 1.4 + 18
///   Kb = bracket * (kbg/100) + kbb
///
/// where:
///   d   = this hit's damage (Hitbox.Damage)
///   w   = defender's weight
///   kbg = knockback growth (Hitbox.KnockbackGrowth)
///   kbb = base knockback (Hitbox.KnockbackBase)
///
/// Also ported: MeleeLight's crouch-cancel (x0.67) and vectoring/V-cancel
/// (x0.95) multipliers, and its 2500 knockback cap. NOT yet ported: the
/// staling/freshness (ds vs. du).
///
/// THE SET-KNOCKBACK BRANCH IS NOW PORTED (it previously was not, and that was
/// the sole blocker on Fox's Down Air, whose every hitbox uses sk=30). See the
/// branch in ComputeMagnitude for the exact formula and why percent/damage
/// legitimately do not appear in it.
///
/// SECOND BUG FOUND AND FIXED THIS PASS, separate from the one above:
/// ComputeMagnitude's return value is a knockback MAGNITUDE, not a velocity —
/// real Melee scales it by 0.03 before applying it as launch speed
/// (hitDetection.js's getHorizontalVelocity/getVerticalVelocity). PlayerMover.
/// ApplyHit was applying the raw magnitude directly as Velocity with no
/// scaling at all, i.e. every hit launched the defender at ~33x real Melee
/// speed — a jab at 0% (magnitude ~20) became a ~20-units/tick launch versus
/// DashSpeed's 1.9, so anyone hit by anything left the stage in a single
/// tick. This is what "any hit sends the dummy flying off the map" was.
/// <see cref="VelocityScale"/> now carries that factor; ApplyHit multiplies
/// by it. ComputeHitstunFrames is untouched — confirmed by reading
/// getHitstun directly that it takes the UNSCALED magnitude, so it was never
/// part of this bug.
/// </summary>
public static class KnockbackMath
{
	/// <summary>Frames of hitstun per unit of knockback magnitude. Matches
	/// MeleeLight's getHitstun exactly: floor(knockback * 0.4).</summary>
	public static readonly Fx HitstunPerKnockbackUnit = Fx.Ratio(2, 5);

	private static readonly Fx Ten = Fx.FromInt(10);
	private static readonly Fx Twenty = Fx.FromInt(20);
	private static readonly Fx TwoHundred = Fx.FromInt(200);
	private static readonly Fx OneHundred = Fx.FromInt(100);
	private static readonly Fx WeightOffset = Fx.FromInt(100);
	private static readonly Fx FormulaScalar = Fx.Ratio(14, 10); // 1.4
	private static readonly Fx FormulaBase = Fx.FromInt(18);
	private static readonly Fx KnockbackCap = Fx.FromInt(2500);
	private static readonly Fx CrouchCancelMultiplier = Fx.Ratio(67, 100);  // 0.67
	private static readonly Fx VectorCancelMultiplier = Fx.Ratio(95, 100);  // 0.95

	/// <summary>Default defender weight, used only by the overload below when no
	/// real per-character weight is available yet. Matches Fox — see
	/// Characters/Fox/FoxAttributes.cs — since Fox is the only fighter with real
	/// extracted data right now. Phase 9 (Character framework) replaces every
	/// call site that relies on this default with the defender's actual weight.</summary>
	private static readonly Fx DefaultWeight = Fx.FromInt(75);

	/// <summary>Converts a raw knockback MAGNITUDE (from <see cref="ComputeMagnitude"/>)
	/// into an actual launch VELOCITY. Real Melee/MeleeLight does NOT apply the
	/// formula's output directly as velocity — <c>src/physics/hitDetection.js</c>'s
	/// <c>getHorizontalVelocity</c>/<c>getVerticalVelocity</c> both compute
	/// <c>initialVelocity = knockback * 0.03</c> before splitting it by launch
	/// angle. <see cref="ComputeHitstunFrames"/> is correct as-is and must NOT
	/// use this scalar — MeleeLight's own <c>getHitstun</c> takes the RAW
	/// (unscaled) knockback value (<c>floor(knockback * 0.4)</c>), confirmed by
	/// reading both functions side by side in the real source, not assumed from
	/// symmetry with this one.</summary>
	public static readonly Fx VelocityScale = Fx.Ratio(3, 100); // 0.03

	/// <summary>percentBeforeHit = the defender's damage percent BEFORE this
	/// hit's damage is added. This method adds hit.Damage internally to match
	/// MeleeLight's real formula — see the class doc. Caller still applies
	/// hit.Damage to the defender's running percent separately/afterward for
	/// display and for the NEXT hit's percentBeforeHit; don't double-add it
	/// here.</summary>
	public static Fx ComputeMagnitude(
		Fx percentBeforeHit, in Hitbox hit, Fx defenderWeight,
		bool crouching = false, bool vectorCancel = false)
	{
		Fx kb;
		Fx weightTerm = TwoHundred / (defenderWeight + WeightOffset);

		if (hit.SetKnockback != Fx.Zero)
		{
			// SET-KNOCKBACK BRANCH — getKnockback's `else` in hitDetection.js:
            //   kb = ((((sk * 10 / 20) + 1) * 1.4 * (200/(w+100)) + 18) * (kg/100)) + bk
            // Percent and damage do NOT appear: a set-knockback hit launches the
            // same distance at 0% as at 200%. That is the whole point of the
			// branch and is what makes Fox's drill (dair) a combo tool rather
			// than a kill move. `sk * 10 / 20` is kept as the source writes it
			// (i.e. sk/2) rather than pre-simplified, so this reads 1:1 against
			// the original line.
			Fx skTerm = (hit.SetKnockback * Ten / Twenty) + Fx.One;
			Fx scaledSk = skTerm * FormulaScalar * weightTerm + FormulaBase;
			kb = scaledSk * (hit.KnockbackGrowth / OneHundred) + hit.KnockbackBase;
		}
		else
		{
			Fx d = hit.Damage;
			Fx p = percentBeforeHit + d; // MeleeLight computes damagestaled + floor(percent) internally — see class doc.

			Fx pTerm = p / Ten + (p * d) / Twenty;
			Fx scaled = pTerm * weightTerm * FormulaScalar + FormulaBase;
			kb = scaled * (hit.KnockbackGrowth / OneHundred) + hit.KnockbackBase;
		}

		if (kb > KnockbackCap) kb = KnockbackCap;
		if (crouching) kb *= CrouchCancelMultiplier;
		if (vectorCancel) kb *= VectorCancelMultiplier;
		return kb;
	}

	/// <summary>Convenience overload using <see cref="DefaultWeight"/> — see its
	/// doc comment. Prefer the explicit-weight overload once a real defender
	/// weight is available (Phase 9+).</summary>
	public static Fx ComputeMagnitude(Fx percentBeforeHit, in Hitbox hit)
		=> ComputeMagnitude(percentBeforeHit, in hit, DefaultWeight);

	public static int ComputeHitstunFrames(Fx magnitude)
	{
		int frames = (magnitude * HitstunPerKnockbackUnit).ToIntFloor();
		return frames < 1 ? 1 : frames;
	}

	/// <summary>Per-tick knockback velocity decay magnitude during hitstun.
	/// MeleeLight's getHorizontalDecay/getVerticalDecay both use 0.051 *
	/// cos/sin(angle) — i.e. a fixed decay magnitude of 0.051 split across
	/// X/Y by the launch angle. This engine stores a pre-normalized launch
	/// DIRECTION instead of an angle (see Hitbox.cs's doc comment on why),
	/// so PlayerMover.TickHitstun applies this as a straight magnitude decay
	/// toward zero on each velocity axis independently — an adaptation of
	/// the real formula to this engine's direction-vector representation,
	/// not a literal transcription. Revisit once/if a real angle system
	/// exists (Hitbox.cs already notes that'd be an additive change).</summary>
	public static readonly Fx KnockbackDecayPerTick = Fx.Ratio(51, 1000);
}
