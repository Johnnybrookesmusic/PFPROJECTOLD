using PlatformFighter.Core.Math;

namespace PlatformFighter.Characters.Fox;

/// <summary>
/// Real Fox attribute data. Originally converted from the <c>PlFx.dat</c>
/// extraction (<c>fox_physics.json</c>) — now cross-validated against
/// MeleeLight's <c>src/characters/fox/attributes.js</c>, a real, labeled,
/// working open-source engine (not a re-derivation), which resolved several
/// of the DAT dump's unlabeled/unknown fields and caught a couple of
/// straight-up discrepancies. Every field notes BOTH sources where they
/// overlap; where they disagree, that's called out explicitly rather than
/// silently picking one.
///
/// This is the real-data counterpart to <c>Gameplay/MovementConstants.cs</c>'s
/// placeholders — same field shape, same "per tick" (1/60s) unit convention —
/// but it is NOT wired into <c>PlayerMover</c> yet. That's Phase 9 (Character
/// framework): turning the shared static <c>MovementConstants</c> into
/// per-instance/per-character data.
///
/// Values are <c>Fx.Ratio(numerator, 1_000_000)</c> — the source double
/// rounded to 6 decimal digits (past float32's ~7 significant digits), kept
/// as a plain integer literal per <c>Core/Math/Fx.cs</c>'s "no float
/// literals in sim code" rule.
///
/// RESOLVED from DAT "Unknown" fields, via MeleeLight's attributes.js:
///   - Unknown0x54 (0.9)  == MeleeLight's djMomentum   (double-jump horizontal
///     momentum retention multiplier — not a thing this file wires in yet,
///     since PlayerMover.TickAirborne always steers straight at AirSpeedMax
///     rather than preserving incoming momentum; see MovementConstants.cs's
///     "What's NOT done" list, which already flagged this exact gap).
///   - Unknown0x3C (0.72) == MeleeLight's jumpHinitV   (initial horizontal
///     speed applied by a GROUND jump's takeoff frame, distinct from
///     InitialJumpHorizontalSpeed/jumpHmaxV, the cap that speed can grow
///     toward with air control afterward).
///
/// DISAGREEMENTS between the DAT dump and MeleeLight's attributes.js (both
/// kept below, DAT field name used for the property name since that's this
/// file's existing convention — do not assume the DAT value is the correct
/// one just because it's primary here):
///   - InitialWalkSpeed: DAT 0.2 vs MeleeLight walkInitV 0.16.
///   - WalkAcceleration: DAT 0.1 vs MeleeLight walkAcc 0.2 (suspiciously
///     exactly swapped with the line above — worth checking for a transcription
///     mixup on one side before trusting either).
///   - ShieldBreakInitialVelocity: DAT 3.3 vs MeleeLight shieldBreakVel 2.5.
///
/// STILL OPEN (MeleeLight didn't resolve these):
///   - RunSpeed field ambiguity: DAT's InitialRunSpeed (2.2) matches
///     MeleeLight's dMaxV (2.2, "dash max velocity") and fightcore.gg's
///     displayed Run Speed — used below. DAT's literal "RunSpeed" field
///     (1.728) still has no confirmed meaning; MeleeLight's dAccA/dAccB
///     (0.1/0.02, two-stage dash acceleration) are in a similar magnitude
///     range but don't cleanly match it either. RunAccelerationFieldRaw
///     (DAT's 30.0) remains unexplained — MeleeLight's actual dash/run
///     acceleration constants (dAccA/dAccB) are two orders of magnitude
///     smaller, so 30.0 is still most likely a different unit entirely
///     (frame count?), not a velocity-delta-per-tick accel.
/// </summary>
public static class FoxAttributes
{
    // ---- Identity -----------------------------------------------------
    /// <summary>DAT: Weight; MeleeLight: weight. Agree: 75.</summary>
    public static readonly Fx Weight = Fx.FromInt(75);

