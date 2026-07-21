using PlatformFighter.Core.Math;
using PlatformFighter.Core.Sim.Collision;

namespace PlatformFighter.Stages;

/// <summary>
/// Step 1 of the Melee Lite Translation Directive: Battlefield collision and
/// blast zones. Transcribed directly and exactly from MeleeLight's real
/// source — <c>src/stages/vs-stages/battlefield.js</c> — not approximated,
/// not eyeballed from a screenshot.
///
/// COORDINATE CONVENTION: MeleeLight's stage data is Y-UP (confirmed by
/// reading, not assumed: <c>src/characters/*/moves/FALL.js</c> etc. apply
/// gravity as <c>cVel.y -= gravity</c>, i.e. falling makes Y more NEGATIVE;
/// <c>physics.js</c>'s blast-zone check is <c>pos.y &lt; blastzone.min.y</c>
/// for falling off the BOTTOM, meaning "down" is the smaller/more-negative
/// Y direction). This engine's <see cref="FxVec2"/>/<see cref="FxAabb"/> are
/// explicitly +Y-DOWN (see FxAabb.cs's own doc comment) — the same
/// Y-up-to-Y-down adaptation this codebase already made for Gravity's sign
/// when porting Fox's attributes (PlayerMover adds Gravity to make Velocity.Y
/// more positive = more "down"), not a new convention invented for this file.
/// Every Y coordinate below is therefore the source value NEGATED; every X
/// coordinate is unchanged. All source values have exactly one decimal
/// digit, so ×10 is exact — every constant here is Fx.Ratio(n, 10), never a
/// float literal.
///
/// NOT PORTED HERE, and why (so this isn't mistaken for an oversight):
///  - <c>polygon</c> / <c>box</c> / <c>scale</c> / <c>offset</c> — confirmed
///    by grep that MeleeLight's own physics.js/stage.js never read these;
///    they're consumed ONLY by stagerender.js for drawing the stage's visual
///    shape on an HTML canvas. Render-only, so out of scope for "collision
///    and blast zones." Revisit when a visual/rendering step exists.
///  - <c>connected</c> (the ground-to-ground adjacency graph used when a
///    stage has multiple touching ground pieces, e.g. walking from one
///    platform edge onto another without falling) — Battlefield's own
///    battlefield.js never defines this field (it's optional on MeleeLight's
///    own Stage type), because Battlefield has exactly one ground segment
///    with nothing adjacent to either edge. Omitting it here matches the
///    source exactly; it is not a gap.
///  - Camera bounds — MeleeLight's battlefield.js has no per-stage camera
///    field at all (checked directly: no "camera" key on the stage object,
///    and physics.js's only camera-related line is a stage-agnostic
///    out-of-camera sound trigger). There is no authoritative constant to
///    port. Not inventing one — flagging it as genuinely unresolved for
///    whoever adds camera framing later.
///  - The actual ECB-vs-segment collision RESOLUTION algorithm (walking
///    along ground, falling off edges, teetering, low-ceiling blocking —
///    <c>physics.js</c>'s dealWithGround/fallOffGround/moveAlongGround) is
///    NOT implemented by this file. This file is DATA: real segments, real
///    blast zone, real spawn/ledge points. Resolving a fighter's ECB against
///    these segments is Step 2 (Core physics) — see StageGeometry.cs's and
///    FxSegment.cs's own scope notes for why bundling that in here would
///    violate the whole reason for doing this in 6 ordered steps.
/// </summary>
public static class Battlefield
{
	/// <summary>Same instance every call — deterministic across snapshot rebuilds,
	/// same convention as Debug/TestStage.Default.</summary>
	public static readonly StageGeometry Geometry = Build();

