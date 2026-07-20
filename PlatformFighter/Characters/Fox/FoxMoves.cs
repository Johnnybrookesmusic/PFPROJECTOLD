using System.Collections.Generic;
using PlatformFighter.Core.Math;
 
namespace PlatformFighter.Characters.Fox;
 
/// <summary>
/// Phase 9: Fox's move data as compiled C# constants, transcribed from
/// Characters/Fox/FoxMoveData.json (itself sourced from MeleeLight's real
/// engine — src/characters/fox/attributes.js hitbox definitions and
/// src/characters/fox/moves/*.js frame timers; see that JSON's _readme for
/// the full provenance and every simplification made getting here).
///
/// GroundedNormals is what the Fox/Falco hybrid actually uses (grounded
/// attack button dispatch in PlayerMover.TryStartAttack). FireBird/Illusion/
/// ReflectorHit are standalone fields (not in GroundedNormals) — as of
/// Phase 11 they ARE reachable, via Characters/Hybrid/FoxFalcoHybrid.cs's
/// moveset dictionary keying them under MoveSlot.UpB/SideB/DownB directly
/// (TryStartAttack picks the slot from stick direction + the special
/// button; see its own doc comment).
/// </summary>
public static class FoxMoves
{
	public static readonly MoveDef Jab1 = new(
		"Jab1", MoveCategory.GroundedNormal,
		firstActiveFrame: 2, lastActiveFrame: 3, totalFrames: 17, iasaFrame: 15,
		damage: 4, angleDegrees: 70, knockbackBase: 0, knockbackGrowth: 100);
 
	public static readonly MoveDef ForwardTilt = new(
		"ForwardTilt", MoveCategory.GroundedNormal,
		firstActiveFrame: 5, lastActiveFrame: 8, totalFrames: 27, iasaFrame: 24,
		damage: 9, angleDegrees: AngleTable.SakuraiAngle, knockbackBase: 0, knockbackGrowth: 100);
 
	public static readonly MoveDef UpTilt = new(
		"UpTilt", MoveCategory.GroundedNormal,
		firstActiveFrame: 5, lastActiveFrame: 11, totalFrames: 24, iasaFrame: 21,
		damage: 12, angleDegrees: 110, knockbackBase: 18, knockbackGrowth: 140);
 
	public static readonly MoveDef DownTilt = new(
		"DownTilt", MoveCategory.GroundedNormal,
		firstActiveFrame: 7, lastActiveFrame: 9, totalFrames: 30, iasaFrame: 27,
		damage: 10, angleDegrees: 70, knockbackBase: 25, knockbackGrowth: 125);
 
	public static readonly MoveDef ForwardSmash = new(
		"ForwardSmash", MoveCategory.GroundedNormal,
		firstActiveFrame: 12, lastActiveFrame: 22, totalFrames: 40, iasaFrame: 35,
		damage: 15, angleDegrees: AngleTable.SakuraiAngle, knockbackBase: 10, knockbackGrowth: 105);
 
	public static readonly MoveDef UpSmash = new(
		"UpSmash", MoveCategory.GroundedNormal,
		firstActiveFrame: 7, lastActiveFrame: 17, totalFrames: 42, iasaFrame: 38,
		damage: 18, angleDegrees: 80, knockbackBase: 30, knockbackGrowth: 112);
 
	public static readonly MoveDef DownSmash = new(
		"DownSmash", MoveCategory.GroundedNormal,
		firstActiveFrame: 6, lastActiveFrame: 10, totalFrames: 50, iasaFrame: 45,
		damage: 15, angleDegrees: 25, knockbackBase: 20, knockbackGrowth: 65);
 
	public static readonly MoveDef DashAttack = new(
		"DashAttack", MoveCategory.GroundedNormal,
		firstActiveFrame: 4, lastActiveFrame: 17, totalFrames: 40, iasaFrame: 35,
		damage: 7, angleDegrees: 72, knockbackBase: 35, knockbackGrowth: 90);
 
	/// <summary>Up-B (Fire Fox) launch hit. Phase 11: now reachable via input
	/// (PlayerMover.TryStartAttack dispatches special+up-stick here) and now a
	/// REAL self-launch, not just a hitbox. LaunchSpeed 3.8 and LaunchDecayPerTick
	/// 0.1 are transcribed directly from MeleeLight's
    /// src/characters/fox/moves/UPSPECIALLAUNCH.js (cVel = 3.8*cos/sin(angle) at
    /// launch frame, decaying by 0.1/tick, ~30 thrust frames before free-fall) —
	/// hit data (damage/angle/knockback) is still fightcore.gg's. Simplifications
	/// vs. real Melee, documented rather than silently accepted: (1) real Fire Fox
	/// aims along a continuous stick angle (upbAngleMultiplier); this only picks
	/// between straight-up and a fixed 45°-diagonal (see PlayerMover.TryStartAttack),
	/// since Fx has no arbitrary-angle trig. (2) decay starts immediately here
	/// instead of real Melee's frame-6 delay. (3) no ledge-sweetspot/wall-bonk
    /// (FIREFOXBOUNCE) handling.</summary>
    public static readonly MoveDef FireBird = new(
        "FireBird", MoveCategory.Special,
        firstActiveFrame: 1, lastActiveFrame: 3, totalFrames: 45, iasaFrame: 45,
        damage: 14, angleDegrees: 80, knockbackBase: 60, knockbackGrowth: 60,
        launchSpeed: Fx.Ratio(3_800_000, 1_000_000), launchDecayPerTick: Fx.Ratio(100_000, 1_000_000));
 
