using System.Collections.Generic;
using PlatformFighter.Core.Math;

namespace PlatformFighter.Characters.Fox;

/// <summary>
/// Fox's COMPLETE moveset, ported 1:1 from MeleeLight.
///
/// HOW THIS FILE WAS PRODUCED, and why that matters. Every hitbox parameter
/// below - damage, angle, knockback growth, base knockback, set knockback, hit
/// type, spatial offset, radius - was extracted PROGRAMMATICALLY from
/// <c>meleelight/src/characters/fox/attributes.js</c>: its
/// <c>setHitBoxes(CHARIDS.FOX_ID, {...})</c> table joined against its
/// <c>setOffsets(CHARIDS.FOX_ID, {...})</c> table (including the late
/// <c>offsets[...].push(...)</c> appends further down the file). This is 97
/// individual hitboxes across 24 moves; hand-transcription at that volume
/// reliably introduces errors that surface months later looking like gameplay
/// bugs rather than typos. The parameter order used is
/// <c>createHitbox(offset, size, dmg, angle, kg, bk, sk, type, clank, hG, hA)</c>
/// from <c>src/main/util/createHitBox.js</c>.
///
/// Active-frame windows are the one thing NOT auto-extracted - they live in each
/// move's own state file as <c>player[p].timer</c> comparisons, and were read
/// individually from <c>src/characters/fox/moves/*.js</c> and
/// <c>src/characters/shared/moves/*.js</c>.
///
/// CONVENTIONS (see Hitbox.cs / HitboxSpec.cs for the full rationale):
///  - OffsetY is the source value NEGATED. MeleeLight is Y-up, this engine is
///    Y-down; the same adaptation Stages/Battlefield.cs applies to stage data.
///  - OffsetX and DirX mirror with facing at hit time; the Y components do not.
///  - Radius is the source's <c>size</c> field. Real Melee hitboxes are spheres,
///    so a circle once flattened to 2D - not a box half-extent.
///  - Each MoveDef's flat single-hit fields are that move's REPRESENTATIVE
///    hitbox (its first/strongest box). Real behaviour comes from the Hitboxes
///    array; the flat fields stay because the debug HUD and tests read them.
///
/// WHAT CHANGED vs. the previous partial port:
///  - Multi-hitbox is real. Up-tilt now has its four genuinely-different boxes
///    (12 damage at angle 110, three more at 9 damage / angles 84-80), forward
///    air its five sequential hits, and dash attack / both smashes / nair /
///    bair / upair their two distinct stages. Each of those was ONE box before.
///  - Down Air exists. It was blocked purely on set-knockback support - every
///    dair hitbox uses sk=30 - and KnockbackMath now has that branch, so the
///    drill is real, including the 3-frame re-arm cadence from ATTACKAIRD.js.
///  - Jab2, Jab3 (the five-position rapid jab), Grab, Pummel, the get-up attack
///    and both ledge attacks are ported. None of them existed before.
///  - Throws carry real data, with release frames from each THROW*.js.
/// </summary>
public static class FoxMoves
{
	public static readonly MoveDef Jab1 = new(
		"Jab1",
		MoveCategory.GroundedNormal,
		firstActiveFrame: 2,
		lastActiveFrame: 3,
		totalFrames: 17,
		iasaFrame: 15,
		damage: 4,
		angleDegrees: 70,
		knockbackBase: 0,
		knockbackGrowth: 100,
		offsetX: Fx.Ratio(5490, 1000),
		offsetY: -Fx.Ratio(5970, 1000),
		radius: Fx.Ratio(3328, 1000),
		hitboxes: new HitboxSpec[]
		{
			// --- jab1 ---
			new(firstActiveFrame: 2, lastActiveFrame: 3, damage: 4, angleDegrees: 70, knockbackGrowth: 100, knockbackBase: 0, offsetX: Fx.Ratio(5490, 1000), offsetY: -Fx.Ratio(5970, 1000), radius: Fx.Ratio(3328, 1000)),
			new(firstActiveFrame: 2, lastActiveFrame: 3, damage: 4, angleDegrees: 70, knockbackGrowth: 100, knockbackBase: 0, offsetX: Fx.Ratio(6200, 1000), offsetY: -Fx.Ratio(9120, 1000), radius: Fx.Ratio(3328, 1000)),
		});

	public static readonly MoveDef Jab2 = new(
		"Jab2",
		MoveCategory.GroundedNormal,
		firstActiveFrame: 3,
		lastActiveFrame: 4,
		totalFrames: 20,
		iasaFrame: 16,
		damage: 4,
		angleDegrees: 70,
		knockbackBase: 0,
		knockbackGrowth: 100,
		offsetX: Fx.Ratio(9540, 1000),
		offsetY: -Fx.Ratio(6930, 1000),
		radius: Fx.Ratio(3328, 1000),
		hitboxes: new HitboxSpec[]
		{
			// --- jab2 ---
			new(firstActiveFrame: 3, lastActiveFrame: 4, damage: 4, angleDegrees: 70, knockbackGrowth: 100, knockbackBase: 0, offsetX: Fx.Ratio(9540, 1000), offsetY: -Fx.Ratio(6930, 1000), radius: Fx.Ratio(3328, 1000)),
			new(firstActiveFrame: 3, lastActiveFrame: 4, damage: 4, angleDegrees: 70, knockbackGrowth: 100, knockbackBase: 0, offsetX: Fx.Ratio(2900, 1000), offsetY: -Fx.Ratio(6030, 1000), radius: Fx.Ratio(3328, 1000)),
		});