	private static StageGeometry Build()
	{
		var stage = new StageGeometry();

		// ---- Ground: the main platform's top surface. Battlefield has
		// exactly one ground segment spanning its full width. ----
		stage.AddGround(new FxSegment(
			new FxVec2(-Fx.Ratio(684, 10), Fx.Ratio(0, 10)),
			new FxVec2(Fx.Ratio(684, 10), Fx.Ratio(0, 10))));

		// ---- Ceiling: the underside chamfers' flat caps (5 segments) —
		// collide when a fighter recovering from below runs into the
		// underside of the main platform. ----
		stage.AddCeiling(new FxSegment(new FxVec2(-Fx.Ratio(650, 10), Fx.Ratio(60, 10)), new FxVec2(-Fx.Ratio(360, 10), Fx.Ratio(190, 10))));
		stage.AddCeiling(new FxSegment(new FxVec2(-Fx.Ratio(290, 10), Fx.Ratio(350, 10)), new FxVec2(-Fx.Ratio(100, 10), Fx.Ratio(400, 10))));
		stage.AddCeiling(new FxSegment(new FxVec2(-Fx.Ratio(100, 10), Fx.Ratio(300, 10)), new FxVec2(Fx.Ratio(100, 10), Fx.Ratio(300, 10))));
		stage.AddCeiling(new FxSegment(new FxVec2(Fx.Ratio(650, 10), Fx.Ratio(60, 10)), new FxVec2(Fx.Ratio(360, 10), Fx.Ratio(190, 10))));
		stage.AddCeiling(new FxSegment(new FxVec2(Fx.Ratio(290, 10), Fx.Ratio(350, 10)), new FxVec2(Fx.Ratio(100, 10), Fx.Ratio(400, 10))));

		// ---- WallL: 6 segments (named for wall orientation in MeleeLight,
		// not stage side — see StageGeometry.cs's doc comment). Five form the
		// left underside chamfer's steps; the sixth (x=10, y −30..−40) is one
		// side of the small central notch. ----
		stage.AddWallL(new FxSegment(new FxVec2(-Fx.Ratio(684, 10), Fx.Ratio(0, 10)), new FxVec2(-Fx.Ratio(650, 10), Fx.Ratio(60, 10))));
		stage.AddWallL(new FxSegment(new FxVec2(-Fx.Ratio(360, 10), Fx.Ratio(190, 10)), new FxVec2(-Fx.Ratio(390, 10), Fx.Ratio(210, 10))));
		stage.AddWallL(new FxSegment(new FxVec2(-Fx.Ratio(390, 10), Fx.Ratio(210, 10)), new FxVec2(-Fx.Ratio(330, 10), Fx.Ratio(250, 10))));
		stage.AddWallL(new FxSegment(new FxVec2(-Fx.Ratio(330, 10), Fx.Ratio(250, 10)), new FxVec2(-Fx.Ratio(300, 10), Fx.Ratio(290, 10))));
		stage.AddWallL(new FxSegment(new FxVec2(-Fx.Ratio(300, 10), Fx.Ratio(290, 10)), new FxVec2(-Fx.Ratio(290, 10), Fx.Ratio(350, 10))));
		stage.AddWallL(new FxSegment(new FxVec2(Fx.Ratio(100, 10), Fx.Ratio(300, 10)), new FxVec2(Fx.Ratio(100, 10), Fx.Ratio(400, 10))));

		// ---- WallR: mirror of WallL (5 segments for the right chamfer, 1 for
		// the notch's other side, x=−10). ----
		stage.AddWallR(new FxSegment(new FxVec2(Fx.Ratio(684, 10), Fx.Ratio(0, 10)), new FxVec2(Fx.Ratio(650, 10), Fx.Ratio(60, 10))));
		stage.AddWallR(new FxSegment(new FxVec2(Fx.Ratio(360, 10), Fx.Ratio(190, 10)), new FxVec2(Fx.Ratio(390, 10), Fx.Ratio(210, 10))));
		stage.AddWallR(new FxSegment(new FxVec2(Fx.Ratio(390, 10), Fx.Ratio(210, 10)), new FxVec2(Fx.Ratio(330, 10), Fx.Ratio(250, 10))));
		stage.AddWallR(new FxSegment(new FxVec2(Fx.Ratio(330, 10), Fx.Ratio(250, 10)), new FxVec2(Fx.Ratio(300, 10), Fx.Ratio(290, 10))));
		stage.AddWallR(new FxSegment(new FxVec2(Fx.Ratio(300, 10), Fx.Ratio(290, 10)), new FxVec2(Fx.Ratio(290, 10), Fx.Ratio(350, 10))));
		stage.AddWallR(new FxSegment(new FxVec2(-Fx.Ratio(100, 10), Fx.Ratio(300, 10)), new FxVec2(-Fx.Ratio(100, 10), Fx.Ratio(400, 10))));

		// ---- Platforms: two side platforms + one top platform, all flat —
		// real one-way platforms, so these use OneWayPlatform (Y + X range),
		// not FxSegment. Y negated same as everything else here. ----
		stage.AddPlatform(new OneWayPlatform(-Fx.Ratio(272, 10), -Fx.Ratio(576, 10), -Fx.Ratio(200, 10)));
		stage.AddPlatform(new OneWayPlatform(-Fx.Ratio(272, 10), Fx.Ratio(200, 10), Fx.Ratio(576, 10)));
		stage.AddPlatform(new OneWayPlatform(-Fx.Ratio(544, 10), -Fx.Ratio(188, 10), Fx.Ratio(188, 10)));

		// ---- Blast zone: MeleeLight's Box2D([-224,-108.8],[224,200]) (X
		// unchanged; Y negated — since negating swaps which corner is
		// min/max on that axis, FromMinMax is given both raw corners and
		// sorts it out, rather than this file guessing which of the negated
		// values is now "min"). ----
		stage.SetBlastZone(FxAabb.FromMinMax(
			new FxVec2(Fx.FromInt(-224), Fx.Ratio(1088, 10)),
			new FxVec2(Fx.FromInt(224), -Fx.FromInt(200))));

		// ---- Ledges: MeleeLight's ledgePos — exactly the ground segment's
		// own two endpoints (verified by direct comparison with the ground
		// array above). ----
		stage.AddLedge(new FxVec2(-Fx.Ratio(684, 10), Fx.Ratio(0, 10)));
		stage.AddLedge(new FxVec2(Fx.Ratio(684, 10), Fx.Ratio(0, 10)));

		// ---- Starting points + facing: MeleeLight's startingPoint/startingFace
		// (parallel arrays there; paired into FacingPoint here — see its own
		// doc comment on why). Order matches source: P1, P2, P3, P4. ----
		stage.AddStartingPoint(new FxVec2(-Fx.Ratio(500, 10), -Fx.Ratio(500, 10)), 1);
		stage.AddStartingPoint(new FxVec2(Fx.Ratio(500, 10), -Fx.Ratio(500, 10)), -1);
		stage.AddStartingPoint(new FxVec2(-Fx.Ratio(250, 10), -Fx.Ratio(50, 10)), 1);
		stage.AddStartingPoint(new FxVec2(Fx.Ratio(250, 10), -Fx.Ratio(50, 10)), -1);

		// ---- Respawn points + facing: MeleeLight's respawnPoints/respawnFace.
		// Note points 3/4 differ from the starting points above (y=35 vs y=5,
		// pre-flip) — that's real source data, not a copy-paste error: Melee
		// respawn platforms sit higher than the P3/P4 starting positions. ----
		stage.AddRespawnPoint(new FxVec2(-Fx.Ratio(500, 10), -Fx.Ratio(500, 10)), 1);
		stage.AddRespawnPoint(new FxVec2(Fx.Ratio(500, 10), -Fx.Ratio(500, 10)), -1);
		stage.AddRespawnPoint(new FxVec2(-Fx.Ratio(250, 10), -Fx.Ratio(350, 10)), 1);
		stage.AddRespawnPoint(new FxVec2(Fx.Ratio(250, 10), -Fx.Ratio(350, 10)), -1);

		return stage;
	}
}