	/// <summary>Side-B (Illusion), GROUND only — Phase 11's other new self-launch
	/// special. LaunchSpeed 18.72 (a near-instant horizontal burst, not a ramp-up)
	/// transcribed directly from MeleeLight's
    /// src/characters/fox/moves/SIDESPECIALGROUND.js (cVel.x = 18.72*face at the
    /// burst frame, decaying by 0.1/tick from a re-based 2.1 a few frames later).
    /// KNOWN GAPS, documented rather than silently cut: (1) real Melee has a
    /// ~21-frame startup before the burst (aiming/crouch); this applies LaunchSpeed
    /// on the very first action frame instead — a real gap, not a rounding choice.
    /// (2) air Illusion (SIDESPECIALAIR.js, a gradual accel/decel curve, not a
	/// burst) isn't modeled — TryStartAttack only dispatches this slot when
	/// grounded. (3) LaunchDecayPerTick here (0.468) is NOT MeleeLight's real
    /// number — real Illusion decays 18.72 down to a re-based 2.1 (not a
    /// continuous decay from 18.72) then decays THAT to 0 at 0.1/tick over ~39
	/// more frames. This engine's single-phase decay model can't represent that
    /// two-stage curve, so the rate is tuned to fully bleed off within TotalFrames
    /// instead — the alternative (real 0.1/tick from 18.72) would leave a huge,
    /// wrong-looking residual slide velocity once the move hands control back to
    /// TickGrounded.
    ///
	/// Post-Phase-11 fix: hit data is no longer all-zero. Real Melee's Illusion
	/// is low-damage/low-knockback (SmashWiki/Smashpedia: "deals little damage
	/// (3%) and has very little knockback") — fightcore.gg's own page (frames
	/// 22-25 active, 63 total) couldn't be fetched directly this session (server
	/// error), so the exact base-knockback/growth/angle fields below are a
	/// documented approximation from that description, not a verified transcription
	/// like the rest of this file's moves. Active window is ALSO deliberately not
	/// real Melee's frames 22-25: this engine's burst fires on move-frame 1 (see
    /// gap (1) above), so the hitbox is active frames 2-14 — while Fox is actually
	/// moving fast in THIS engine's timing — rather than copying real frame numbers
	/// that assumed a 21-frame startup this engine doesn't have.</summary>
    public static readonly MoveDef Illusion = new(
        "Illusion", MoveCategory.Special,
        firstActiveFrame: 2, lastActiveFrame: 14, totalFrames: 40, iasaFrame: 40,
        damage: 3, angleDegrees: 40, knockbackBase: 10, knockbackGrowth: 30,
        launchSpeed: Fx.Ratio(18_720_000, 1_000_000), launchDecayPerTick: Fx.Ratio(468_000, 1_000_000));
 
    /// <summary>Down-B (Reflector) direct-contact hit. Angle 361 (Sakurai) is a
    /// documented PLACEHOLDER — the real value was null/unresolved in the source
    /// data (reflector interactions in real Melee depend on catching a projectile
	/// vs. a direct hit, which this single-hit MoveDef doesn't distinguish). Not
	/// reachable via input yet either — see FireBird's note above.</summary>
    public static readonly MoveDef ReflectorHit = new(
        "ReflectorHit", MoveCategory.Special,
        firstActiveFrame: 1, lastActiveFrame: 3, totalFrames: 40, iasaFrame: 40,
        damage: 0, angleDegrees: AngleTable.SakuraiAngle, knockbackBase: 0, knockbackGrowth: 100);
 
	/// <summary>Fox's own Neutral Air — real data from MeleeLight's
    /// src/characters/fox/attributes.js (nair1, the first/clean hit — same
    /// "collapse multi-hit to first hit" convention as everywhere else in this
    /// file) and src/characters/fox/moves/ATTACKAIRN.js (active frame4-7,
    /// before hitboxes.id swaps to the weaker nair2 at frame8). Added this pass
    /// because the hybrid previously had NO Fox aerials at all — every aerial
	/// slot silently fell back to Falco's, which is correct for Bair/Uair per
	/// the hybrid design but was accidentally also true for Nair/Fair/Dair.</summary>
	public static readonly MoveDef NeutralAir = new(
		"NeutralAir", MoveCategory.Aerial,
		firstActiveFrame: 4, lastActiveFrame: 7, totalFrames: 44, iasaFrame: 38,
		damage: 12, angleDegrees: AngleTable.SakuraiAngle, knockbackBase: 10, knockbackGrowth: 100,
		landingLagFrames: 16);
 
