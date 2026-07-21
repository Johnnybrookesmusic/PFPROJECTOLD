using System;
using System.Text;
using PlatformFighter.Characters.Fox;
using PlatformFighter.Core.Combat;
using PlatformFighter.Core.Input;
using PlatformFighter.Core.Math;
using PlatformFighter.Core.Sim;
using PlatformFighter.Core.Sim.Collision;
using PlatformFighter.Stages;
using PlatformFighter.Gameplay;

namespace PlatformFighter.Debug;

/// <summary>
/// The 2.1/2.2 (Phase 2), 4.1/4.2 (Phase 4), 5.1 (Phase 5), 6.1 (Phase 6),
/// and 7.1 (Phase 7) acceptance tests, runnable any time (F9 in Main).
/// These use throwaway SimWorlds driven directly with scripted inputs —
/// the live world on screen is never touched. Keep these passing
/// forever; the first phase that breaks them is the phase that just
/// broke rollback.
/// </summary>
public static class DeterminismTest
{
	public static string RunAll()
	{
		var log = new StringBuilder();
		bool twin = TwinRunTest(log);
		bool snap = SnapshotRestoreTest(log);
		bool colTwin = CollisionTwinRunTest(log);
		bool colGrounded = CollisionGroundedTest(log);
		bool colPlatform = CollisionPlatformLandTest(log);
		bool moverTwin = PlayerMoverTwinRunTest(log);
		bool moverJump = PlayerMoverJumpArcTest(log);
		bool actionState = PlayerActionStateTest(log);
		bool hitstun = PlayerMoverHitstunTest(log);
		bool hybridCombat = HybridSelfPlayCombatTest(log);
		bool locomotion = FoxLocomotionTest(log);
		bool blastZone = BlastZoneStockRespawnTest(log);
		bool allPass = twin && snap && colTwin && colGrounded && colPlatform && moverTwin && moverJump && actionState && hitstun && hybridCombat && locomotion && blastZone;
		log.Insert(0, allPass
			? "DETERMINISM TESTS: PASS\n"
			: "DETERMINISM TESTS: *** FAIL ***\n");
		return log.ToString();
	}

	private static SimWorld NewWorld()
	{
		SimObjectTypes.Register(TestMover.TypeIdValue, () => new TestMover(FxVec2.Zero));
		var world = new SimWorld(seed: 12345);
		world.Register(new TestMover(new FxVec2(Fx.FromInt(100), Fx.FromInt(300))));
		return world;
	}

	/// <summary>
	/// Deterministic pseudo-input, a pure function of the frame number —
	/// this doubles as the "recorded inputs" for the replay half of the
	/// snapshot test.
	/// </summary>
	private static FrameInput ScriptedInput(int frame)
{
	ButtonFlags buttons = ButtonFlags.None;
	if ((frame / 7) % 3 == 0) buttons |= ButtonFlags.Attack;
	if ((frame / 11) % 2 == 0) buttons |= ButtonFlags.Jump;
	if ((frame / 13) % 5 == 0) buttons |= ButtonFlags.LDigital;

	sbyte x  = (sbyte)((frame % 201) - 100);
	sbyte y  = (sbyte)(100 - (frame % 201));
	sbyte cx = (sbyte)(((frame * 3) % 201) - 100);
	sbyte cy = (sbyte)(((frame * 5) % 201) - 100);
	byte l   = (byte)((frame * 7) % 256);
	byte r   = (byte)((frame * 11) % 256);
	return new FrameInput(buttons, x, y, cx, cy, l, r);
}

	private static void TickScripted(SimWorld world, int frame)
	{
		Span<FrameInput> inputs = stackalloc FrameInput[SimWorld.MaxPlayers];
		for (int p = 0; p < SimWorld.MaxPlayers; p++)
			inputs[p] = p == 0 ? ScriptedInput(frame) : FrameInput.None;
		world.Tick(inputs);
	}

	/// <summary>2.1: two worlds, identical inputs → identical hash at 0 / 1000 / 10000.</summary>
	private static bool TwinRunTest(StringBuilder log)
	{
		var w1 = NewWorld();
		var w2 = NewWorld();
		int[] checkpoints = { 0, 1000, 10000 };
		bool pass = true;
		int frame = 0;

		foreach (int cp in checkpoints)
		{
			while (frame < cp)
			{
				TickScripted(w1, frame);
				TickScripted(w2, frame);
				frame++;
			}
			ulong h1 = w1.ComputeStateHash();
			ulong h2 = w2.ComputeStateHash();
			bool ok = h1 == h2;
			pass &= ok;
			log.AppendLine($"Twin run, frame {cp}: {h1:X16} / {h2:X16} -> {(ok ? "match" : "DIVERGED")}");
		}
		return pass;
	}

