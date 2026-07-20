using Godot;
using PlatformFighter.Characters.Hybrid;
using PlatformFighter.Core;
using PlatformFighter.Core.Math;
using PlatformFighter.Core.Sim;
using PlatformFighter.Debug;
using PlatformFighter.Gameplay;

namespace PlatformFighter;

/// <summary>
/// Phase 9 demo scene: two Fox/Falco hybrid PlayerMovers plus a CombatSystem
/// tying them together, replacing the single-mover Phase 5-8 scene. P1 reads
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
	/// data (CharacterPhysics.HalfSize, TestStage.Default's solids/platforms)
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

		SpawnFighters();
		BuildPlaceholderVisuals();
	}

	private Sprite2D _shadow1 = null!;
	private Sprite2D? _shadow2;
	private float _floorTopY = 450f; // fallback; overwritten from TestStage.Default in BuildPlaceholderVisuals

	/// <summary>Shaded placeholder boxes (gradient + rim light + a facing "visor" mark)
	/// for both fighters, sized from CharacterPhysics.HalfSize, plus a beveled floor/
	/// platform from TestStage.Default and drop shadows under each fighter — all
	/// generated at runtime, no external asset files. Purely cosmetic over the same
	/// flat-box placeholder; nothing here changes sim data or hurtbox size.</summary>
	private void BuildPlaceholderVisuals()
	{
		var halfSize = FoxFalcoHybrid.Instance.Physics.HalfSize;
		int w = (int)(halfSize.X.ToFloat() * 2f);
		int h = (int)(halfSize.Y.ToFloat() * 2f);

		foreach (var solid in TestStage.Default.Solids)
			_floorTopY = System.Math.Min(_floorTopY, solid.Top.ToFloat());

		_sprite.Texture = MakeFighterTexture(w, h, new Color(0.22f, 0.5f, 1f));   // P1: blue
		if (_sprite2 != null)
			_sprite2.Texture = MakeFighterTexture(w, h, new Color(1f, 0.32f, 0.32f)); // P2: red

		_shadow1 = MakeShadowSprite();
		AddChild(_shadow1);
		if (_sprite2 != null)
		{
			_shadow2 = MakeShadowSprite();
			AddChild(_shadow2);
		}

		var stageNode = new Node2D { Name = "StageVisual" };
		AddChild(stageNode);
		stageNode.ZIndex = -1;

		foreach (var solid in TestStage.Default.Solids)
			stageNode.AddChild(MakeBeveledRect(solid, new Color(0.32f, 0.33f, 0.38f)));

		foreach (var plat in TestStage.Default.Platforms)
		{
			var aabb = new Core.Sim.Collision.FxAabb(
				new FxVec2((plat.XMin + plat.XMax) / Fx.FromInt(2), plat.Y),
				new FxVec2((plat.XMax - plat.XMin) / Fx.FromInt(2), Fx.FromInt(4)));
			stageNode.AddChild(MakeBeveledRect(aabb, new Color(0.45f, 0.46f, 0.52f)));
		}
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
		shadow.Position = new Vector2(fighterPos.X, _floorTopY);

	private void SpawnFighters()
	{
		// Snapshot restore needs a factory for every registered type. Both
		// PlayerMover slots default to the hybrid (character: null ->
		// FoxFalcoHybrid.Instance in PlayerMover's own constructor) — see
		// that constructor's fallback. CombatSystem's factory intentionally
		// throws; see its class doc comment's "KNOWN GAP" note.
		SimObjectTypes.Register(PlayerMover.TypeIdValue, () => new PlayerMover(FxVec2.Zero, TestStage.Default));
		SimObjectTypes.Register(CombatSystem.TypeIdValue, () =>
			throw new System.NotSupportedException(
				"CombatSystem can't be cold-rebuilt from a type id alone (needs two live " +
				"PlayerMover references) -- see CombatSystem's class doc comment. Every real " +
				"call site constructs it explicitly alongside its two PlayerMovers instead."));

		_p1 = new PlayerMover(new(Fx.FromInt(300), Fx.FromInt(300)), TestStage.Default, playerIndex: 0);
		_p2 = new PlayerMover(new(Fx.FromInt(700), Fx.FromInt(300)), TestStage.Default, playerIndex: 1);
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
			if (_shadow2 != null) UpdateShadow(_shadow2, _sprite2.Position);
		}

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
			$"P1 state: {_p1.CurrentState} move: {_p1.CurrentMove} percent: {_p1.Percent}\n" +
			$"P2 state: {_p2.CurrentState} move: {_p2.CurrentMove} percent: {_p2.Percent}\n" +
			$"State hash: {_driver.LatestHash:X16}\n" +
			$"[F9] run determinism tests\n" +
			_testResult;
	}
}