	public static readonly MoveDef Jab3 = new(
		"Jab3",
		MoveCategory.GroundedNormal,
		firstActiveFrame: 9,
		lastActiveFrame: 39,
		totalFrames: 51,
		iasaFrame: 51,
		damage: 1,
		angleDegrees: 78,
		knockbackBase: 10,
		knockbackGrowth: 80,
		offsetX: Fx.Ratio(2120, 1000),
		offsetY: -Fx.Ratio(10360, 1000),
		radius: Fx.Ratio(3328, 1000),
		hitboxes: new HitboxSpec[]
		{
			// --- jab3_1 ---
			new(firstActiveFrame: 9, lastActiveFrame: 15, damage: 1, angleDegrees: 78, knockbackGrowth: 80, knockbackBase: 10, offsetX: Fx.Ratio(2120, 1000), offsetY: -Fx.Ratio(10360, 1000), radius: Fx.Ratio(3328, 1000)),
			new(firstActiveFrame: 9, lastActiveFrame: 15, damage: 1, angleDegrees: 78, knockbackGrowth: 80, knockbackBase: 10, offsetX: Fx.Ratio(4990, 1000), offsetY: -Fx.Ratio(12110, 1000), radius: Fx.Ratio(3328, 1000)),
			new(firstActiveFrame: 9, lastActiveFrame: 15, damage: 1, angleDegrees: 78, knockbackGrowth: 80, knockbackBase: 10, offsetX: Fx.Ratio(10810, 1000), offsetY: -Fx.Ratio(15640, 1000), radius: Fx.Ratio(3328, 1000)),
			// --- jab3_2 ---
			new(firstActiveFrame: 16, lastActiveFrame: 22, damage: 1, angleDegrees: 78, knockbackGrowth: 80, knockbackBase: 10, offsetX: Fx.Ratio(2300, 1000), offsetY: -Fx.Ratio(9680, 1000), radius: Fx.Ratio(3328, 1000)),
			new(firstActiveFrame: 16, lastActiveFrame: 22, damage: 1, angleDegrees: 78, knockbackGrowth: 80, knockbackBase: 10, offsetX: Fx.Ratio(5490, 1000), offsetY: -Fx.Ratio(10750, 1000), radius: Fx.Ratio(3328, 1000)),
			new(firstActiveFrame: 16, lastActiveFrame: 22, damage: 1, angleDegrees: 78, knockbackGrowth: 80, knockbackBase: 10, offsetX: Fx.Ratio(11940, 1000), offsetY: -Fx.Ratio(12910, 1000), radius: Fx.Ratio(3328, 1000)),
			// --- jab3_3 ---
			new(firstActiveFrame: 23, lastActiveFrame: 29, damage: 1, angleDegrees: 78, knockbackGrowth: 80, knockbackBase: 10, offsetX: Fx.Ratio(1910, 1000), offsetY: -Fx.Ratio(8410, 1000), radius: Fx.Ratio(3328, 1000)),
			new(firstActiveFrame: 23, lastActiveFrame: 29, damage: 1, angleDegrees: 78, knockbackGrowth: 80, knockbackBase: 10, offsetX: Fx.Ratio(5280, 1000), offsetY: -Fx.Ratio(8500, 1000), radius: Fx.Ratio(3328, 1000)),
			new(firstActiveFrame: 23, lastActiveFrame: 29, damage: 1, angleDegrees: 78, knockbackGrowth: 80, knockbackBase: 10, offsetX: Fx.Ratio(12090, 1000), offsetY: -Fx.Ratio(8680, 1000), radius: Fx.Ratio(3328, 1000)),
			// --- jab3_4 ---
			new(firstActiveFrame: 30, lastActiveFrame: 36, damage: 1, angleDegrees: 78, knockbackGrowth: 80, knockbackBase: 10, offsetX: Fx.Ratio(2330, 1000), offsetY: -Fx.Ratio(7690, 1000), radius: Fx.Ratio(3328, 1000)),
			new(firstActiveFrame: 30, lastActiveFrame: 36, damage: 1, angleDegrees: 78, knockbackGrowth: 80, knockbackBase: 10, offsetX: Fx.Ratio(5380, 1000), offsetY: -Fx.Ratio(6970, 1000), radius: Fx.Ratio(3328, 1000)),
			new(firstActiveFrame: 30, lastActiveFrame: 36, damage: 1, angleDegrees: 78, knockbackGrowth: 80, knockbackBase: 10, offsetX: Fx.Ratio(11530, 1000), offsetY: -Fx.Ratio(5510, 1000), radius: Fx.Ratio(3328, 1000)),
			// --- jab3_5 ---
			new(firstActiveFrame: 37, lastActiveFrame: 39, damage: 1, angleDegrees: 78, knockbackGrowth: 80, knockbackBase: 10, offsetX: Fx.Ratio(2070, 1000), offsetY: -Fx.Ratio(6670, 1000), radius: Fx.Ratio(3328, 1000)),
			new(firstActiveFrame: 37, lastActiveFrame: 39, damage: 1, angleDegrees: 78, knockbackGrowth: 80, knockbackBase: 10, offsetX: Fx.Ratio(4790, 1000), offsetY: -Fx.Ratio(5730, 1000), radius: Fx.Ratio(3328, 1000)),
			new(firstActiveFrame: 37, lastActiveFrame: 39, damage: 1, angleDegrees: 78, knockbackGrowth: 80, knockbackBase: 10, offsetX: Fx.Ratio(10290, 1000), offsetY: -Fx.Ratio(3840, 1000), radius: Fx.Ratio(3328, 1000)),
		});

	public static readonly MoveDef ForwardTilt = new(
		"ForwardTilt",
		MoveCategory.GroundedNormal,
		firstActiveFrame: 5,
		lastActiveFrame: 8,
		totalFrames: 27,
		iasaFrame: 24,
		damage: 9,
		angleDegrees: 361,
		knockbackBase: 0,
		knockbackGrowth: 100,
		offsetX: Fx.Ratio(1400, 1000),
		offsetY: -Fx.Ratio(5270, 1000),
		radius: Fx.Ratio(2734, 1000),
		hitboxes: new HitboxSpec[]
		{
			// --- ftilt ---
			new(firstActiveFrame: 5, lastActiveFrame: 8, damage: 9, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 0, offsetX: Fx.Ratio(1400, 1000), offsetY: -Fx.Ratio(5270, 1000), radius: Fx.Ratio(2734, 1000)),
			new(firstActiveFrame: 5, lastActiveFrame: 8, damage: 9, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 0, offsetX: Fx.Ratio(4330, 1000), offsetY: -Fx.Ratio(7070, 1000), radius: Fx.Ratio(3125, 1000)),
			new(firstActiveFrame: 5, lastActiveFrame: 8, damage: 9, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 0, offsetX: Fx.Ratio(3160, 1000), offsetY: -Fx.Ratio(7680, 1000), radius: Fx.Ratio(2344, 1000)),
		});

	public static readonly MoveDef UpTilt = new(
		"UpTilt",
		MoveCategory.GroundedNormal,
		firstActiveFrame: 5,
		lastActiveFrame: 11,
		totalFrames: 24,
		iasaFrame: 21,
		damage: 12,
		angleDegrees: 110,
		knockbackBase: 18,
		knockbackGrowth: 140,
		offsetX: -Fx.Ratio(3840, 1000),
		offsetY: -Fx.Ratio(4750, 1000),
		radius: Fx.Ratio(5078, 1000),
		hitboxes: new HitboxSpec[]
		{
			// --- uptilt ---
			new(firstActiveFrame: 5, lastActiveFrame: 11, damage: 12, angleDegrees: 110, knockbackGrowth: 140, knockbackBase: 18, offsetX: -Fx.Ratio(3840, 1000), offsetY: -Fx.Ratio(4750, 1000), radius: Fx.Ratio(5078, 1000)),
			new(firstActiveFrame: 5, lastActiveFrame: 11, damage: 9, angleDegrees: 84, knockbackGrowth: 140, knockbackBase: 18, offsetX: -Fx.Ratio(3840, 1000), offsetY: -Fx.Ratio(4750, 1000), radius: Fx.Ratio(5078, 1000)),
			new(firstActiveFrame: 5, lastActiveFrame: 11, damage: 9, angleDegrees: 80, knockbackGrowth: 140, knockbackBase: 18, offsetX: -Fx.Ratio(3830, 1000), offsetY: -Fx.Ratio(4710, 1000), radius: Fx.Ratio(3515, 1000)),
			new(firstActiveFrame: 5, lastActiveFrame: 11, damage: 9, angleDegrees: 80, knockbackGrowth: 140, knockbackBase: 18, offsetX: Fx.Ratio(470, 1000), offsetY: -Fx.Ratio(8190, 1000), radius: Fx.Ratio(3125, 1000)),
		});