	/// <summary>2.2: snapshot at 100, sim 300 more, restore, replay the same inputs, hashes must match.</summary>
	private static bool SnapshotRestoreTest(StringBuilder log)
	{
		var world = NewWorld();
		for (int f = 0; f < 100; f++) TickScripted(world, f);

		Snapshot snap = world.CreateSnapshot();

		for (int f = 100; f < 400; f++) TickScripted(world, f);
		ulong firstRun = world.ComputeStateHash();

		world.RestoreSnapshot(snap);
		bool restoreOk = world.ComputeStateHash() == snap.Hash;

		for (int f = 100; f < 400; f++) TickScripted(world, f);
		ulong replay = world.ComputeStateHash();
		bool replayOk = replay == firstRun;

		log.AppendLine($"Restore returns to snapshot state: {(restoreOk ? "yes" : "*** NO ***")}");
		log.AppendLine($"Replay after restore, frame 400: {replay:X16} vs {firstRun:X16} -> {(replayOk ? "match" : "DIVERGED")}");
		return restoreOk && replayOk;
	}

	private static SimWorld NewCollisionWorld()
	{
		SimObjectTypes.Register(TestBody.TypeIdValue, () => new TestBody(FxVec2.Zero, TestStage.Default));
		var world = new SimWorld(seed: 999);
		world.Register(new TestBody(new FxVec2(Fx.FromInt(500), Fx.FromInt(100)), TestStage.Default));
		return world;
	}

	/// <summary>4.1: two worlds with a gravity+collision body reading the SAME scripted
	/// player-0 input, identical hash at 0 / 60 / 500 — proves world.GetInput() and
	/// CollisionResolver both stay bit-identical now that an object actually consumes input.</summary>
	private static bool CollisionTwinRunTest(StringBuilder log)
	{
		var w1 = NewCollisionWorld();
		var w2 = NewCollisionWorld();
		int[] checkpoints = { 0, 60, 500 };
		bool pass = true;
		int frame = 0;

		foreach (int cp in checkpoints)
		{
			while (frame < cp)
			{
				TickScripted(w1, frame);
				TickScripted(w2, frame);
				frame++;
			}
			ulong h1 = w1.ComputeStateHash();
			ulong h2 = w2.ComputeStateHash();
			bool ok = h1 == h2;
			pass &= ok;
			log.AppendLine($"Collision twin run, frame {cp}: {h1:X16} / {h2:X16} -> {(ok ? "match" : "DIVERGED")}");
		}
		return pass;
	}

	/// <summary>4.2: a body dropped above TestStage's floor with neutral input must come to
	/// rest exactly on the floor's top surface and report Grounded — not just hash-match
	/// while forever falling or tunneling through.</summary>
	private static bool CollisionGroundedTest(StringBuilder log)
	{
		SimObjectTypes.Register(TestBody.TypeIdValue, () => new TestBody(FxVec2.Zero, TestStage.Default));
		var world = new SimWorld(seed: 999);
		// x=100 is deliberately clear of the platform's [350,650] x-range so this
		// test exercises the floor, not the platform (see Debug/TestStage.cs) —
		// a body dropped in-range of the platform is SUPPOSED to land on it, per
		// Docs/COLLISION.md, and that's a different scenario than this test covers.
		var body = new TestBody(new FxVec2(Fx.FromInt(100), Fx.FromInt(100)), TestStage.Default);
		world.Register(body);

		// Neutral input throughout: 90 ticks (1.5s) is comfortably enough
		// to fall from y=100 onto the floor's top surface at y=450 and
		// settle (velocity re-zeroed by the resolver every subsequent tick).
		Span<FrameInput> neutral = stackalloc FrameInput[SimWorld.MaxPlayers];
		for (int f = 0; f < 90; f++) world.Tick(neutral);

		Fx restingY = Fx.FromInt(450) - Fx.FromInt(30); // floor top (500-50) minus body half-height (30)
		bool restingOnFloor = body.Position.Y == restingY;
		bool pass = body.Grounded && restingOnFloor;

		log.AppendLine($"Body grounded after falling: {(body.Grounded ? "yes" : "*** NO ***")}, " +
			$"resting Y = {body.Position.Y} (expected {restingY}) -> {(restingOnFloor ? "match" : "*** MISMATCH ***")}");
		return pass;
	}

