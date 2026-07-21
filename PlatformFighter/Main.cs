using Godot;
using PlatformFighter.Characters.Fox;
using PlatformFighter.Core;
using PlatformFighter.Core.Math;
using PlatformFighter.Core.Rendering;
using PlatformFighter.Core.Sim;
using PlatformFighter.Debug;
using PlatformFighter.Gameplay;
using PlatformFighter.Stages;

namespace PlatformFighter;

/// <summary>
/// Master Directive v2 demo scene: two pure-Fox (Characters/Fox/FoxCharacter.cs)
/// PlayerMovers plus a CombatSystem tying them together — Fox vs. Fox on
/// Battlefield, no Falco data anywhere in the loop. P1 reads
/// device slot 0 (keyboard, always present — see Docs/INPUT.md), P2 reads
/// device slot 1 (a second controller if one's connected, otherwise idle
/// input) — same self-play-vs-real-second-player duality the Phase 9 session
/// that built PlayerMover's attack dispatch called for. Both sprites render
/// so the hybrid can be watched fighting itself with just P1 attacking, or
/// played by two people with two controllers.
/// </summary>
public partial class Main : Node2D
{
	private SimDriver _driver = null!;
	private PlayerMover _p1 = null!;
	private PlayerMover _p2 = null!;
	private Sprite2D _sprite = null!;
	private Sprite2D? _sprite2;
	private Label _debugLabel = null!;

	private bool _f9WasDown;
	private string _testResult = "";

	/// <summary>
	/// Closes the single biggest gap left after Phase 9-11: the sim (physics,
	/// hitboxes, hitlag, specials, self-play combat) was all real and tested,
	/// but the scene rendered nothing but an empty TestSprite with no texture,
	/// no P2 node, and no stage — "just the code behind the scenes," per the
	/// project owner. No new art pipeline exists yet (Phase 8/Animation is
	/// still not started), so this builds flat-color placeholder boxes
	/// procedurally at runtime — sized/colored/positioned FROM the real sim
	/// data (CharacterPhysics.Ecb, Battlefield.Geometry's real segments/platforms)
	/// rather than hardcoded guesses — so what you see on screen is an honest
	/// picture of the sim, not decoration. Swapping in real animated sprites
	/// later is a texture-assignment change, not a rewrite of this method.
	/// </summary>
	public override void _Ready()
	{
		_driver = GetNode<SimDriver>("SimDriver");
		_sprite = GetNode<Sprite2D>("TestSprite");
		_debugLabel = GetNode<Label>("DebugLabel");

		// P2's sprite is optional in the scene tree — older saved scenes from
		// Phase 5-8 only have TestSprite. Falls back to null (P2 still ticks
		// and fights in the sim, it just won't be drawn) rather than throwing,
		// so this doesn't hard-require a .tscn edit to keep building.
		if (HasNode("TestSprite2"))
			_sprite2 = GetNode<Sprite2D>("TestSprite2");

				// Step 2: one transform node carries sim->screen (scale + origin) for
		// everything, so all render code below stays in sim units.
		_world = new Node2D
		{
			Name = "World",
			Scale = new Vector2(RenderScale, RenderScale),
			Position = WorldOrigin
		};
		AddChild(_world);

		// Directive v3: initialize the Melee-style camera from the real stage
		// geometry and current viewport. The initial RenderScale/WorldOrigin above
		// remain as the first-frame fallback until _Process begins driving _world.
		_camera = new CameraController(
			Battlefield.Geometry,
			GetViewportRect().Size);

		// Godot's own Node.Reparent. keepGlobalTransform:false is deliberate — we
		// WANT these to inherit _world's sim-to-screen transform, not to be
		// compensated back to their old screen positions. _Process repositions them
		// from sim state every frame anyway.
		_sprite.Reparent(_world, keepGlobalTransform: false);
		_sprite2?.Reparent(_world, keepGlobalTransform: false);

		SpawnFighters();
		BuildPlaceholderVisuals();
	}

	private Sprite2D _shadow1 = null!;
private Sprite2D? _shadow2;
private Node2D _world = null!;
private CameraController _camera = null!;
private Fx _floorTopY = Fx.Zero; // sim-space ground height; set in BuildPlaceholderVisuals