	public static readonly MoveDef DownTilt = new(
		"DownTilt",
		MoveCategory.GroundedNormal,
		firstActiveFrame: 7,
		lastActiveFrame: 9,
		totalFrames: 30,
		iasaFrame: 27,
		damage: 10,
		angleDegrees: 70,
		knockbackBase: 25,
		knockbackGrowth: 125,
		offsetX: Fx.Ratio(8940, 1000),
		offsetY: -Fx.Ratio(1690, 1000),
		radius: Fx.Ratio(2734, 1000),
		hitboxes: new HitboxSpec[]
		{
			// --- dtilt ---
			new(firstActiveFrame: 7, lastActiveFrame: 9, damage: 10, angleDegrees: 70, knockbackGrowth: 125, knockbackBase: 25, offsetX: Fx.Ratio(8940, 1000), offsetY: -Fx.Ratio(1690, 1000), radius: Fx.Ratio(2734, 1000)),
			new(firstActiveFrame: 7, lastActiveFrame: 9, damage: 10, angleDegrees: 80, knockbackGrowth: 125, knockbackBase: 25, offsetX: Fx.Ratio(10700, 1000), offsetY: -Fx.Ratio(960, 1000), radius: Fx.Ratio(2734, 1000)),
			new(firstActiveFrame: 7, lastActiveFrame: 9, damage: 10, angleDegrees: 90, knockbackGrowth: 125, knockbackBase: 25, offsetX: Fx.Ratio(12470, 1000), offsetY: -Fx.Ratio(240, 1000), radius: Fx.Ratio(3125, 1000)),
		});

	public static readonly MoveDef ForwardSmash = new(
		"ForwardSmash",
		MoveCategory.GroundedNormal,
		firstActiveFrame: 12,
		lastActiveFrame: 22,
		totalFrames: 40,
		iasaFrame: 35,
		damage: 15,
		angleDegrees: 361,
		knockbackBase: 10,
		knockbackGrowth: 105,
		offsetX: Fx.Ratio(9910, 1000),
		offsetY: -Fx.Ratio(13900, 1000),
		radius: Fx.Ratio(3515, 1000),
		hitboxes: new HitboxSpec[]
		{
			// --- fsmash1 ---
			new(firstActiveFrame: 12, lastActiveFrame: 16, damage: 15, angleDegrees: 361, knockbackGrowth: 105, knockbackBase: 10, offsetX: Fx.Ratio(9910, 1000), offsetY: -Fx.Ratio(13900, 1000), radius: Fx.Ratio(3515, 1000)),
			new(firstActiveFrame: 12, lastActiveFrame: 16, damage: 15, angleDegrees: 361, knockbackGrowth: 105, knockbackBase: 10, offsetX: Fx.Ratio(7860, 1000), offsetY: -Fx.Ratio(10320, 1000), radius: Fx.Ratio(3125, 1000)),
			new(firstActiveFrame: 12, lastActiveFrame: 16, damage: 15, angleDegrees: 361, knockbackGrowth: 105, knockbackBase: 10, offsetX: Fx.Ratio(6520, 1000), offsetY: -Fx.Ratio(7880, 1000), radius: Fx.Ratio(2344, 1000)),
			// --- fsmash2 ---
			new(firstActiveFrame: 17, lastActiveFrame: 22, damage: 12, angleDegrees: 361, knockbackGrowth: 105, knockbackBase: 2, offsetX: Fx.Ratio(12840, 1000), offsetY: -Fx.Ratio(5930, 1000), radius: Fx.Ratio(3515, 1000)),
			new(firstActiveFrame: 17, lastActiveFrame: 22, damage: 12, angleDegrees: 361, knockbackGrowth: 105, knockbackBase: 2, offsetX: Fx.Ratio(8220, 1000), offsetY: -Fx.Ratio(6750, 1000), radius: Fx.Ratio(3125, 1000)),
			new(firstActiveFrame: 17, lastActiveFrame: 22, damage: 12, angleDegrees: 361, knockbackGrowth: 105, knockbackBase: 2, offsetX: Fx.Ratio(4680, 1000), offsetY: -Fx.Ratio(7650, 1000), radius: Fx.Ratio(2344, 1000)),
		});

	public static readonly MoveDef UpSmash = new(
		"UpSmash",
		MoveCategory.GroundedNormal,
		firstActiveFrame: 7,
		lastActiveFrame: 17,
		totalFrames: 42,
		iasaFrame: 38,
		damage: 18,
		angleDegrees: 80,
		knockbackBase: 30,
		knockbackGrowth: 112,
		offsetX: Fx.Ratio(6370, 1000),
		offsetY: -Fx.Ratio(7580, 1000),
		radius: Fx.Ratio(3328, 1000),
		hitboxes: new HitboxSpec[]
		{
			// --- upsmash1 ---
			new(firstActiveFrame: 7, lastActiveFrame: 9, damage: 18, angleDegrees: 80, knockbackGrowth: 112, knockbackBase: 30, offsetX: Fx.Ratio(6370, 1000), offsetY: -Fx.Ratio(7580, 1000), radius: Fx.Ratio(3328, 1000)),
			new(firstActiveFrame: 7, lastActiveFrame: 9, damage: 18, angleDegrees: 80, knockbackGrowth: 112, knockbackBase: 30, offsetX: Fx.Ratio(5970, 1000), offsetY: -Fx.Ratio(5390, 1000), radius: Fx.Ratio(4656, 1000)),
			// --- upsmash2 ---
			new(firstActiveFrame: 10, lastActiveFrame: 17, damage: 13, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: Fx.Ratio(3180, 1000), offsetY: -Fx.Ratio(18460, 1000), radius: Fx.Ratio(3328, 1000)),
			new(firstActiveFrame: 10, lastActiveFrame: 17, damage: 13, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: Fx.Ratio(3660, 1000), offsetY: -Fx.Ratio(20650, 1000), radius: Fx.Ratio(3828, 1000)),
		});