	/// <summary>4.3: a body dropped in-range of TestStage's one-way platform (x=500, within
	/// the platform's [350,650] span) with neutral input must land ON the platform, not fall
	/// through to the floor — the counterpart to CollisionGroundedTest, which deliberately
	/// spawns clear of the platform to test the floor instead. This is the exact scenario
	/// that originally exposed a stale expected-value bug in CollisionGroundedTest.</summary>
	private static bool CollisionPlatformLandTest(StringBuilder log)
	{
		SimObjectTypes.Register(TestBody.TypeIdValue, () => new TestBody(FxVec2.Zero, TestStage.Default));
		var world = new SimWorld(seed: 999);
		var body = new TestBody(new FxVec2(Fx.FromInt(500), Fx.FromInt(100)), TestStage.Default);
		world.Register(body);

		// Neutral input throughout: 90 ticks (1.5s) is comfortably enough to
		// fall from y=100 onto the platform's top surface at y=300 and settle.
		Span<FrameInput> neutral = stackalloc FrameInput[SimWorld.MaxPlayers];
		for (int f = 0; f < 90; f++) world.Tick(neutral);

		Fx restingY = Fx.FromInt(300) - Fx.FromInt(30); // platform Y (300) minus body half-height (30)
		bool restingOnPlatform = body.Position.Y == restingY;
		bool pass = body.Grounded && restingOnPlatform;

		log.AppendLine($"Body grounded on platform: {(body.Grounded ? "yes" : "*** NO ***")}, " +
			$"resting Y = {body.Position.Y} (expected {restingY}) -> {(restingOnPlatform ? "match" : "*** MISMATCH ***")}");
		return pass;
	}

	private static SimWorld NewMoverWorld()
	{
		SimObjectTypes.Register(PlayerMover.TypeIdValue, () => new PlayerMover(FxVec2.Zero, Battlefield.Geometry));
		var world = new SimWorld(seed: 999);
		// x=100, same as CollisionGroundedTest — clear of the platform's [350,650] span.
		world.Register(new PlayerMover(new FxVec2(Fx.Zero, -Fx.FromInt(20)), Battlefield.Geometry));
		return world;
	}

	/// <summary>5.1: two worlds with a full PlayerMover (dash/run/jump/fast-fall state
	/// machine on top of CollisionResolver) reading the SAME scripted input, identical
	/// hash at 0 / 60 / 500 — proves the Phase 5 state machine is exactly as deterministic
	/// as TestBody's simpler placeholder physics was.</summary>
	private static bool PlayerMoverTwinRunTest(StringBuilder log)
	{
		var w1 = NewMoverWorld();
		var w2 = NewMoverWorld();
		int[] checkpoints = { 0, 60, 500 };
		bool pass = true;
		int frame = 0;

		foreach (int cp in checkpoints)
		{
			while (frame < cp)
			{
				TickScripted(w1, frame);
				TickScripted(w2, frame);
				frame++;
			}
			ulong h1 = w1.ComputeStateHash();
			ulong h2 = w2.ComputeStateHash();
			bool ok = h1 == h2;
			pass &= ok;
			log.AppendLine($"PlayerMover twin run, frame {cp}: {h1:X16} / {h2:X16} -> {(ok ? "match" : "DIVERGED")}");
		}
		return pass;
	}

	/// <summary>5.2: hold jump for the jump-squat window (full hop), release, and let
	/// gravity bring the mover back down — it must leave the ground, come back down, and
	/// re-settle on the SAME floor resting Y that CollisionGroundedTest expects (420),
	/// with a fresh double-jump re-granted on landing. Not a frame-perfect apex-height
	/// check (that's Phase 5 tuning work) — just proof the jump/gravity/landing loop
	/// closes without getting stuck airborne or tunneling.</summary>
	private static bool PlayerMoverJumpArcTest(StringBuilder log)
	{
		SimObjectTypes.Register(PlayerMover.TypeIdValue, () => new PlayerMover(FxVec2.Zero, Battlefield.Geometry));
		var world = new SimWorld(seed: 999);
		var mover = new PlayerMover(new FxVec2(Fx.Zero, -Fx.FromInt(20)), Battlefield.Geometry);
		world.Register(mover);

		Span<FrameInput> neutral = stackalloc FrameInput[SimWorld.MaxPlayers];
		Span<FrameInput> jump = stackalloc FrameInput[SimWorld.MaxPlayers];
		jump[0] = new FrameInput(ButtonFlags.Jump, 0, 0);

		// Settle onto the floor FIRST. Spawns are airborne above the stage now
		// (Step 2b), so this must wait for an actual landing rather than assume
		// one tick is enough -- guessing a tick count here is what made this
		// test double-jump from mid-air and land on the top platform instead.
		for (int f = 0; f < 120 && !mover.Grounded; f++) world.Tick(neutral);
		// then hold jump through the full jump-squat window
		// (full hop) and release — long enough to clear the squat, arc up,
		// and fall back down within the 120-tick budget.
		bool leftGround = false;
		for (int f = 0; f < 10; f++)
		{
			world.Tick(jump);
			if (!mover.Grounded) leftGround = true;
		}
		for (int f = 0; f < 110; f++) world.Tick(neutral);

		// Step 2: Battlefield's ground line is y=0 and Position is the FEET, so the
		// resting position is the ground plus the resolver's own snap epsilon.
		Fx restingY = Fx.Zero + SegmentCollisionResolver.SurfaceSnapEpsilon;
		bool resettled = mover.Grounded && mover.Position.Y == restingY;
		bool jumpsRestored = mover.JumpsRemaining == MovementConstants.ExtraJumps;
		bool pass = leftGround && resettled && jumpsRestored;

		log.AppendLine($"PlayerMover jump arc: left ground: {(leftGround ? "yes" : "*** NO ***")}, " +
			$"resettled at Y = {mover.Position.Y} (expected {restingY}): {(resettled ? "yes" : "*** NO ***")}, " +
			$"jumps restored: {(jumpsRestored ? "yes" : "*** NO ***")} -> {(pass ? "match" : "*** MISMATCH ***")}");
		return pass;
	}