    // ---- Ground movement ------------------------------------------------
    /// <summary>DAT: MaxWalkSpeed; MeleeLight: walkMaxV. Agree: 1.6.</summary>
    public static readonly Fx WalkSpeed = Fx.Ratio(1_600_000, 1_000_000);
	/// <summary>DAT: InitialWalkSpeed (0.2). DISAGREES with MeleeLight's walkInitV (0.16) — see file header.</summary>
	public static readonly Fx InitialWalkSpeed = Fx.Ratio(200_000, 1_000_000);
	/// <summary>MeleeLight: walkInitV (0.16), kept alongside InitialWalkSpeed pending the discrepancy above.</summary>
	public static readonly Fx InitialWalkSpeedMeleeLight = Fx.Ratio(160_000, 1_000_000);
	/// <summary>DAT: WalkAcceleration (0.1). DISAGREES with MeleeLight's walkAcc (0.2) — see file header.</summary>
    public static readonly Fx GroundAccel = Fx.Ratio(100_000, 1_000_000);
    /// <summary>MeleeLight: walkAcc (0.2), kept alongside GroundAccel pending the discrepancy above.</summary>
    public static readonly Fx WalkAccelMeleeLight = Fx.Ratio(200_000, 1_000_000);
    /// <summary>DAT: Friction; MeleeLight: traction. Agree: 0.08.</summary>
    public static readonly Fx GroundTraction = Fx.Ratio(80_000, 1_000_000);
    /// <summary>DAT: InitialDashSpeed; MeleeLight: dTInitV ("dash turn initial velocity"). Agree: 1.9.</summary>
    public static readonly Fx DashSpeed = Fx.Ratio(1_900_000, 1_000_000);
	/// <summary>MeleeLight: dInitV (2.02) — the actual dash-START speed; distinct from DashSpeed/dTInitV above, which is a dash-TURN value. fightcore.gg's screenshot for Fox's "Initial Dash" also shows 2.02, matching this, not DashSpeed. Not yet reconciled with how PlayerMover.TickGrounded uses a single DashSpeed constant for both purposes.</summary>
    public static readonly Fx InitialDashSpeed = Fx.Ratio(2_020_000, 1_000_000);
    /// <summary>DAT: InitialRunSpeed; MeleeLight: dMaxV ("dash max velocity"). Agree: 2.2. See file header re: the OTHER "RunSpeed"-named field in the DAT dump, which is still unresolved.</summary>
    public static readonly Fx RunSpeed = Fx.Ratio(2_200_000, 1_000_000);
    /// <summary>DAT: RunSpeed (1.728), kept raw and UNUSED — still unresolved, see file header.</summary>
    public static readonly Fx RunSpeedFieldRaw = Fx.Ratio(1_728_000, 1_000_000);
    /// <summary>DAT: RunAcceleration (30.0), kept raw and UNUSED — still unresolved, see file header.</summary>
    public static readonly Fx RunAccelerationFieldRaw = Fx.FromInt(30);
    /// <summary>MeleeLight: dAccA — first-stage dash/run acceleration.</summary>
    public static readonly Fx DashAccelA = Fx.Ratio(100_000, 1_000_000);
    /// <summary>MeleeLight: dAccB — second-stage dash/run acceleration.</summary>
    public static readonly Fx DashAccelB = Fx.Ratio(20_000, 1_000_000);
	/// <summary>DAT: StopTurnInitialSpeed (0.1) — pivot/turnaround kick speed, not yet consumed by PlayerMover's turn-around logic.</summary>
	public static readonly Fx StopTurnInitialSpeed = Fx.Ratio(100_000, 1_000_000);
	/// <summary>DAT: TiltTurnForcedVelocity (3.0).</summary>
	public static readonly Fx TiltTurnForcedVelocity = Fx.FromInt(3);
	/// <summary>MeleeLight: runTurnBreakPoint (16) — no DAT equivalent found; likely a percent-of-max-speed or frame threshold for when a run-direction-reversal becomes a pivot vs. a full stop. Not yet consumed anywhere.</summary>
	public const int RunTurnBreakPoint = 16;

