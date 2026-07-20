using System.Collections.Generic;
using PlatformFighter.Core.Math;
 
namespace PlatformFighter.Characters.Falco;
 
/// <summary>
/// Falco's move data, re-transcribed this pass DIRECTLY from MeleeLight's real
/// source (src/characters/falco/attributes.js hitbox tables + src/characters/
/// falco/moves/*.js frame timers), replacing the previous version of this file
/// which was hand-transcribed from fightcore.gg — per explicit instruction
/// that MeleeLight is the source of truth, not third-party frame data sites.
/// Concretely this changed BackAir's damage (9 -> 15) and UpAir's base
/// knockback (30 -> 40) from what fightcore.gg had listed.
///
/// This file now holds exactly the moves the hybrid's override list actually
/// uses (Characters/Hybrid/FoxFalcoHybrid.cs): Down Tilt, Up Tilt, Down Smash,
/// Back Air, Up Air, Blaster (Neutral B), Phantasm (Side B). The previous
/// version also had Falco's Nair/Fair/DownAir — dropped, not because they're
/// wrong, but because the hybrid doesn't use them (Fox's own Nair/Fair now
/// live in FoxMoves.cs) and keeping unused, lower-provenance data around
/// invites someone wiring it in by mistake later.
///
/// Multi-hit moves are still collapsed to their first/cleanest listed hit —
/// same simplification MoveDef's own doc comment documents. Where a move has
/// several hitbox IDs sharing identical damage/angle/kb (just different
/// sizes/offsets for a layered swing — e.g. Down Tilt's three IDs), that's not
/// actually an ambiguity to collapse, since they already agree.
/// </summary>
public static class FalcoMoves
{
	/// <summary>Real data: attributes.js `bair1` (offsets[3].bair1, first/near
	/// hitbox), frame timing from ATTACKAIRB.js (active frame4-7, before
	/// hitboxes.id swaps to the weaker bair2 at frame8, off entirely at
	/// frame20; FALL at timer&gt;39). Landing lag kept from the previous
	/// (fightcore-sourced) pass — MeleeLight's `land` handler wasn't checked
	/// this round, so that one field is still not verified against real
	/// source, unlike everything else here.</summary>
	public static readonly MoveDef BackAir = new(
		"BackAir", MoveCategory.Aerial,
		firstActiveFrame: 4, lastActiveFrame: 7, totalFrames: 40, iasaFrame: 38,
		damage: 15, angleDegrees: AngleTable.SakuraiAngle, knockbackBase: 0, knockbackGrowth: 100,
		landingLagFrames: 20);
 
	/// <summary>Real data: attributes.js `upair1` (offsets[3].upair1, first
	/// hit — sk=0, unlike Fox's equivalent-slot upair1 which has a nonzero sk
	/// and had to be skipped; Falco's is the plain formula, no gap here).
	/// Frame timing from ATTACKAIRU.js (active frame8-9, before hitboxes.id
	/// swaps to upair2 at frame11, off at frame15/40).</summary>
	public static readonly MoveDef UpAir = new(
		"UpAir", MoveCategory.Aerial,
		firstActiveFrame: 8, lastActiveFrame: 9, totalFrames: 40, iasaFrame: 36,
		damage: 6, angleDegrees: 90, knockbackBase: 40, knockbackGrowth: 20,
		landingLagFrames: 18);
 
	/// <summary>Real data: attributes.js `dtilt` (offsets[3].downtilt — all
	/// three hitbox IDs share identical damage/angle/kb, only sizes/offsets
	/// differ, so no first-hit ambiguity to resolve here). Frame timing from
	/// DOWNTILT.js: hitboxes active at timer===7 through timer&lt;10 (frames
	/// 7-9), turned off at timer===10; interrupt/FAF at timer&gt;29 -> SQUATWAIT
	/// (totalFrames 30).</summary>
	public static readonly MoveDef DownTilt = new(
		"DownTilt", MoveCategory.GroundedNormal,
		firstActiveFrame: 7, lastActiveFrame: 9, totalFrames: 30, iasaFrame: 28,
		damage: 13, angleDegrees: 75, knockbackBase: 25, knockbackGrowth: 125);
 