	public static readonly MoveDef DownSmash = new(
		"DownSmash",
		MoveCategory.GroundedNormal,
		firstActiveFrame: 6,
		lastActiveFrame: 10,
		totalFrames: 50,
		iasaFrame: 45,
		damage: 15,
		angleDegrees: 25,
		knockbackBase: 20,
		knockbackGrowth: 65,
		offsetX: -Fx.Ratio(8650, 1000),
		offsetY: -Fx.Ratio(1450, 1000),
		radius: Fx.Ratio(4687, 1000),
		hitboxes: new HitboxSpec[]
		{
			// --- dsmash ---
			new(firstActiveFrame: 6, lastActiveFrame: 10, damage: 15, angleDegrees: 25, knockbackGrowth: 65, knockbackBase: 20, offsetX: -Fx.Ratio(8650, 1000), offsetY: -Fx.Ratio(1450, 1000), radius: Fx.Ratio(4687, 1000)),
			new(firstActiveFrame: 6, lastActiveFrame: 10, damage: 15, angleDegrees: 25, knockbackGrowth: 65, knockbackBase: 20, offsetX: Fx.Ratio(9110, 1000), offsetY: -Fx.Ratio(1840, 1000), radius: Fx.Ratio(4687, 1000)),
			new(firstActiveFrame: 6, lastActiveFrame: 10, damage: 12, angleDegrees: 361, knockbackGrowth: 65, knockbackBase: 20, offsetX: -Fx.Ratio(4650, 1000), offsetY: -Fx.Ratio(1460, 1000), radius: Fx.Ratio(3515, 1000)),
			new(firstActiveFrame: 6, lastActiveFrame: 10, damage: 12, angleDegrees: 361, knockbackGrowth: 65, knockbackBase: 20, offsetX: Fx.Ratio(5110, 1000), offsetY: -Fx.Ratio(1670, 1000), radius: Fx.Ratio(3515, 1000)),
		});

	public static readonly MoveDef DashAttack = new(
		"DashAttack",
		MoveCategory.GroundedNormal,
		firstActiveFrame: 4,
		lastActiveFrame: 17,
		totalFrames: 40,
		iasaFrame: 35,
		damage: 7,
		angleDegrees: 72,
		knockbackBase: 35,
		knockbackGrowth: 90,
		offsetX: Fx.Ratio(9830, 1000),
		offsetY: -Fx.Ratio(7160, 1000),
		radius: Fx.Ratio(3828, 1000),
		hitboxes: new HitboxSpec[]
		{
			// --- dashattack1 ---
			new(firstActiveFrame: 4, lastActiveFrame: 7, damage: 7, angleDegrees: 72, knockbackGrowth: 90, knockbackBase: 35, offsetX: Fx.Ratio(9830, 1000), offsetY: -Fx.Ratio(7160, 1000), radius: Fx.Ratio(3828, 1000)),
			new(firstActiveFrame: 4, lastActiveFrame: 7, damage: 7, angleDegrees: 72, knockbackGrowth: 90, knockbackBase: 35, offsetX: Fx.Ratio(5370, 1000), offsetY: -Fx.Ratio(7490, 1000), radius: Fx.Ratio(3828, 1000)),
			// --- dashattack2 ---
			new(firstActiveFrame: 8, lastActiveFrame: 17, damage: 5, angleDegrees: 72, knockbackGrowth: 90, knockbackBase: 20, offsetX: Fx.Ratio(7900, 1000), offsetY: -Fx.Ratio(7170, 1000), radius: Fx.Ratio(2734, 1000)),
			new(firstActiveFrame: 8, lastActiveFrame: 17, damage: 5, angleDegrees: 72, knockbackGrowth: 90, knockbackBase: 20, offsetX: Fx.Ratio(4950, 1000), offsetY: -Fx.Ratio(7470, 1000), radius: Fx.Ratio(2734, 1000)),
		});

	public static readonly MoveDef NeutralAir = new(
		"NeutralAir",
		MoveCategory.Aerial,
		firstActiveFrame: 4,
		lastActiveFrame: 7,
		totalFrames: 44,
		iasaFrame: 38,
		damage: 12,
		angleDegrees: 361,
		knockbackBase: 10,
		knockbackGrowth: 100,
		landingLagFrames: 16,
		offsetX: -Fx.Ratio(960, 1000),
		offsetY: -Fx.Ratio(6530, 1000),
		radius: Fx.Ratio(3496, 1000),
		hitboxes: new HitboxSpec[]
		{
			// --- nair1 ---
			new(firstActiveFrame: 4, lastActiveFrame: 4, damage: 12, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: -Fx.Ratio(960, 1000), offsetY: -Fx.Ratio(6530, 1000), radius: Fx.Ratio(3496, 1000)),
			new(firstActiveFrame: 4, lastActiveFrame: 4, damage: 12, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: Fx.Ratio(5720, 1000), offsetY: -Fx.Ratio(6280, 1000), radius: Fx.Ratio(3496, 1000)),
			new(firstActiveFrame: 4, lastActiveFrame: 4, damage: 12, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: Fx.Ratio(5720, 1000), offsetY: -Fx.Ratio(6280, 1000), radius: Fx.Ratio(2992, 1000)),
			// --- nair2 ---
			new(firstActiveFrame: 5, lastActiveFrame: 7, damage: 9, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: -Fx.Ratio(960, 1000), offsetY: -Fx.Ratio(6530, 1000), radius: Fx.Ratio(3496, 1000)),
			new(firstActiveFrame: 5, lastActiveFrame: 7, damage: 9, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: Fx.Ratio(5720, 1000), offsetY: -Fx.Ratio(6280, 1000), radius: Fx.Ratio(3496, 1000)),
			new(firstActiveFrame: 5, lastActiveFrame: 7, damage: 9, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: Fx.Ratio(5720, 1000), offsetY: -Fx.Ratio(6280, 1000), radius: Fx.Ratio(2922, 1000)),
		});