	/// <summary>6.1: drives the exact same short-hop-then-land scenario as the Phase 5
	/// jump-arc test, but this time asserts on PlayerActionState directly — Idle before
	/// takeoff, JumpSquat while the button's held on the ground, Jump while rising,
	/// Fall after the apex, Landing for a short window on touchdown, and back to Idle.
	/// This is what proves Phase 6 is actually a state machine and not just a set of
	/// fields nobody reads — if a future phase changes movement code and this stops
	/// visiting all five states in order, something broke the derivation, not just the feel.</summary>
	private static bool PlayerActionStateTest(StringBuilder log)
	{
		SimObjectTypes.Register(PlayerMover.TypeIdValue, () => new PlayerMover(FxVec2.Zero, Battlefield.Geometry));
		var world = new SimWorld(seed: 999);
		var mover = new PlayerMover(new FxVec2(Fx.Zero, -Fx.FromInt(20)), Battlefield.Geometry);
		world.Register(mover);

		Span<FrameInput> neutral = stackalloc FrameInput[SimWorld.MaxPlayers];
		Span<FrameInput> jump = stackalloc FrameInput[SimWorld.MaxPlayers];
		jump[0] = new FrameInput(ButtonFlags.Jump, 0, 0);

		// Wait for the actual landing first (spawns are airborne above the stage
		// as of Step 2b), THEN clear the Landing window into steady-state Idle.
		// The old fixed tick count assumed the fighter spawned already resting;
		// it now spawns in the air, so a fixed count left it still falling and
		// the subsequent "jump" became a mid-air double jump -- no Idle, no
		// JumpSquat, and a landing on the top platform instead of the ground.
		for (int f = 0; f < 120 && !mover.Grounded; f++) world.Tick(neutral);
		for (int f = 0; f < MovementConstants.LandingFrames + 4; f++) world.Tick(neutral);
		bool sawIdle = mover.CurrentState == PlayerActionState.Idle;

		bool sawJumpSquat = false, sawJump = false, sawFall = false, sawLanding = false;
		for (int f = 0; f < 10; f++)
		{
			world.Tick(jump);
			if (mover.CurrentState == PlayerActionState.JumpSquat) sawJumpSquat = true;
			if (mover.CurrentState == PlayerActionState.Jump) sawJump = true;
		}

		// Run until it's back to Idle (bounded, so a real regression fails loudly
		// instead of hanging) rather than guessing a fixed frame budget that would
		// need to cover both the fall AND the Landing window clearing afterward.
		bool endedIdle = false;
		for (int f = 0; f < 300 && !endedIdle; f++)
		{
			world.Tick(neutral);
			if (mover.CurrentState == PlayerActionState.Fall) sawFall = true;
			if (mover.CurrentState == PlayerActionState.Landing) sawLanding = true;
			if (mover.CurrentState == PlayerActionState.Idle) endedIdle = true;
		}

		bool pass = sawIdle && sawJumpSquat && sawJump && sawFall && sawLanding && endedIdle;

		log.AppendLine("PlayerActionState sequence: " +
			$"Idle={(sawIdle ? "yes" : "NO")} JumpSquat={(sawJumpSquat ? "yes" : "NO")} " +
			$"Jump={(sawJump ? "yes" : "NO")} Fall={(sawFall ? "yes" : "NO")} " +
			$"Landing={(sawLanding ? "yes" : "NO")} EndedIdle={(endedIdle ? "yes" : "NO")} " +
			$"-> {(pass ? "match" : "*** MISMATCH ***")}");
		return pass;
	}