	/// <summary>Real data: attributes.js `uptilt` (offsets[3].uptilt.id0 —
	/// id1 has the same damage/kb, angle 90 instead of 97; id0 used as the
	/// representative hit). Frame timing from UPTILT.js: active at
	/// timer===5 through timer&lt;12 (frames 5-11), off at timer===12;
	/// interrupt/FAF at timer&gt;23 -> WAIT (totalFrames 24).</summary>
	public static readonly MoveDef UpTilt = new(
		"UpTilt", MoveCategory.GroundedNormal,
		firstActiveFrame: 5, lastActiveFrame: 11, totalFrames: 24, iasaFrame: 22,
		damage: 9, angleDegrees: 97, knockbackBase: 30, knockbackGrowth: 120);
 
	/// <summary>Real data: attributes.js `dsmash` (offsets[3].downsmash.id0 —
	/// the near/strong hitbox; id2/id3 are a separate, weaker far hitbox with
	/// dmg13/angle80, not modeled here per the single-representative-hit
	/// simplification). This is the UNCHARGED value — DownSmash's real charge
	/// scaling (up to ~1.5x damage/knockback at max charge, per DOWNSMASH.js's
	/// 60-frame charge window) isn't modeled; MoveDef has no charge concept.
    /// Frame timing from DOWNSMASH.js: hitboxes active at timer===6 through
    /// timer&lt;11 (frames 6-10), off at timer===11; interrupt/FAF at
    /// timer&gt;49 -> WAIT (totalFrames 50).</summary>
    public static readonly MoveDef DownSmash = new(
        "DownSmash", MoveCategory.GroundedNormal,
        firstActiveFrame: 6, lastActiveFrame: 10, totalFrames: 50, iasaFrame: 46,
        damage: 16, angleDegrees: 25, knockbackBase: 20, knockbackGrowth: 70);
 
    /// <summary>Neutral B for the hybrid. Real hitbox source for a proper laser
	/// projectile wasn't re-checked this pass (still the prior pass's adapted
    /// "2-frame shot fired window" standing in for a projectile this engine
	/// can't simulate yet) — kept as-is, not re-verified against
	/// src/characters/falco/attributes.js's blaster-related fields this round.</summary>
    public static readonly MoveDef Blaster = new(
        "Blaster", MoveCategory.Special,
        firstActiveFrame: 23, lastActiveFrame: 24, totalFrames: 57, iasaFrame: 50,
        damage: 3, angleDegrees: AngleTable.SakuraiAngle, knockbackBase: 0, knockbackGrowth: 100);
 
    /// <summary>Side-B (Phantasm), GROUND only. Real data from
	/// src/physics/article.js's ILLUSION article definition (NOT
	/// attributes.js's normal per-character hitbox table — Phantasm's hit
	/// comes from a separately-spawned "article"/projectile-style object, same
	/// as Fox's own Illusion): isFox=false branch, grounded (type=1) override
    /// values — damage 7, angle 65, knockbackGrowth 60, knockbackBase 74.
    ///
    /// IMPORTANT CORRECTION vs. the design brief: real Falco Phantasm is NOT a
	/// spike/meteor in MeleeLight's own source — angle 65 sends the target up
	/// and away, not down. Porting the actual number rather than forcing a
	/// meteor angle to match a description that doesn't match the source data
    /// this whole effort is supposed to be authoritative.
    ///
    /// Movement burst (cVel.x = 16.50*face at the dash-launch frame, from
    /// SIDESPECIALGROUND.js) is NOT ported to LaunchSpeed/LaunchDecayPerTick
    /// here — that would need re-deriving a single-phase decay curve the same
	/// way FoxMoves.Illusion's doc comment already flags as an approximation
	/// for Fox's version, and this pass didn't do that work for Falco's; this
	/// MoveDef has a real hit but Falco won't actually dash yet. Flagged
	/// rather than silently left as a non-mover Illusion.</summary>
	public static readonly MoveDef Phantasm = new(
		"Phantasm", MoveCategory.Special,
		firstActiveFrame: 18, lastActiveFrame: 21, totalFrames: 60, iasaFrame: 60,
		damage: 7, angleDegrees: 65, knockbackBase: 74, knockbackGrowth: 60);
}