	/// <summary>Step 2: sim units are MeleeLight units and the RENDER layer converts
	/// to pixels — the sim never inflates itself to suit a camera. 4.5 is not a
	/// guess: it is Battlefield's own <c>scale</c> field in MeleeLight's
	/// <c>src/stages/vs-stages/battlefield.js</c> (alongside <c>offset:[600,480]</c>),
	/// confirmed there to be consumed only by stagerender.js and never by physics.js.
	/// Applied once, as the transform on <see cref="_world"/>, so every piece of render
	/// code below keeps working in plain sim units instead of scattering conversions.</summary>
	private const float RenderScale = 4.5f;

	/// <summary>Where sim (0,0) — Battlefield's ground centre — lands on screen.
	/// MeleeLight's own offset is [600,480] for its canvas; this is the same idea
	/// retargeted to the 1152x648 viewport, leaving the stage (±68.4 sim = ±308 px)
	/// comfortably framed with the top platform (54.4 sim = 245 px up) on screen.</summary>
	private static readonly Vector2 WorldOrigin = new(576f, 420f);

	/// <summary>Fighter textures are authored at this multiple of sim size and then
	/// scaled back down, so a 6x13-unit fighter is not a literal 6x13-pixel image
	/// stretched 4.5x into mush.</summary>
	private const int TextureOversample = 4;

	/// <summary>Shaded placeholder boxes (gradient + rim light + a facing "visor" mark)
	/// for both fighters, sized from CharacterPhysics.HalfSize, plus a beveled floor/
	/// platforms from Battlefield.Geometry and drop shadows under each fighter — all
	/// generated at runtime, no external asset files. Purely cosmetic over the same
	/// flat-box placeholder; nothing here changes sim data or hurtbox size.</summary>
	private void BuildPlaceholderVisuals()
	{
		// Sized from the REAL ECB now (Step 2), not the old 20x30 pixel placeholder:
		// 6 wide x 13 tall in sim units. Authored oversampled and scaled back down so
		// it isn't a 6x13-pixel image stretched 4.5x.
		var ecb = FoxCharacter.Instance.Physics.Ecb;
		int w = System.Math.Max(1, (int)(ecb.HalfWidth.ToFloat() * 2f * TextureOversample));
		int h = System.Math.Max(1, (int)(ecb.TopHeight.ToFloat() * TextureOversample));

		// Battlefield's ground line is the shadow plane.
		_floorTopY = Battlefield.Geometry.Ground.Count > 0 ? Battlefield.Geometry.Ground[0].A.Y : Fx.Zero;

		_sprite.Texture = MakeFighterTexture(w, h, new Color(0.22f, 0.5f, 1f));   // P1: blue
		AnchorFighterSprite(_sprite, h);
		if (_sprite2 != null)
		{
			_sprite2.Texture = MakeFighterTexture(w, h, new Color(1f, 0.32f, 0.32f)); // P2: red
			AnchorFighterSprite(_sprite2, h);
		}

		_shadow1 = MakeShadowSprite();
		_world.AddChild(_shadow1);
		if (_sprite2 != null)
		{
			_shadow2 = MakeShadowSprite();
			_world.AddChild(_shadow2);
		}

		var stageNode = new Node2D { Name = "StageVisual" };
		stageNode.ZIndex = -1;

		// Step 2: draw the REAL Battlefield (Step 1's transcribed geometry) instead of
		// TestStage's debug slab. Ground segments are drawn as a thick slab hanging
		// below the collision line; platforms as thin bars. The diagonal underside
		// chamfers (Ceiling/WallL/WallR) are not drawn yet — they exist as collision
		// data and would need a polygon, not a rect. Placeholder visuals, still not
		// Phase 8.
		foreach (var seg in Battlefield.Geometry.Ground)
		{
			Fx halfW = (seg.MaxX - seg.MinX) / Fx.FromInt(2);
			Fx thickness = Fx.FromInt(6);
			var aabb = new Core.Sim.Collision.FxAabb(
				new FxVec2((seg.MinX + seg.MaxX) / Fx.FromInt(2), seg.A.Y + thickness),
				new FxVec2(halfW, thickness));
			stageNode.AddChild(MakeBeveledRect(aabb, new Color(0.32f, 0.33f, 0.38f)));
		}

		foreach (var plat in Battlefield.Geometry.Platforms)
		{
			var aabb = new Core.Sim.Collision.FxAabb(
				new FxVec2((plat.XMin + plat.XMax) / Fx.FromInt(2), plat.Y),
				new FxVec2((plat.XMax - plat.XMin) / Fx.FromInt(2), Fx.Ratio(15, 10)));
			stageNode.AddChild(MakeBeveledRect(aabb, new Color(0.45f, 0.46f, 0.52f)));
		}

		_world.AddChild(stageNode);
	}