	/// <summary>7.1: apply the same Hitbox to two twin worlds' movers at the same frame
	/// (bypassing hit-detection, which doesn't exist yet — see Docs/COMBAT.md) and
	/// confirm ApplyHit + TickHitstun are exactly as deterministic as everything else:
	/// identical hashes after the hit, identical eventual recovery back to normal
	/// airborne/grounded states once hitstun runs out.</summary>
	private static bool PlayerMoverHitstunTest(StringBuilder log)
	{
		SimObjectTypes.Register(PlayerMover.TypeIdValue, () => new PlayerMover(FxVec2.Zero, Battlefield.Geometry));
		var hit = new Hitbox(
			damage: Fx.FromInt(10),
			knockbackBase: Fx.FromInt(20),
			knockbackGrowth: Fx.FromInt(30),
			dirX: Fx.Ratio(7, 10),
			dirY: -Fx.Ratio(7, 10));

		var w1 = new SimWorld(seed: 999);
		var m1 = new PlayerMover(new FxVec2(Fx.Zero, -Fx.FromInt(20)), Battlefield.Geometry);
		w1.Register(m1);
		var w2 = new SimWorld(seed: 999);
		var m2 = new PlayerMover(new FxVec2(Fx.Zero, -Fx.FromInt(20)), Battlefield.Geometry);
		w2.Register(m2);

		Span<FrameInput> neutral = stackalloc FrameInput[SimWorld.MaxPlayers];
		for (int f = 0; f < 10; f++) { w1.Tick(neutral); w2.Tick(neutral); }

		m1.ApplyHit(in hit, attackerFacingRight: true);
		m2.ApplyHit(in hit, attackerFacingRight: true);

		bool tookDamage = m1.Percent == Fx.FromInt(10);
		bool launchedAirborne = !m1.Grounded;

		// CurrentState is only recomputed inside Tick() (see PlayerMover's doc
		// comment on DeriveActionState) — checking it before any tick has run
		// since the hit would read the stale PRE-hit state. One tick is enough
		// for it to catch up; HitstunFramesRemaining is already set from the
		// ApplyHit call itself, so it's still > 0 after decrementing once.
		w1.Tick(neutral);
		w2.Tick(neutral);
		bool inHitstun = m1.CurrentState == PlayerActionState.Hitstun;

		bool hashesMatch = w1.ComputeStateHash() == w2.ComputeStateHash();
		bool recovered = false;
		for (int f = 0; f < 300 && !recovered; f++)
		{
			w1.Tick(neutral);
			w2.Tick(neutral);
			hashesMatch &= w1.ComputeStateHash() == w2.ComputeStateHash();
			if (m1.CurrentState != PlayerActionState.Hitstun) recovered = true;
		}

		bool pass = tookDamage && inHitstun && launchedAirborne && hashesMatch && recovered;

		log.AppendLine($"PlayerMover hitstun: took damage={(tookDamage ? "yes" : "NO")} " +
			$"(percent={m1.Percent}), entered hitstun={(inHitstun ? "yes" : "NO")}, " +
			$"launched airborne={(launchedAirborne ? "yes" : "NO")}, " +
			$"twin hashes matched throughout={(hashesMatch ? "yes" : "NO")}, " +
			$"recovered to a non-hitstun state={(recovered ? "yes" : "NO")} -> {(pass ? "match" : "*** MISMATCH ***")}");
		return pass;
	}
	/// <summary>9.1: two full worlds -- each with the real Phase 9 setup (two
	/// FoxFalcoHybrid PlayerMovers + a CombatSystem, exactly like Main.cs's live
	/// scene) -- reading the SAME scripted P1-attacks-P2 input, must hash-match
	/// at 0 / 60 / 300 exactly like every earlier phase's twin-run test, AND P2
	/// must have actually taken damage by frame 300 -- proving the whole Phase 9
	/// chain (attack dispatch -> active-hitbox window -> CombatSystem's AABB
	/// overlap check -> ApplyHit/knockback/hitstun -> hitlag on both sides) is
	/// wired together AND deterministic, not just one or the other.</summary>
	private static bool HybridSelfPlayCombatTest(StringBuilder log)
	{
		SimObjectTypes.Register(PlayerMover.TypeIdValue, () => new PlayerMover(FxVec2.Zero, Battlefield.Geometry));

		(SimWorld world, PlayerMover p1, PlayerMover p2) NewHybridWorld()
		{
			var w = new SimWorld(seed: 999);
			var m1 = new PlayerMover(new FxVec2(-Fx.FromInt(6), -Fx.FromInt(20)), Battlefield.Geometry, playerIndex: 0);
			var m2 = new PlayerMover(new FxVec2(Fx.FromInt(6), -Fx.FromInt(20)), Battlefield.Geometry, playerIndex: 1);
			m2.FacingRight = false;
			w.Register(m1);
			w.Register(m2);
			w.Register(new CombatSystem(m1, m2));
			return (w, m1, m2);
		}

		var (w1, p1a, p2a) = NewHybridWorld();
		var (w2, p1b, p2b) = NewHybridWorld();

		// P1 holds attack (A) with a neutral stick every frame: repeatedly fires
		// Jab1 (TryStartAttack only starts a new move when the mover is free to
		// act, so this naturally chains jab -> recover -> jab rather than
		// re-triggering mid-swing). P2 sits at neutral input the whole time --
		// this is a one-sided attack test, not a fairness test.
		Span<FrameInput> p1Attacks = stackalloc FrameInput[SimWorld.MaxPlayers];
		p1Attacks[0] = new FrameInput(ButtonFlags.Attack, 0, 0);

		int[] checkpoints = { 0, 60, 300 };
		bool pass = true;
		int frame = 0;
		foreach (int cp in checkpoints)
		{
			while (frame < cp)
			{
				w1.Tick(p1Attacks);
				w2.Tick(p1Attacks);
				frame++;
			}
			ulong h1 = w1.ComputeStateHash();
			ulong h2 = w2.ComputeStateHash();
			bool ok = h1 == h2;
			pass &= ok;
			log.AppendLine($"Hybrid combat twin run, frame {cp}: {h1:X16} / {h2:X16} -> {(ok ? "match" : "DIVERGED")}");
		}

		bool p2TookDamage = p2a.Percent > Fx.Zero;
		bool hashesStillMatch = w1.ComputeStateHash() == w2.ComputeStateHash();
		pass &= p2TookDamage && hashesStillMatch;

		log.AppendLine($"Hybrid combat: P2 took damage from P1's Jab1={(p2TookDamage ? "yes" : "*** NO ***")} " +
			$"(P2 percent={p2a.Percent}), final hashes match={(hashesStillMatch ? "yes" : "*** NO ***")} " +
			$"-> {(pass ? "match" : "*** MISMATCH ***")}");
		return pass;
	}