	public static readonly MoveDef ForwardAir = new(
		"ForwardAir",
		MoveCategory.Aerial,
		firstActiveFrame: 6,
		lastActiveFrame: 20,
		totalFrames: 45,
		iasaFrame: 40,
		damage: 7,
		angleDegrees: 361,
		knockbackBase: 10,
		knockbackGrowth: 100,
		landingLagFrames: 22,
		offsetX: Fx.Ratio(2630, 1000),
		offsetY: -Fx.Ratio(8740, 1000),
		radius: Fx.Ratio(5156, 1000),
		hitboxes: new HitboxSpec[]
		{
			// --- fair1 ---
			new(firstActiveFrame: 6, lastActiveFrame: 8, damage: 7, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: Fx.Ratio(2630, 1000), offsetY: -Fx.Ratio(8740, 1000), radius: Fx.Ratio(5156, 1000)),
			new(firstActiveFrame: 6, lastActiveFrame: 8, damage: 7, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: Fx.Ratio(6480, 1000), offsetY: -Fx.Ratio(10690, 1000), radius: Fx.Ratio(5156, 1000)),
			// --- fair2 ---
			new(firstActiveFrame: 9, lastActiveFrame: 11, damage: 5, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: Fx.Ratio(4490, 1000), offsetY: -Fx.Ratio(7330, 1000), radius: Fx.Ratio(4656, 1000)),
			new(firstActiveFrame: 9, lastActiveFrame: 11, damage: 5, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: Fx.Ratio(8900, 1000), offsetY: -Fx.Ratio(6920, 1000), radius: Fx.Ratio(4656, 1000)),
			// --- fair3 ---
			new(firstActiveFrame: 12, lastActiveFrame: 14, damage: 6, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: Fx.Ratio(2700, 1000), offsetY: -Fx.Ratio(8890, 1000), radius: Fx.Ratio(4656, 1000)),
			new(firstActiveFrame: 12, lastActiveFrame: 14, damage: 6, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: Fx.Ratio(6800, 1000), offsetY: -Fx.Ratio(10620, 1000), radius: Fx.Ratio(4656, 1000)),
			// --- fair4 ---
			new(firstActiveFrame: 15, lastActiveFrame: 17, damage: 4, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: Fx.Ratio(4510, 1000), offsetY: -Fx.Ratio(7570, 1000), radius: Fx.Ratio(4656, 1000)),
			new(firstActiveFrame: 15, lastActiveFrame: 17, damage: 4, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: Fx.Ratio(4990, 1000), offsetY: -Fx.Ratio(7310, 1000), radius: Fx.Ratio(4656, 1000)),
			// --- fair5 ---
			new(firstActiveFrame: 18, lastActiveFrame: 20, damage: 3, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: Fx.Ratio(4510, 1000), offsetY: -Fx.Ratio(7720, 1000), radius: Fx.Ratio(4656, 1000)),
			new(firstActiveFrame: 18, lastActiveFrame: 20, damage: 3, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: Fx.Ratio(8160, 1000), offsetY: -Fx.Ratio(7720, 1000), radius: Fx.Ratio(4656, 1000)),
		});

	public static readonly MoveDef BackAir = new(
		"BackAir",
		MoveCategory.Aerial,
		firstActiveFrame: 4,
		lastActiveFrame: 7,
		totalFrames: 40,
		iasaFrame: 37,
		damage: 15,
		angleDegrees: 361,
		knockbackBase: 10,
		knockbackGrowth: 100,
		landingLagFrames: 20,
		offsetX: -Fx.Ratio(20, 1000),
		offsetY: -Fx.Ratio(8000, 1000),
		radius: Fx.Ratio(3660, 1000),
		hitboxes: new HitboxSpec[]
		{
			// --- bair1 ---
			new(firstActiveFrame: 4, lastActiveFrame: 4, damage: 15, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: -Fx.Ratio(20, 1000), offsetY: -Fx.Ratio(8000, 1000), radius: Fx.Ratio(3660, 1000)),
			new(firstActiveFrame: 4, lastActiveFrame: 4, damage: 15, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: -Fx.Ratio(8040, 1000), offsetY: -Fx.Ratio(9590, 1000), radius: Fx.Ratio(4992, 1000)),
			new(firstActiveFrame: 4, lastActiveFrame: 4, damage: 9, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: Fx.Ratio(2660, 1000), offsetY: -Fx.Ratio(4200, 1000), radius: Fx.Ratio(3328, 1000)),
			// --- bair2 ---
			new(firstActiveFrame: 5, lastActiveFrame: 7, damage: 9, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: Fx.Ratio(0, 1000), offsetY: Fx.Ratio(0, 1000), radius: Fx.Ratio(3328, 1000)),
			new(firstActiveFrame: 5, lastActiveFrame: 7, damage: 9, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: -Fx.Ratio(7910, 1000), offsetY: -Fx.Ratio(9390, 1000), radius: Fx.Ratio(3992, 1000)),
			new(firstActiveFrame: 5, lastActiveFrame: 7, damage: 9, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 10, offsetX: Fx.Ratio(2970, 1000), offsetY: -Fx.Ratio(4200, 1000), radius: Fx.Ratio(3328, 1000)),
		});

	public static readonly MoveDef UpAir = new(
		"UpAir",
		MoveCategory.Aerial,
		firstActiveFrame: 11,
		lastActiveFrame: 14,
		totalFrames: 40,
		iasaFrame: 36,
		damage: 13,
		angleDegrees: 85,
		knockbackBase: 40,
		knockbackGrowth: 116,
		landingLagFrames: 18,
		offsetX: -Fx.Ratio(1220, 1000),
		offsetY: -Fx.Ratio(12270, 1000),
		radius: Fx.Ratio(3660, 1000),
		hitboxes: new HitboxSpec[]
		{
			// --- upair1 ---
			new(firstActiveFrame: 11, lastActiveFrame: 12, damage: 5, angleDegrees: 92, knockbackGrowth: 120, knockbackBase: 0, offsetX: -Fx.Ratio(3720, 1000), offsetY: -Fx.Ratio(12500, 1000), radius: Fx.Ratio(4297, 1000), setKnockback: 30),
			new(firstActiveFrame: 11, lastActiveFrame: 12, damage: 5, angleDegrees: 92, knockbackGrowth: 120, knockbackBase: 0, offsetX: -Fx.Ratio(5040, 1000), offsetY: -Fx.Ratio(13520, 1000), radius: Fx.Ratio(4297, 1000), setKnockback: 30),
			new(firstActiveFrame: 11, lastActiveFrame: 12, damage: 5, angleDegrees: 92, knockbackGrowth: 120, knockbackBase: 0, offsetX: -Fx.Ratio(2070, 1000), offsetY: -Fx.Ratio(12780, 1000), radius: Fx.Ratio(4297, 1000), setKnockback: 30),
			// --- upair2 ---
			new(firstActiveFrame: 13, lastActiveFrame: 14, damage: 13, angleDegrees: 85, knockbackGrowth: 116, knockbackBase: 40, offsetX: -Fx.Ratio(1220, 1000), offsetY: -Fx.Ratio(12270, 1000), radius: Fx.Ratio(3660, 1000)),
			new(firstActiveFrame: 13, lastActiveFrame: 14, damage: 13, angleDegrees: 85, knockbackGrowth: 116, knockbackBase: 40, offsetX: -Fx.Ratio(1670, 1000), offsetY: -Fx.Ratio(14420, 1000), radius: Fx.Ratio(4883, 1000)),
			new(firstActiveFrame: 13, lastActiveFrame: 14, damage: 13, angleDegrees: 85, knockbackGrowth: 116, knockbackBase: 40, offsetX: Fx.Ratio(610, 1000), offsetY: -Fx.Ratio(8890, 1000), radius: Fx.Ratio(4883, 1000)),
		});