	/// <summary>Fox's own Forward Air — real data from attributes.js (fair1,
    /// first hit) and ATTACKAIRF.js (active frame6-8, before the second swing
	/// wave at frame16). See NeutralAir's note re: why this is new this pass.</summary>
	public static readonly MoveDef ForwardAir = new(
		"ForwardAir", MoveCategory.Aerial,
		firstActiveFrame: 6, lastActiveFrame: 8, totalFrames: 45, iasaFrame: 40,
		damage: 7, angleDegrees: AngleTable.SakuraiAngle, knockbackBase: 10, knockbackGrowth: 100,
		landingLagFrames: 22);
 
	/// <summary>Fox's own Up Air — real data from attributes.js, but using the
    /// SECOND hit (upair2: dmg13/angle85/kg116/bk40) instead of the first
    /// (upair1). upair1 has a nonzero `sk` (set-knockback) field in the source
    /// — a different knockback formula branch (fixed value scaled by a
    /// setKnockback constant, not the growth/base formula) that Hitbox/
	/// KnockbackMath don't support yet (no SetKnockback field exists). upair2
	/// has sk=0, the normal formula this engine already implements, so it's
    /// used here as the representative hit rather than fabricating a wrong
    /// approximation for upair1. Frame window (11-14) and total (ATTACKAIRU.js,
	/// interrupt at timer&gt;39) match upair2's real active window, not upair1's
	/// earlier one — this move's ACTIVE TIMING is real; only the choice of
	/// WHICH of the two real hits to represent is a simplification.
	///
	/// NOTE: in the hybrid's actual moveset (FoxFalcoHybrid.cs), Falco's own
	/// UpAir replaces this slot entirely per the override list — this exists
	/// for a Fox-only build and so the data isn't lost.</summary>
    public static readonly MoveDef UpAir = new(
        "UpAir", MoveCategory.Aerial,
        firstActiveFrame: 11, lastActiveFrame: 14, totalFrames: 40, iasaFrame: 36,
        damage: 13, angleDegrees: 85, knockbackBase: 40, knockbackGrowth: 116,
        landingLagFrames: 18);
 
	/// <summary>Fox's own Down Air does NOT have an entry here — its only real
	/// hitbox (attributes.js `dair`) has a nonzero `sk` (set-knockback) field,
	/// same gap as UpAir's upair1 above, except Dair has no non-SK second hit
    /// to fall back on. Porting it faithfully needs SetKnockback support added
    /// to Hitbox/KnockbackMath first — deliberately left out rather than
	/// approximated with the wrong formula. The hybrid's DownAir slot is
	/// simply unfilled until that support exists (see FoxFalcoHybrid.cs).</summary>
 
	/// <summary>What PlayerMover.TryStartAttack actually dispatches to for grounded
	/// attacks. NeutralB is deliberately NOT here — the hybrid design uses Falco's
	/// Blaster for Neutral B instead of Fox's own laser (Characters/Hybrid/
	/// FoxFalcoHybrid.cs), so it comes from FalcoMoves, not this table.</summary>
	public static readonly Dictionary<MoveSlot, MoveDef> GroundedNormals = new()
	{
		[MoveSlot.Jab1] = Jab1,
		[MoveSlot.ForwardTilt] = ForwardTilt,
		[MoveSlot.UpTilt] = UpTilt,
		[MoveSlot.DownTilt] = DownTilt,
		[MoveSlot.ForwardSmash] = ForwardSmash,
		[MoveSlot.UpSmash] = UpSmash,
		[MoveSlot.DownSmash] = DownSmash,
		[MoveSlot.DashAttack] = DashAttack,
	};
 
	/// <summary>Fox's own aerials — see each move's doc comment above for what's
    /// real vs. a documented gap. Not merged into GroundedNormals since MoveSlot
	/// keys don't overlap; kept as its own table so FoxFalcoHybrid.cs can layer
	/// Falco's Bair/Uair overrides on top of a real Fox aerial base instead of
    /// defaulting the whole aerial category to Falco by omission.</summary>
    public static readonly Dictionary<MoveSlot, MoveDef> Aerials = new()
    {
        [MoveSlot.NeutralAir] = NeutralAir,
        [MoveSlot.ForwardAir] = ForwardAir,
        [MoveSlot.UpAir] = UpAir,
    };
}
 