	/// <summary>
	/// 3.1 (Melee Lite Translation Directive, Step 3 — Fox locomotion): asserts the
	/// TRANSCRIBED MeleeLight ground movement produces MeleeLight's own numbers,
	/// not just "some movement happens."
	///
	/// Three properties, each of which would silently regress if someone swapped a
	/// formula back for a MoveToward() approximation:
	///  - Walk converges on walkMaxV * stick, exactly. WALK.js accelerates
	///    proportionally to the REMAINING gap, so a flat per-tick step gets the top
	///    speed right and the ramp wrong.
	///  - Dash into run converges on dMaxV (2.2), Fox's real run speed.
	///  - Dash-dancing reverses facing repeatedly WITHOUT travelling across the
	///    stage. That only works because SMASHTURN can be dashed out of on frame 2
	///    exactly; widen that gate and the fighter slides away instead of holding
	///    position, which is the bug this test exists to catch.
	/// </summary>
	private static bool FoxLocomotionTest(StringBuilder log)
	{
		SimObjectTypes.Register(PlayerMover.TypeIdValue, () => new PlayerMover(FxVec2.Zero, Battlefield.Geometry));

		// Spawn well to the LEFT so a speed test has runway. Battlefield's ground
		// is only +/-68.4 wide: the first draft of this test drove 150 ticks from
		// x=0, which walks clean off the stage long before it finishes measuring.
		static (SimWorld, PlayerMover) NewWorld(int startX = 0)
		{
			var w = new SimWorld(seed: 777);
			var m = new PlayerMover(new FxVec2(Fx.FromInt(startX), -Fx.FromInt(20)), Battlefield.Geometry, playerIndex: 0);
			w.Register(m);
			return (w, m);
		}

		static void Drive(SimWorld w, sbyte stickX, int ticks)
		{
			Span<FrameInput> inputs = stackalloc FrameInput[SimWorld.MaxPlayers];
			for (int f = 0; f < ticks; f++)
			{
				inputs[0] = new FrameInput(ButtonFlags.None, stickX, 0);
				w.Tick(inputs);
			}
		}

		static void Settle(SimWorld w, PlayerMover m)
		{
			Span<FrameInput> neutral = stackalloc FrameInput[SimWorld.MaxPlayers];
			for (int f = 0; f < 120 && !m.Grounded; f++) w.Tick(neutral);
			for (int f = 0; f < 8; f++) w.Tick(neutral);
		}

		Fx tolerance = Fx.Ratio(5, 100);

		// --- walk ---------------------------------------------------------
		var (w1, m1) = NewWorld(-50);
		Settle(w1, m1);
		Drive(w1, 60, 50);   // ~48 units of travel — converges well inside the stage
		Fx expectedWalk = FoxAttributes.WalkSpeed * Fx.Ratio(60, 100);
		bool walkOk = m1.Ground == GroundState.Walk && Fx.Abs(m1.Velocity.X - expectedWalk) < tolerance;

		// --- dash -> run --------------------------------------------------
		var (w2, m2) = NewWorld(-50);
		Settle(w2, m2);
		Drive(w2, 100, 40);  // dash impulse hits dMaxV almost immediately; 40 ticks stays on-stage
		bool runOk = m2.Ground == GroundState.Run && Fx.Abs(m2.Velocity.X - FoxAttributes.RunSpeed) < tolerance;

		// --- dash dance ---------------------------------------------------
		var (w3, m3) = NewWorld();
		Settle(w3, m3);
		Fx startX = m3.Position.X;
		bool startFacing = m3.FacingRight;
		int facingChanges = 0;
		bool prevFacing = startFacing;
		{
			Span<FrameInput> inputs = stackalloc FrameInput[SimWorld.MaxPlayers];
			sbyte stick = 100;
			for (int f = 0; f < 120; f++)
			{
				if (f % 10 == 0) stick = (sbyte)(-stick);
				inputs[0] = new FrameInput(ButtonFlags.None, stick, 0);
				w3.Tick(inputs);
				if (m3.FacingRight != prevFacing) { facingChanges++; prevFacing = m3.FacingRight; }
			}
		}
		Fx drift = Fx.Abs(m3.Position.X - startX);
		bool danceOk = facingChanges >= 4 && drift < Fx.FromInt(30);

		// --- determinism of all of the above ------------------------------
		var (w4, m4) = NewWorld();
		var (w5, m5) = NewWorld();
		Settle(w4, m4);
		Settle(w5, m5);
		{
			Span<FrameInput> a = stackalloc FrameInput[SimWorld.MaxPlayers];
			sbyte stick = 100;
			for (int f = 0; f < 200; f++)
			{
				if (f % 7 == 0) stick = (sbyte)(-stick);
				a[0] = new FrameInput(ButtonFlags.None, stick, 0);
				w4.Tick(a);
				w5.Tick(a);
			}
		}
		bool twinOk = w4.ComputeStateHash() == w5.ComputeStateHash();

		// --- ledge reach (regression) -------------------------------------
		// Battlefield's underside chamfer has a wall segment running (68.4,0) ->
		// (65,6), i.e. straight down from the ground lip. A grounded fighter's
		// feet sit at exactly y=0 — the wall's top edge — so a "touching counts
		// as overlapping" test blocked walking at x~62 and made the ledge
		// unreachable. That is a bug you would only notice by trying to walk off
		// the stage, so it gets an explicit assertion.
		var (w6, m6) = NewWorld(40);
		Settle(w6, m6);
		Drive(w6, 100, 60);
		bool reachedLedge = !m6.Grounded || m6.Position.X > Fx.FromInt(66);

		bool pass = walkOk && runOk && danceOk && twinOk && reachedLedge;
		log.AppendLine("Fox locomotion: " +
			$"walk={m1.Velocity.X} (expected {expectedWalk}) {(walkOk ? "yes" : "*** NO ***")}, " +
			$"run={m2.Velocity.X} (expected {FoxAttributes.RunSpeed}) {(runOk ? "yes" : "*** NO ***")}, " +
			$"dash-dance facing changes={facingChanges} drift={drift} {(danceOk ? "yes" : "*** NO ***")}, " +
			$"twin hashes match={(twinOk ? "yes" : "*** NO ***")}, " +
			$"ledge reachable (x={m6.Position.X}, grounded={m6.Grounded}) {(reachedLedge ? "yes" : "*** NO ***")} " +
			$"-> {(pass ? "match" : "*** MISMATCH ***")}");
		return pass;
	}

