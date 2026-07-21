using PlatformFighter.Core.Math;
using PlatformFighter.Core.Sim.Collision;

namespace PlatformFighter.Characters;

/// <summary>
/// Phase 9: per-instance movement physics, replacing Gameplay/MovementConstants.cs's
/// shared placeholder — that file's own doc comment predicted exactly this
/// ("Per-character stats arrive in Phase 9 ... PlayerMover should take these
/// as constructor/data parameters instead of a single shared struct"). Same
/// field shape/units (per-tick, 1/60s) as MovementConstants so PlayerMover's
/// existing math is a drop-in field-name swap, not a rewrite.
///
/// FromFox() is real data (Characters/Fox/FoxAttributes.cs — PlFx.dat +
/// MeleeLight attributes.js, cross-validated, disagreements documented there).
/// FromFalco() is MeleeLight's attributes.js ONLY, not cross-checked against a
/// real PlFc.dat parse the way Fox's was — treat it as a reasonable stand-in
/// for Phase 11 (second fighter), not final data. The Fox/Falco hybrid
/// (Characters/Hybrid/FoxFalcoHybrid.cs) uses FromFox() exclusively — the
/// synopsis's "Fox base gameplay" calls for Fox's movement/weight/jumps, full
/// stop; FromFalco() exists here for whenever Falco becomes his own fighter.
/// </summary>
public readonly struct CharacterPhysics
{
    // ---- Ground movement ----------------------------------------------
    public readonly Fx WalkSpeed;
    public readonly Fx DashSpeed;
    public readonly Fx RunSpeed;
    public readonly Fx GroundAccel;
    public readonly Fx GroundTraction;
    public readonly int DashInitiateFrames;

    // ---- Step 3: real MeleeLight ground-locomotion stats -------------------
    // These are what DASH.js / RUN.js / WALK.js actually read. They were all
    // already transcribed in FoxAttributes.cs and sitting unused; Step 3 wires
	// them in. Names map to MeleeLight's attribute keys, given in each comment.
	/// <summary>MeleeLight <c>walkInitV</c>. Part of WALK.js's acceleration
    /// factor, NOT a starting speed despite the name.</summary>
    public readonly Fx WalkInitialSpeed;
    /// <summary>MeleeLight <c>dInitV</c> — the impulse DASH.js adds on its 2nd frame.</summary>
    public readonly Fx DashInitialSpeed;
    /// <summary>MeleeLight <c>dTInitV</c> — dash-TURN initial velocity. Distinct
    /// from <see cref="DashInitialSpeed"/>; conflating the two was a latent bug
    /// FoxAttributes.cs already flagged.</summary>
    public readonly Fx DashTurnSpeed;
    /// <summary>MeleeLight <c>dAccA</c> — first-stage dash/run acceleration.</summary>
    public readonly Fx DashAccelA;
    /// <summary>MeleeLight <c>dAccB</c> — second-stage dash/run acceleration.</summary>
    public readonly Fx DashAccelB;
    /// <summary>MeleeLight <c>dashFrameMin</c> — earliest dash frame that can
    /// transition into RUN.</summary>
    public readonly int DashFrameMin;
    /// <summary>MeleeLight <c>dashFrameMax</c> — after this a fresh dash input
    /// re-dashes instead of running. This is the dash-dance window.</summary>
    public readonly int DashFrameMax;
    /// <summary>MeleeLight <c>framesData.DASH</c> — total dash animation length;
    /// past it, DASH falls back to WAIT.</summary>
    public readonly int DashTotalFrames;

    // ---- Air movement -----------------------------------------------------
    public readonly Fx AirSpeedMax;
    public readonly Fx AirAccel;

    // ---- Gravity / falling ------------------------------------------------
    public readonly Fx Gravity;
    public readonly Fx FallSpeedCap;
    public readonly Fx FastFallSpeedCap;

    // ---- Jumping ------------------------------------------------------------
    public readonly int JumpSquatFrames;
    public readonly Fx ShortHopVelocity;
    public readonly Fx FullHopVelocity;
    public readonly Fx DoubleJumpVelocity;
    public readonly int ExtraJumps;

    // ---- Landing / misc -----------------------------------------------------
    public readonly int LandingFrames;
    public readonly Fx Weight;

    /// <summary>Step 2: the real Environmental Collision Box (see Core/Sim/Collision/Ecb.cs).
    /// This is what fighter-vs-stage resolution actually uses now. Feet-origin.</summary>
    public readonly Ecb Ecb;

    /// <summary>DERIVED from <see cref="Ecb"/>, kept only for the systems that still
	/// want a symmetric centre+halfsize box (CombatSystem's hurtbox overlap). It is no
	/// longer independent data that can drift out of sync with the collision shape —
	/// which is exactly how it came to be 6.7x too large (see FoxEcb.cs's header).</summary>
    public FxVec2 HalfSize => new(Ecb.HalfWidth, Ecb.TopHeight / (Fx.One + Fx.One));

    public CharacterPhysics(
        Fx walkSpeed, Fx dashSpeed, Fx runSpeed, Fx groundAccel, Fx groundTraction, int dashInitiateFrames,
        Fx walkInitialSpeed, Fx dashInitialSpeed, Fx dashTurnSpeed, Fx dashAccelA, Fx dashAccelB,
        int dashFrameMin, int dashFrameMax, int dashTotalFrames,
        Fx airSpeedMax, Fx airAccel,
        Fx gravity, Fx fallSpeedCap, Fx fastFallSpeedCap,
        int jumpSquatFrames, Fx shortHopVelocity, Fx fullHopVelocity, Fx doubleJumpVelocity, int extraJumps,
        int landingFrames, Fx weight, Ecb ecb)
    {
        WalkSpeed = walkSpeed;
        DashSpeed = dashSpeed;
        RunSpeed = runSpeed;
        GroundAccel = groundAccel;
        GroundTraction = groundTraction;
        DashInitiateFrames = dashInitiateFrames;
        WalkInitialSpeed = walkInitialSpeed;
        DashInitialSpeed = dashInitialSpeed;
        DashTurnSpeed = dashTurnSpeed;
        DashAccelA = dashAccelA;
        DashAccelB = dashAccelB;
        DashFrameMin = dashFrameMin;
        DashFrameMax = dashFrameMax;
        DashTotalFrames = dashTotalFrames;
        AirSpeedMax = airSpeedMax;
        AirAccel = airAccel;
        Gravity = gravity;
        FallSpeedCap = fallSpeedCap;
        FastFallSpeedCap = fastFallSpeedCap;
        JumpSquatFrames = jumpSquatFrames;
        ShortHopVelocity = shortHopVelocity;
        FullHopVelocity = fullHopVelocity;
        DoubleJumpVelocity = doubleJumpVelocity;
        ExtraJumps = extraJumps;
        LandingFrames = landingFrames;
        Weight = weight;
        Ecb = ecb;
    }

    /// <summary>Updated this pass to resolve every DAT-vs-MeleeLight disagreement
	/// FoxAttributes.cs's header had flagged, IN FAVOR OF MeleeLight — per explicit
	/// instruction that MeleeLight's own working, playable numbers are the source
    /// of truth for this recreation, not the raw DAT dump. Concretely:
	///   - groundAccel now uses WalkAccelMeleeLight (0.2) instead of GroundAccel (DAT's 0.1).
	///   - dashSpeed now uses InitialDashSpeed (MeleeLight dInitV, 2.02 — the actual
	///     dash-START speed) instead of DashSpeed (dTInitV, 1.9 — a dash-TURN value;
	///     using the turn value for straight dash-start was itself a latent bug).
	/// DashInitiateFrames (12) is still the MovementConstants placeholder — no
	/// MeleeLight dash-lock-frame field was found this pass either.
	///
	/// HalfSize/ECB: RESOLVED in Step 2. The old flat 20x30 pixel placeholder made
	/// Fox 40 units wide on Step 1's real 136.8-wide Battlefield -- 29.2% of the whole
    /// stage, against a true 4.4%. A previous pass reverted the fix because swapping
	/// HalfSize alone broke DeterminismTest's combat check (P1/P2 spawned a fixed 30
	/// apart, which only landed a hit at the inflated size). That reasoning was right --
	/// a half-measure IS worse -- but the fix was to move the spawn distances too, which
	/// Step 2 does. Velocities/gravity/traction were ALREADY in MeleeLight units and
	/// consistent with the stage; body size was the sole outlier, so there is no
	/// whole-engine rescale. Render scale (4.5, from battlefield.js) is the render
	/// layer's job -- see Main.cs. HalfSize is now DERIVED from Ecb and cannot drift
    /// from it again.</summary>
    public static CharacterPhysics FromFox() => new(
        walkSpeed: Fox.FoxAttributes.WalkSpeed,
        dashSpeed: Fox.FoxAttributes.InitialDashSpeed,
        runSpeed: Fox.FoxAttributes.RunSpeed,
        groundAccel: Fox.FoxAttributes.WalkAccelMeleeLight,
        groundTraction: Fox.FoxAttributes.GroundTraction,
        dashInitiateFrames: 12,
        walkInitialSpeed: Fox.FoxAttributes.InitialWalkSpeedMeleeLight,
        dashInitialSpeed: Fox.FoxAttributes.InitialDashSpeed,
        dashTurnSpeed: Fox.FoxAttributes.DashSpeed,
        dashAccelA: Fox.FoxAttributes.DashAccelA,
        dashAccelB: Fox.FoxAttributes.DashAccelB,
        dashFrameMin: 11,
        dashFrameMax: 21,
        dashTotalFrames: 21,
        airSpeedMax: Fox.FoxAttributes.AirSpeedMax,
        airAccel: Fox.FoxAttributes.AirAccel,
        gravity: Fox.FoxAttributes.Gravity,
        fallSpeedCap: Fox.FoxAttributes.FallSpeedCap,
        fastFallSpeedCap: Fox.FoxAttributes.FastFallSpeedCap,
        jumpSquatFrames: Fox.FoxAttributes.JumpSquatFrames,
        shortHopVelocity: Fox.FoxAttributes.ShortHopVelocity,
        fullHopVelocity: Fox.FoxAttributes.FullHopVelocity,
        doubleJumpVelocity: Fox.FoxAttributes.DoubleJumpVelocity,
        extraJumps: Fox.FoxAttributes.ExtraJumps,
        landingFrames: Fox.FoxAttributes.LandingFrames,
        weight: Fox.FoxAttributes.Weight,
        ecb: Fox.FoxEcb.Default);

    /// <summary>MeleeLight src/characters/falco/attributes.js only — see class-level
    /// doc comment. Not consumed by the Phase 9 hybrid (which uses FromFox()
    /// exclusively); kept ready for Phase 11 (second fighter, Falco standalone).</summary>
    public static CharacterPhysics FromFalco() => new(
        walkSpeed: Fx.Ratio(1_400_000, 1_000_000),
        dashSpeed: Fx.Ratio(1_900_000, 1_000_000),
        runSpeed: Fx.Ratio(1_500_000, 1_000_000),
        groundAccel: Fx.Ratio(100_000, 1_000_000),
        groundTraction: Fx.Ratio(80_000, 1_000_000),
        dashInitiateFrames: 12,
        walkInitialSpeed: Fx.Ratio(200_000, 1_000_000),   // falco walkInitV 0.2
        dashInitialSpeed: Fx.Ratio(1_820_000, 1_000_000), // falco dInitV 1.82
        dashTurnSpeed: Fx.Ratio(1_900_000, 1_000_000),    // falco dTInitV 1.9
        dashAccelA: Fx.Ratio(100_000, 1_000_000),         // falco dAccA 0.1
        dashAccelB: Fx.Ratio(20_000, 1_000_000),          // falco dAccB 0.02
        dashFrameMin: 11,
        dashFrameMax: 21,
        dashTotalFrames: 21,
        airSpeedMax: Fx.Ratio(830_000, 1_000_000),
        airAccel: Fx.Ratio(50_000, 1_000_000),
        gravity: Fx.Ratio(170_000, 1_000_000),
        fallSpeedCap: Fx.Ratio(3_100_000, 1_000_000),
        fastFallSpeedCap: Fx.Ratio(3_500_000, 1_000_000),
        jumpSquatFrames: 5,
        shortHopVelocity: Fx.Ratio(1_900_000, 1_000_000),
        fullHopVelocity: Fx.Ratio(4_100_000, 1_000_000),
        doubleJumpVelocity: Fx.Ratio(4_100_000, 1_000_000) * Fx.Ratio(94, 100),
        extraJumps: 1,
        landingFrames: 4,
        weight: Fx.FromInt(80),
        ecb: Fox.FoxEcb.Default);
}