	public static readonly MoveDef DownAir = new(
		"DownAir",
		MoveCategory.Aerial,
		firstActiveFrame: 5,
		lastActiveFrame: 25,
		totalFrames: 49,
		iasaFrame: 49,
		damage: 3,
		angleDegrees: 290,
		knockbackBase: 0,
		knockbackGrowth: 100,
		landingLagFrames: 18,
		offsetX: Fx.Ratio(1820, 1000),
		offsetY: -Fx.Ratio(6050, 1000),
		radius: Fx.Ratio(5156, 1000),
		setKnockback: 30,
		hitboxes: new HitboxSpec[]
		{
			// --- dair ---
			new(firstActiveFrame: 5, lastActiveFrame: 25, damage: 3, angleDegrees: 290, knockbackGrowth: 100, knockbackBase: 0, offsetX: Fx.Ratio(1820, 1000), offsetY: -Fx.Ratio(6050, 1000), radius: Fx.Ratio(5156, 1000), setKnockback: 30, refreshPeriod: 3),
			new(firstActiveFrame: 5, lastActiveFrame: 25, damage: 2, angleDegrees: 290, knockbackGrowth: 100, knockbackBase: 0, offsetX: Fx.Ratio(2720, 1000), offsetY: -Fx.Ratio(3950, 1000), radius: Fx.Ratio(5988, 1000), setKnockback: 30, refreshPeriod: 3),
		});

	public static readonly MoveDef GetUpAttack = new(
		"GetUpAttack",
		MoveCategory.GroundedNormal,
		firstActiveFrame: 17,
		lastActiveFrame: 26,
		totalFrames: 49,
		iasaFrame: 49,
		damage: 6,
		angleDegrees: 361,
		knockbackBase: 80,
		knockbackGrowth: 50,
		offsetX: Fx.Ratio(13620, 1000),
		offsetY: -Fx.Ratio(6430, 1000),
		radius: Fx.Ratio(7031, 1000),
		hitboxes: new HitboxSpec[]
		{
			// --- downattack1 ---
			new(firstActiveFrame: 17, lastActiveFrame: 19, damage: 6, angleDegrees: 361, knockbackGrowth: 50, knockbackBase: 80, offsetX: Fx.Ratio(13620, 1000), offsetY: -Fx.Ratio(6430, 1000), radius: Fx.Ratio(7031, 1000)),
			new(firstActiveFrame: 17, lastActiveFrame: 19, damage: 6, angleDegrees: 361, knockbackGrowth: 50, knockbackBase: 80, offsetX: Fx.Ratio(7950, 1000), offsetY: -Fx.Ratio(6300, 1000), radius: Fx.Ratio(3906, 1000)),
			new(firstActiveFrame: 17, lastActiveFrame: 19, damage: 6, angleDegrees: 361, knockbackGrowth: 50, knockbackBase: 80, offsetX: Fx.Ratio(3930, 1000), offsetY: -Fx.Ratio(5780, 1000), radius: Fx.Ratio(3906, 1000)),
			// --- downattack2 ---
			new(firstActiveFrame: 24, lastActiveFrame: 26, damage: 6, angleDegrees: 361, knockbackGrowth: 50, knockbackBase: 80, offsetX: -Fx.Ratio(5480, 1000), offsetY: -Fx.Ratio(7780, 1000), radius: Fx.Ratio(4687, 1000)),
			new(firstActiveFrame: 24, lastActiveFrame: 26, damage: 6, angleDegrees: 361, knockbackGrowth: 50, knockbackBase: 80, offsetX: -Fx.Ratio(8470, 1000), offsetY: -Fx.Ratio(8120, 1000), radius: Fx.Ratio(6250, 1000)),
			new(firstActiveFrame: 24, lastActiveFrame: 26, damage: 6, angleDegrees: 361, knockbackGrowth: 50, knockbackBase: 80, offsetX: -Fx.Ratio(1000, 1000), offsetY: -Fx.Ratio(8690, 1000), radius: Fx.Ratio(8694, 1000)),
		});

	public static readonly MoveDef LedgeAttackQuick = new(
		"LedgeAttackQuick",
		MoveCategory.GroundedNormal,
		firstActiveFrame: 25,
		lastActiveFrame: 34,
		totalFrames: 54,
		iasaFrame: 54,
		damage: 8,
		angleDegrees: 361,
		knockbackBase: 0,
		knockbackGrowth: 100,
		offsetX: Fx.Ratio(5270, 1000),
		offsetY: -Fx.Ratio(6740, 1000),
		radius: Fx.Ratio(4687, 1000),
		setKnockback: 90,
		hitboxes: new HitboxSpec[]
		{
			// --- ledgegetupquick ---
			new(firstActiveFrame: 25, lastActiveFrame: 34, damage: 8, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 0, offsetX: Fx.Ratio(5270, 1000), offsetY: -Fx.Ratio(6740, 1000), radius: Fx.Ratio(4687, 1000), setKnockback: 90),
			new(firstActiveFrame: 25, lastActiveFrame: 34, damage: 8, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 0, offsetX: Fx.Ratio(5190, 1000), offsetY: -Fx.Ratio(5540, 1000), radius: Fx.Ratio(4687, 1000), setKnockback: 90),
			new(firstActiveFrame: 25, lastActiveFrame: 34, damage: 8, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 0, offsetX: -Fx.Ratio(4290, 1000), offsetY: -Fx.Ratio(7600, 1000), radius: Fx.Ratio(4687, 1000), setKnockback: 90),
		});

	public static readonly MoveDef LedgeAttackSlow = new(
		"LedgeAttackSlow",
		MoveCategory.GroundedNormal,
		firstActiveFrame: 57,
		lastActiveFrame: 59,
		totalFrames: 69,
		iasaFrame: 69,
		damage: 8,
		angleDegrees: 361,
		knockbackBase: 0,
		knockbackGrowth: 100,
		offsetX: Fx.Ratio(6100, 1000),
		offsetY: -Fx.Ratio(9980, 1000),
		radius: Fx.Ratio(3125, 1000),
		setKnockback: 90,
		hitboxes: new HitboxSpec[]
		{
			// --- ledgegetupslow ---
			new(firstActiveFrame: 57, lastActiveFrame: 59, damage: 8, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 0, offsetX: Fx.Ratio(6100, 1000), offsetY: -Fx.Ratio(9980, 1000), radius: Fx.Ratio(3125, 1000), setKnockback: 90),
			new(firstActiveFrame: 57, lastActiveFrame: 59, damage: 8, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 0, offsetX: Fx.Ratio(8660, 1000), offsetY: -Fx.Ratio(12620, 1000), radius: Fx.Ratio(4687, 1000), setKnockback: 90),
			new(firstActiveFrame: 57, lastActiveFrame: 59, damage: 8, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 0, offsetX: -Fx.Ratio(380, 1000), offsetY: -Fx.Ratio(8360, 1000), radius: Fx.Ratio(4687, 1000), setKnockback: 90),
		});