	// ---- Air movement -----------------------------------------------------
	/// <summary>DAT: MaxAerialHorizontalSpeed; MeleeLight: aerialHmaxV. Agree: 0.83.</summary>
	public static readonly Fx AirSpeedMax = Fx.Ratio(830_000, 1_000_000);
	/// <summary>DAT: AerialSpeed; MeleeLight: airMobA. Agree: 0.06 — first-stage air acceleration.</summary>
	public static readonly Fx AirAccel = Fx.Ratio(60_000, 1_000_000);
	/// <summary>DAT: AerialFriction; MeleeLight: airFriction. Agree: 0.02.</summary>
	public static readonly Fx AirFriction = Fx.Ratio(20_000, 1_000_000);
	/// <summary>MeleeLight: airMobB (0.02) — second-stage air acceleration constant with no DAT-side equivalent found; not yet consumed anywhere (PlayerMover.TickAirborne only uses one AirAccel constant today).</summary>
	public static readonly Fx AirAccelB = Fx.Ratio(20_000, 1_000_000);

	// ---- Gravity / falling ------------------------------------------------
	/// <summary>DAT: Gravity; MeleeLight: gravity. Agree: 0.23 — CONFIRMS this is the real per-tick fall
	/// acceleration, resolving the open question the previous pass of this file left about
	/// fightcore.gg's "Gravity" column (2.8) actually being fall speed, not gravity — see FallSpeedCap.</summary>
    public static readonly Fx Gravity = Fx.Ratio(230_000, 1_000_000);
	/// <summary>DAT: TerminalVelocity; MeleeLight: terminalV. Agree: 2.8 — this is what fightcore.gg's
	/// per-character list mislabels as "Gravity" for Fox.</summary>
	public static readonly Fx FallSpeedCap = Fx.Ratio(2_800_000, 1_000_000);
	/// <summary>DAT: FastFallTerminalVelocity; MeleeLight: fastFallV. Agree: 3.4.</summary>
	public static readonly Fx FastFallSpeedCap = Fx.Ratio(3_400_000, 1_000_000);

	// ---- Jumping ------------------------------------------------------------
	/// <summary>DAT: JumpStartUpLag; MeleeLight: jumpSquat. Agree: 3 frames.</summary>
	public const int JumpSquatFrames = 3;
	/// <summary>DAT: InitialShortHopVerticalSpeed; MeleeLight: sHopInitV. Agree: 2.1.</summary>
	public static readonly Fx ShortHopVelocity = Fx.Ratio(2_100_000, 1_000_000);
	/// <summary>DAT: InitialJumpVerticalSpeed; MeleeLight: fHopInitV. Agree: 3.68 — full hop.</summary>
	public static readonly Fx FullHopVelocity = Fx.Ratio(3_680_000, 1_000_000);
	/// <summary>DAT: AirJumpMultiplier; MeleeLight: djMultiplier. Agree: 1.2, applied to FullHopVelocity. DERIVED (no direct "double jump speed" field in either source), but now corroborated by two independent sources using the same multiply-by-full-hop approach.</summary>
	public static readonly Fx DoubleJumpVelocity = Fx.Ratio(4_416_000, 1_000_000); // 3.68 * 1.2
	/// <summary>MeleeLight: djMomentum (0.9) — RESOLVES DAT's Unknown0x54. Double-jump horizontal momentum
    /// retention multiplier. Not consumed yet: PlayerMover.TickAirborne always steers straight at
	/// AirSpeedMax rather than blending in existing velocity, which MovementConstants.cs's own
	/// "What's NOT done" list already flagged as a known gap this would eventually feed into.</summary>
	public static readonly Fx DoubleJumpMomentumRetention = Fx.Ratio(900_000, 1_000_000);
	/// <summary>DAT: NumberOfJumps (2) minus the ground jump itself.</summary>
	public const int ExtraJumps = 1;
	/// <summary>DAT: InitialJumpHorizontalSpeed; MeleeLight: jumpHmaxV ("jump horizontal max velocity"). Agree: 1.7 — the CAP a ground jump's horizontal speed can reach with air control, not the takeoff-frame value (see JumpHorizontalInitialSpeed below).</summary>
    public static readonly Fx JumpHorizontalMaxSpeed = Fx.Ratio(1_700_000, 1_000_000);
	/// <summary>MeleeLight: jumpHinitV (0.72) — RESOLVES DAT's Unknown0x3C. The horizontal speed actually
	/// applied ON a ground jump's takeoff frame (distinct from the 1.7 cap above). Not yet consumed:
    /// PlayerMover.TickJumpSquat only ever sets Velocity.Y on the takeoff frame today.</summary>
    public static readonly Fx JumpHorizontalInitialSpeed = Fx.Ratio(720_000, 1_000_000);