	/// <summary>Directive Phase 1 acceptance test: blast zone -> stock loss ->
	/// respawn -> invincibility -> repeat until elimination, plus twin-run
	/// determinism through the whole sequence. Positions are set directly
	/// (bypassing normal falling) since only the blast-zone/respawn/elimination
	/// REACTION is under test here, not how a fighter gets there.</summary>
	private static bool BlastZoneStockRespawnTest(StringBuilder log)
	{
		SimObjectTypes.Register(PlayerMover.TypeIdValue, () => new PlayerMover(FxVec2.Zero, Battlefield.Geometry));

		var w = new SimWorld(seed: 555);
		var m = new PlayerMover(FxVec2.Zero, Battlefield.Geometry, playerIndex: 0);
		w.Register(m);
		Span<FrameInput> neutral = stackalloc FrameInput[SimWorld.MaxPlayers];

		// Well below Battlefield's real blast zone (bottom = 108.8, see
		// Stages/Battlefield.cs) — any Y past that is a death this tick.
		FxVec2 belowBlastZone = new(Fx.Zero, Fx.FromInt(200));

		int startingStocks = PlayerMover.StartingStocks;
		bool firstDeathOk = false, invincibleAfterRespawn = false, respawnedInBounds = false;
		bool eliminatedAtZero = false, frozenAfterElimination = false;

		for (int death = 1; death <= startingStocks; death++)
		{
			m.Position = belowBlastZone;
			w.Tick(neutral);

			int expectedStocks = System.Math.Max(0, startingStocks - death);
			bool stocksOk = m.Stocks == expectedStocks;

			if (death == 1)
			{
				firstDeathOk = stocksOk;
				invincibleAfterRespawn = m.IsInvincible;
				respawnedInBounds = !Battlefield.Geometry.IsPastBlastZone(m.Position);
			}

			if (death == startingStocks)
			{
				eliminatedAtZero = m.IsEliminated && stocksOk;
				// An eliminated fighter must not react to anything further —
				// drive it into the blast zone again and confirm nothing changes.
				FxVec2 beforeExtraTick = m.Position;
				int stocksBefore = m.Stocks;
				m.Position = belowBlastZone;
				w.Tick(neutral);
				frozenAfterElimination = m.IsEliminated && m.Stocks == stocksBefore
					&& m.Position.X == belowBlastZone.X && m.Position.Y == belowBlastZone.Y; // Tick() returns immediately, doesn't even respawn-teleport
				_ = beforeExtraTick;
			}
			else
			{
				// Clear invincibility before forcing the next death, so each
				// iteration is testing a fresh blast-zone-death reaction, not
				// getting skipped by IsInvincible from the previous respawn.
				for (int f = 0; f < PlayerMover.RespawnInvincibilityFrames; f++)
				{
					m.Position = FxVec2.Zero; // stay safely inside the blast zone
					w.Tick(neutral);
				}
			}
		}

		// --- twin-run determinism through the whole death/respawn/elimination
		// sequence -----------------------------------------------------------
		bool twinOk;
		{
			var (w1, m1) = NewDeathWorld();
			var (w2, m2) = NewDeathWorld();
			RunDeathSequence(w1, m1);
			RunDeathSequence(w2, m2);
			twinOk = w1.ComputeStateHash() == w2.ComputeStateHash();
		}

		static (SimWorld, PlayerMover) NewDeathWorld()
		{
			var world = new SimWorld(seed: 555);
			var mover = new PlayerMover(FxVec2.Zero, Battlefield.Geometry, playerIndex: 0);
			world.Register(mover);
			return (world, mover);
		}

		static void RunDeathSequence(SimWorld world, PlayerMover mover)
		{
			Span<FrameInput> input = stackalloc FrameInput[SimWorld.MaxPlayers];
			for (int death = 1; death <= PlayerMover.StartingStocks; death++)
			{
				mover.Position = new FxVec2(Fx.Zero, Fx.FromInt(200));
				world.Tick(input);
				int settle = death == PlayerMover.StartingStocks ? 5 : PlayerMover.RespawnInvincibilityFrames;
				for (int f = 0; f < settle; f++)
				{
					if (!mover.IsEliminated) mover.Position = FxVec2.Zero;
					world.Tick(input);
				}
			}
		}

		bool pass = firstDeathOk && invincibleAfterRespawn && respawnedInBounds
			&& eliminatedAtZero && frozenAfterElimination && twinOk;
		log.AppendLine("Blast zone / stocks / respawn: " +
			$"first death stocks={m.Stocks} {(firstDeathOk ? "yes" : "*** NO ***")}, " +
			$"invincible after respawn={(invincibleAfterRespawn ? "yes" : "*** NO ***")}, " +
			$"respawned in bounds={(respawnedInBounds ? "yes" : "*** NO ***")}, " +
			$"eliminated at 0 stocks={(eliminatedAtZero ? "yes" : "*** NO ***")}, " +
			$"frozen after elimination={(frozenAfterElimination ? "yes" : "*** NO ***")}, " +
			$"twin hashes match={(twinOk ? "yes" : "*** NO ***")} " +
			$"-> {(pass ? "match" : "*** MISMATCH ***")}");
		return pass;
	}
}