	public static readonly MoveDef Grab = new(
		"Grab",
		MoveCategory.GroundedNormal,
		firstActiveFrame: 7,
		lastActiveFrame: 8,
		totalFrames: 30,
		iasaFrame: 30,
		damage: 0,
		angleDegrees: 361,
		knockbackBase: 0,
		knockbackGrowth: 100,
		offsetX: Fx.Ratio(8250, 1000),
		offsetY: -Fx.Ratio(6750, 1000),
		radius: Fx.Ratio(3906, 1000),
		hitboxes: new HitboxSpec[]
		{
			// --- grab ---
			new(firstActiveFrame: 7, lastActiveFrame: 8, damage: 0, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 0, offsetX: Fx.Ratio(8250, 1000), offsetY: -Fx.Ratio(6750, 1000), radius: Fx.Ratio(3906, 1000), hitType: 2),
			new(firstActiveFrame: 7, lastActiveFrame: 8, damage: 0, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 0, offsetX: Fx.Ratio(4500, 1000), offsetY: -Fx.Ratio(6750, 1000), radius: Fx.Ratio(2734, 1000), hitType: 2),
		});

	public static readonly MoveDef Pummel = new(
		"Pummel",
		MoveCategory.GroundedNormal,
		firstActiveFrame: 10,
		lastActiveFrame: 10,
		totalFrames: 24,
		iasaFrame: 24,
		damage: 3,
		angleDegrees: 361,
		knockbackBase: 0,
		knockbackGrowth: 100,
		offsetX: Fx.Ratio(6830, 1000),
		offsetY: -Fx.Ratio(7590, 1000),
		radius: Fx.Ratio(5859, 1000),
		setKnockback: 30,
		hitboxes: new HitboxSpec[]
		{
			// --- pummel ---
			new(firstActiveFrame: 10, lastActiveFrame: 10, damage: 3, angleDegrees: 361, knockbackGrowth: 100, knockbackBase: 0, offsetX: Fx.Ratio(6830, 1000), offsetY: -Fx.Ratio(7590, 1000), radius: Fx.Ratio(5859, 1000), setKnockback: 30),
		});

	/// <summary>THROWUP.js: victim released frame 13, state ends 33. Radius 0 - a throw applies to an already-grabbed victim, it is not a spatial box.</summary>
	public static readonly MoveDef ThrowUp = new(
		"ThrowUp", MoveCategory.GroundedNormal,
		firstActiveFrame: 13, lastActiveFrame: 13, totalFrames: 33, iasaFrame: 33,
		damage: 2, angleDegrees: 90, knockbackBase: 75, knockbackGrowth: 110,
		offsetX: -Fx.Ratio(67, 1000), offsetY: -Fx.Ratio(17540, 1000), radius: Fx.Ratio(0, 1000),
		hitboxes: new HitboxSpec[]
		{
			new(firstActiveFrame: 13, lastActiveFrame: 13, damage: 2, angleDegrees: 90,
				knockbackGrowth: 110, knockbackBase: 75,
				offsetX: -Fx.Ratio(67, 1000), offsetY: -Fx.Ratio(17540, 1000), radius: Fx.Ratio(0, 1000)),
		});

	/// <summary>THROWDOWN.js: released frame 23, ends 43.</summary>
	public static readonly MoveDef ThrowDown = new(
		"ThrowDown", MoveCategory.GroundedNormal,
		firstActiveFrame: 23, lastActiveFrame: 23, totalFrames: 43, iasaFrame: 43,
		damage: 1, angleDegrees: 270, knockbackBase: 150, knockbackGrowth: 40,
		offsetX: Fx.Ratio(501, 1000), offsetY: Fx.Ratio(0, 1000), radius: Fx.Ratio(0, 1000),
		hitboxes: new HitboxSpec[]
		{
			new(firstActiveFrame: 23, lastActiveFrame: 23, damage: 1, angleDegrees: 270,
				knockbackGrowth: 40, knockbackBase: 150,
				offsetX: Fx.Ratio(501, 1000), offsetY: Fx.Ratio(0, 1000), radius: Fx.Ratio(0, 1000)),
		});

	/// <summary>THROWBACK.js: Fox turns on frame 10, releases 14, ends 32.</summary>
	public static readonly MoveDef ThrowBack = new(
		"ThrowBack", MoveCategory.GroundedNormal,
		firstActiveFrame: 14, lastActiveFrame: 14, totalFrames: 32, iasaFrame: 32,
		damage: 2, angleDegrees: 124, knockbackBase: 80, knockbackGrowth: 85,
		offsetX: -Fx.Ratio(6590, 1000), offsetY: -Fx.Ratio(5660, 1000), radius: Fx.Ratio(0, 1000),
		hitboxes: new HitboxSpec[]
		{
			new(firstActiveFrame: 14, lastActiveFrame: 14, damage: 2, angleDegrees: 124,
				knockbackGrowth: 85, knockbackBase: 80,
				offsetX: -Fx.Ratio(6590, 1000), offsetY: -Fx.Ratio(5660, 1000), radius: Fx.Ratio(0, 1000)),
		});