    // ---- Landing lag (Phase 8/9 will want these keyed by move, not flat) ----
    /// <summary>DAT: NormalLandingLag (2.09), rounded to whole ticks for LandingFrames-shaped usage.</summary>
    public const int LandingFrames = 2;
    public static readonly Fx NairLandingLag = Fx.FromInt(16);
    public static readonly Fx FairLandingLag = Fx.FromInt(22);
    public static readonly Fx BairLandingLag = Fx.FromInt(20);
    public static readonly Fx DairLandingLag = Fx.FromInt(18);
    public static readonly Fx UairLandingLag = Fx.FromInt(18);

    // ---- Misc (not yet consumed by any Phase 4-7 system, kept for Phase 9+) ---
    /// <summary>DAT: ShieldSize; MeleeLight: shieldScale. Agree: 14.375.</summary>
    public static readonly Fx ShieldSize = Fx.Ratio(14_375_000, 1_000_000);
	/// <summary>DAT: ShieldBreakInitialVelocity (3.3). DISAGREES with MeleeLight's shieldBreakVel (2.5) — see file header.</summary>
	public static readonly Fx ShieldBreakInitialVelocity = Fx.Ratio(3_300_000, 1_000_000);
	/// <summary>MeleeLight: shieldBreakVel (2.5), kept alongside ShieldBreakInitialVelocity pending the discrepancy above.</summary>
	public static readonly Fx ShieldBreakVelocityMeleeLight = Fx.Ratio(2_500_000, 1_000_000);
	/// <summary>DAT: ModelScale; MeleeLight: modelScale. Agree: 0.96.</summary>
	public static readonly Fx ModelScale = Fx.Ratio(960_000, 1_000_000);
	/// <summary>DAT: LedgeJumpHorizontalVelocity (1.1). No MeleeLight equivalent found under this exact name — MeleeLight's wallJumpVelX/Y (below) are a DIFFERENT mechanic (wall jump, not ledge jump); don't conflate them.</summary>
	public static readonly Fx LedgeJumpHorizontalVelocity = Fx.Ratio(1_100_000, 1_000_000);
	/// <summary>DAT: LedgeJumpVerticalVelocity (4.0). See LedgeJumpHorizontalVelocity's note — not the same thing as wall jump.</summary>
    public static readonly Fx LedgeJumpVerticalVelocity = Fx.FromInt(4);
	/// <summary>MeleeLight: wallJumpVelX — Fox can wall-jump (walljump: true in the source). Wall-jumping doesn't exist in PlayerMover yet (COLLISION.md's known-limitations list doesn't mention it either) — later phase.</summary>
	public static readonly Fx WallJumpVelocityX = Fx.Ratio(1_400_000, 1_000_000);
	/// <summary>MeleeLight: wallJumpVelY.</summary>
	public static readonly Fx WallJumpVelocityY = Fx.Ratio(3_300_000, 1_000_000);
	/// <summary>MeleeLight: airdodgeIntangible (25 frames) — no airdodge system exists yet either.</summary>
	public const int AirdodgeIntangibleFrames = 25;
}
