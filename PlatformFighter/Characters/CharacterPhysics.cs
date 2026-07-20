using PlatformFighter.Core.Math;

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
    public readonly FxVec2 HalfSize;

    public CharacterPhysics(
        Fx walkSpeed, Fx dashSpeed, Fx runSpeed, Fx groundAccel, Fx groundTraction, int dashInitiateFrames,
        Fx airSpeedMax, Fx airAccel,
        Fx gravity, Fx fallSpeedCap, Fx fastFallSpeedCap,
        int jumpSquatFrames, Fx shortHopVelocity, Fx fullHopVelocity, Fx doubleJumpVelocity, int extraJumps,
        int landingFrames, Fx weight, FxVec2 halfSize)
    {
        WalkSpeed = walkSpeed;
        DashSpeed = dashSpeed;
        RunSpeed = runSpeed;
        GroundAccel = groundAccel;
        GroundTraction = groundTraction;
        DashInitiateFrames = dashInitiateFrames;
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
        HalfSize = halfSize;
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
	/// HalfSize: REVERTED to the old flat 20x30 pixel placeholder after a prior
	/// version of this pass swapped it for MeleeLight's real ECB-derived size
	/// (~3.5 x 11) and broke hit detection — DeterminismTest.cs's
	/// HybridSelfPlayCombatTest spawns P1/P2 a fixed 30 (pixel-scale) units
	/// apart, which only lands a hit because CombatSystem's whole-body AABB
    /// overlap check (attacker.HalfSize + defender.HalfSize reach) exceeds that
    /// gap at the OLD size (20+20=40 > 30). At the ECB-derived size the combined
    /// reach was only ~7, so Jab1 whiffed every time — confirmed by re-running
    /// the F9 suite, which failed exactly this check. This is the same
    /// MeleeLight-native-units-vs-pixel-scale-engine mismatch flagged (and
	/// deliberately not chased) at the end of the physics pass: MeleeLight's
	/// ECB/ATTRIBUTES numbers are NOT in the same unit system as this engine's
	/// stage geometry and player spacing, so they can't be substituted in
	/// directly. Fixing this for real needs either (a) a world-scale constant
	/// that converts MeleeLight units to pixels consistently for every
	/// distance-based system (spacing, stage size, hurtbox, per-move hitbox
	/// offsets, all at once), or (b) rescaling the stage/spacing to MeleeLight's
    /// native units instead. Half-measures like this one — swapping ONE
    /// distance value to the other unit system while everything around it
    /// stays in pixels — break hit detection. Do that reconciliation before
    /// touching HalfSize again.</summary>
    public static CharacterPhysics FromFox() => new(
        walkSpeed: Fox.FoxAttributes.WalkSpeed,
        dashSpeed: Fox.FoxAttributes.InitialDashSpeed,
        runSpeed: Fox.FoxAttributes.RunSpeed,
        groundAccel: Fox.FoxAttributes.WalkAccelMeleeLight,
        groundTraction: Fox.FoxAttributes.GroundTraction,
        dashInitiateFrames: 12,
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
        halfSize: new FxVec2(Fx.FromInt(20), Fx.FromInt(30)));

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
        halfSize: new FxVec2(Fx.FromInt(20), Fx.FromInt(30)));
}