	/// <summary>Sim Position is the FEET (Step 2, see Ecb.cs), but Sprite2D centres its
	/// texture on Position — so shift the texture up by half its height. Without this
	/// every fighter renders buried to the waist in the floor.</summary>
	private static void AnchorFighterSprite(Sprite2D sprite, int textureHeight)
	{
		sprite.Scale = new Vector2(1f / TextureOversample, 1f / TextureOversample);
		sprite.Offset = new Vector2(0f, -textureHeight / 2f);
	}

	/// <summary>Vertical gradient (lighter top, darker bottom, like a top-down key light),
	/// a 2px dark outline, a lighter rim-light strip down the left edge, and a small dark
	/// "visor" mark in the upper-right — the sprite's default facing — so FlipH in
	/// _Process gives a real visible facing cue instead of a symmetric, faceless box.</summary>
	private static ImageTexture MakeFighterTexture(int w, int h, Color baseColor)
	{
		w = System.Math.Max(1, w);
		h = System.Math.Max(1, h);
		var image = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
		Color dark = baseColor.Darkened(0.45f);
		Color light = baseColor.Lightened(0.35f);
		Color outline = baseColor.Darkened(0.65f);

		for (int y = 0; y < h; y++)
		{
			float t = (float)y / System.Math.Max(1, h - 1); // 0 top -> 1 bottom
			Color row = baseColor.Lerp(dark, t * 0.8f);
			for (int x = 0; x < w; x++)
			{
				bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
				Color c = edge ? outline : row;
				if (!edge && x <= w / 6) c = row.Lerp(light, 0.5f); // left rim light
				image.SetPixel(x, y, c);
			}
		}

		// Visor mark: a dark horizontal band in the upper-right quadrant, standing in
		// for a face/goggles — gives FlipH something to actually flip.
		int visorY0 = h / 6, visorY1 = h / 3;
		int visorX0 = w * 3 / 5, visorX1 = w - 2;
		for (int y = visorY0; y < visorY1; y++)
			for (int x = visorX0; x < visorX1; x++)
				image.SetPixel(x, y, outline);

		return ImageTexture.CreateFromImage(image);
	}

	/// <summary>Floor/platform boxes with a lighter top edge and darker underside — a
	/// cheap bevel that reads as "solid ground with a top surface" instead of a flat tile.</summary>
	private static Sprite2D MakeBeveledRect(Core.Sim.Collision.FxAabb aabb, Color baseColor)
	{
		int w = System.Math.Max(1, (int)(aabb.HalfSize.X.ToFloat() * 2f));
		int h = System.Math.Max(1, (int)(aabb.HalfSize.Y.ToFloat() * 2f));
		var image = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
		Color top = baseColor.Lightened(0.3f);
		Color bottom = baseColor.Darkened(0.35f);
		int bevel = System.Math.Max(1, h / 6);

		for (int y = 0; y < h; y++)
		{
			Color row = y < bevel ? top : baseColor.Lerp(bottom, (float)(y - bevel) / System.Math.Max(1, h - bevel));
			for (int x = 0; x < w; x++)
				image.SetPixel(x, y, row);
		}

		return new Sprite2D
		{
			Texture = ImageTexture.CreateFromImage(image),
			Position = new Vector2(aabb.Center.X.ToFloat(), aabb.Center.Y.ToFloat()),
		};
	}