	/// <summary>THROWFORWARD.js: ends frame 33. Offset X is the source's own 14.19-4.61 expression, evaluated.</summary>
	public static readonly MoveDef ThrowForward = new(
		"ThrowForward", MoveCategory.GroundedNormal,
		firstActiveFrame: 13, lastActiveFrame: 13, totalFrames: 33, iasaFrame: 33,
		damage: 3, angleDegrees: 45, knockbackBase: 35, knockbackGrowth: 130,
		offsetX: Fx.Ratio(9580, 1000), offsetY: -Fx.Ratio(2805, 1000), radius: Fx.Ratio(0, 1000),
		hitboxes: new HitboxSpec[]
		{
			new(firstActiveFrame: 13, lastActiveFrame: 13, damage: 3, angleDegrees: 45,
				knockbackGrowth: 130, knockbackBase: 35,
				offsetX: Fx.Ratio(9580, 1000), offsetY: -Fx.Ratio(2805, 1000), radius: Fx.Ratio(0, 1000)),
		});

MoveDef FireBird = new(
		"FireBird", MoveCategory.Special,
		firstActiveFrame: 1, lastActiveFrame: 3, totalFrames: 45, iasaFrame: 45,
		damage: 14, angleDegrees: 80, knockbackBase: 60, knockbackGrowth: 60,
		launchSpeed: Fx.Ratio(3_800_000, 1_000_000), launchDecayPerTick: Fx.Ratio(100_000, 1_000_000),
		offsetX: Fx.Ratio(293, 100), offsetY: -Fx.Ratio(1172, 100), radius: Fx.Ratio(4000, 1000));

MoveDef Illusion = new(
		"Illusion", MoveCategory.Special,
		firstActiveFrame: 2, lastActiveFrame: 14, totalFrames: 40, iasaFrame: 40,
		damage: 7, angleDegrees: 80, knockbackBase: 68, knockbackGrowth: 40,
		launchSpeed: Fx.Ratio(18_720_000, 1_000_000), launchDecayPerTick: Fx.Ratio(468_000, 1_000_000),
		offsetX: Fx.Zero, offsetY: Fx.Zero, radius: Fx.Ratio(4_160_000, 1_000_000));

MoveDef ReflectorHit = new(
		"ReflectorHit", MoveCategory.Special,
		firstActiveFrame: 1, lastActiveFrame: 3, totalFrames: 40, iasaFrame: 40,
		damage: 5, angleDegrees: 0, knockbackBase: 0, knockbackGrowth: 100,
		offsetX: -Fx.Ratio(600, 1000), offsetY: -Fx.Ratio(6800, 1000), radius: Fx.Ratio(7999, 1000),
		setKnockback: 80,
		hitboxes: new HitboxSpec[]
		{
			// --- downspecial: the reflector's actual DAMAGING hit ---
            // attributes.js: size 7.999, dmg 5, angle 0, kg 100, bk 0, SK 80,
            // type 4 (electric). Previously this MoveDef carried the type-7
            // `reflector` box instead, which is the reflect FIELD and does 0
            // damage -- so the shine did nothing at all on contact. The set
            // knockback of 80 is what makes shine a fixed-distance popper
            // regardless of percent, which is the whole basis of shine combos.
            new(firstActiveFrame: 1, lastActiveFrame: 3, damage: 5, angleDegrees: 0,
                knockbackGrowth: 100, knockbackBase: 0,
                offsetX: -Fx.Ratio(600, 1000), offsetY: -Fx.Ratio(6800, 1000),
                radius: Fx.Ratio(7999, 1000), setKnockback: 80, hitType: 4),

            // --- reflector: the reflect field itself ---
            // Type 7, 0 damage, live for the whole state. Carried for data
            // completeness; CombatSystem skips reflect-type boxes because
            // projectile reflection is not implemented (see its own comment).
            new(firstActiveFrame: 1, lastActiveFrame: 40, damage: 0, angleDegrees: 361,
                knockbackGrowth: 100, knockbackBase: 0,
                offsetX: -Fx.Ratio(600, 1000), offsetY: -Fx.Ratio(6800, 1000),
                radius: Fx.Ratio(7999, 1000), hitType: 7),
        });

	/// <summary>
	/// Blaster (Neutral-B), grounded — NEUTRALSPECIALGROUND.js.
	///
	/// The move itself has NO hitbox: it spawns a travelling projectile on frame
	/// 12 and is otherwise pure animation, which is why it could not be expressed
	/// as a MoveDef until Gameplay/Projectile.cs existed. Hitboxes is deliberately
	/// left null and the flat damage/knockback fields are zero — the damage lives
	/// on the laser, not on Fox.
	///
	/// Frame data from the source: shot fires on timer 12, state ends past 40.
	/// Laser parameters are in ProjectileSpec.FoxLaser (see Projectile.cs) — the
	/// article definition lives in src/physics/article.js, a different subsystem
	/// from attributes.js, which is why searching the hitbox tables for it comes
	/// up empty.
	/// </summary>
	public static readonly MoveDef Blaster = new(
		"Blaster", MoveCategory.Special,
		firstActiveFrame: 0, lastActiveFrame: 0, totalFrames: 40, iasaFrame: 40,
		damage: 0, angleDegrees: 361, knockbackBase: 0, knockbackGrowth: 0);

	/// <summary>Action frame of <see cref="Blaster"/> on which the laser spawns.
	/// NEUTRALSPECIALGROUND.js: <c>if (player[p].timer === 12)</c>.</summary>
	public const int BlasterFireFrame = 12;


	/// <summary>Grounded normals, keyed for CharacterData's moveset table.</summary>
	public static readonly Dictionary<MoveSlot, MoveDef> GroundedNormals = new()
	{
		[MoveSlot.Jab1] = Jab1,
		[MoveSlot.Jab2] = Jab2,
		[MoveSlot.Jab3] = Jab3,
		[MoveSlot.ForwardTilt] = ForwardTilt,
		[MoveSlot.UpTilt] = UpTilt,
		[MoveSlot.DownTilt] = DownTilt,
		[MoveSlot.ForwardSmash] = ForwardSmash,
		[MoveSlot.UpSmash] = UpSmash,
		[MoveSlot.DownSmash] = DownSmash,
		[MoveSlot.DashAttack] = DashAttack,
	};

	/// <summary>All five aerials. DownAir is present for the first time - see the
	/// class doc on why it was previously impossible to represent.</summary>
	public static readonly Dictionary<MoveSlot, MoveDef> Aerials = new()
	{
		[MoveSlot.NeutralAir] = NeutralAir,
		[MoveSlot.ForwardAir] = ForwardAir,
		[MoveSlot.BackAir] = BackAir,
		[MoveSlot.UpAir] = UpAir,
		[MoveSlot.DownAir] = DownAir,
	};

	/// <summary>Grab, pummel, throws, and the situational get-up / ledge attacks.
	/// DATA is complete; DISPATCH for the grab chain and the ledge states is a
	/// state machine that does not exist yet. Carrying the data now costs nothing
	/// and means that work is not additionally blocked on transcription.</summary>
	public static readonly Dictionary<MoveSlot, MoveDef> GrabsAndSituational = new()
	{
		[MoveSlot.Grab] = Grab,
		[MoveSlot.Pummel] = Pummel,
		[MoveSlot.ThrowForward] = ThrowForward,
		[MoveSlot.ThrowBack] = ThrowBack,
		[MoveSlot.ThrowUp] = ThrowUp,
		[MoveSlot.ThrowDown] = ThrowDown,
		[MoveSlot.GetUpAttack] = GetUpAttack,
		[MoveSlot.LedgeAttackQuick] = LedgeAttackQuick,
		[MoveSlot.LedgeAttackSlow] = LedgeAttackSlow,
	};
}