	private static Sprite2D MakeShadowSprite()
	{
		const int w = 44, h = 14;
		var image = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
		Vector2 center = new(w / 2f, h / 2f);
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				float dx = (x - center.X) / (w / 2f);
				float dy = (y - center.Y) / (h / 2f);
				float dist = dx * dx + dy * dy;
				float alpha = System.Math.Clamp(0.45f * (1f - dist), 0f, 0.45f);
				image.SetPixel(x, y, new Color(0, 0, 0, alpha));
			}
		}
		return new Sprite2D { Texture = ImageTexture.CreateFromImage(image), ZIndex = -1 };
	}

	private void UpdateShadow(Sprite2D shadow, Vector2 fighterPos) =>
		shadow.Position = new Vector2(fighterPos.X, _floorTopY.ToFloat());

	private void SpawnFighters()
	{
		// Snapshot restore needs a factory for every registered type. Both
		// PlayerMover slots default to the hybrid (character: null ->
		// FoxFalcoHybrid.Instance in PlayerMover's own constructor) — see
		// that constructor's fallback. CombatSystem's factory intentionally
		// throws; see its class doc comment's "KNOWN GAP" note.
		SimObjectTypes.Register(PlayerMover.TypeIdValue, () => new PlayerMover(FxVec2.Zero, Battlefield.Geometry));
		SimObjectTypes.Register(CombatSystem.TypeIdValue, () =>
			throw new System.NotSupportedException(
				"CombatSystem can't be cold-rebuilt from a type id alone (needs two live " +
				"PlayerMover references) -- see CombatSystem's class doc comment. Every real " +
				"call site constructs it explicitly alongside its two PlayerMovers instead."));

		// Step 2: real units. Battlefield's ground is y=0 spanning +/-68.4; Position is
		// the FEET, so y=0 is standing on it. Spawned 24 apart (~18% of stage width) —
		// close enough that Fox's jab reach (CombatSystem.AttackReach) can connect, and
		// far enough to actually walk/dash between. The old (300,300)/(700,300) pair was
		// pixel-scale and would now be a full blast zone apart.
		_p1 = new PlayerMover(new(-Fx.FromInt(12), -Fx.FromInt(10)), Battlefield.Geometry, playerIndex: 0);
		_p2 = new PlayerMover(new(Fx.FromInt(12), -Fx.FromInt(10)), Battlefield.Geometry, playerIndex: 1);
		_p2.FacingRight = false; // face P1 on spawn, same as a real match's starting stance

		_driver.World.Register(_p1);
		_driver.World.Register(_p2);
		// Registered AFTER both movers -> ticks after both this frame, per
		// SimWorld's ordered-tick-order guarantee (see CombatSystem's doc
		// comment) -- so TryGetActiveHitbox reflects this tick's attack state.
		_driver.World.Register(new CombatSystem(_p1, _p2));
	}

	public override void _Process(double delta)
	{
		float a = _driver.RenderAlpha;
		_sprite.Position = _p1.PreviousPosition.ToGodot()
							   .Lerp(_p1.Position.ToGodot(), a);
		_sprite.FlipH = !_p1.FacingRight;
		UpdateShadow(_shadow1, _sprite.Position);

				if (_sprite2 != null)
		{
			_sprite2.Position = _p2.PreviousPosition.ToGodot()
								   .Lerp(_p2.Position.ToGodot(), a);
			_sprite2.FlipH = !_p2.FacingRight;
			if (_shadow2 != null)
				UpdateShadow(_shadow2, _sprite2.Position);
		}

		// Melee-style framing camera (Directive v3): frame on both fighters'
		// interpolated sim positions and drive _world's zoom+pan. Render-only —
		// reads sim state, never writes it. Uses the same RenderAlpha-interpolated
		// positions the sprites use so camera and fighters move in lockstep.
		Vector2 camP1 = _p1.PreviousPosition.ToGodot()
			.Lerp(_p1.Position.ToGodot(), a);
		Vector2 camP2 = _p2.PreviousPosition.ToGodot()
			.Lerp(_p2.Position.ToGodot(), a);

		var (worldScale, worldPos) = _camera.Update(camP1, camP2, (float)delta);

		_world.Scale = worldScale;
		_world.Position = worldPos;

		// F9 = run the determinism acceptance tests (view-side key, edge-triggered).
		bool f9 = Input.IsPhysicalKeyPressed(Key.F9);
		if (f9 && !_f9WasDown)
		{
			_testResult = DeterminismTest.RunAll();
			GD.Print(_testResult);
		}
		_f9WasDown = f9;

		var p1Input = _driver.World.GetInput(0);
		_debugLabel.Text =
			$"Sim frame: {_driver.World.FrameNumber}\n" +
			$"Render FPS: {Engine.GetFramesPerSecond()}\n" +
			$"Ticks this render frame: {_driver.TicksLastFrame}\n" +
			$"Alpha: {a:0.00}\n" +
			$"P1 input: {p1Input}\n" +
			$"P1 state: {_p1.CurrentState} move: {_p1.CurrentMove} percent: {_p1.Percent} stocks: {_p1.Stocks}\n" +
			$"P2 state: {_p2.CurrentState} move: {_p2.CurrentMove} percent: {_p2.Percent} stocks: {_p2.Stocks}\n" +
			$"State hash: {_driver.LatestHash:X16}\n" +
			$"[F9] run determinism tests\n" +
			_testResult;
	}
}
