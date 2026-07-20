using PlatformFighter.Core.Math;

namespace PlatformFighter.Gameplay;

/// <summary>
/// Phase 5 movement tuning. Every value here is a PLACEHOLDER — generic,
/// Melee-shaped numbers picked to make the state machine exercisable and
/// deterministic, NOT final feel. Per-character stats arrive in Phase 9
/// (Character framework), at which point PlayerMover should take these as
/// constructor/data parameters instead of a single shared struct.
///
/// UNITS: everything is "per tick" (1/60s), matching Debug/TestBody.cs's
/// existing convention — NOT "per second" like Debug/TestMover.cs. Do not
/// mix the two within one object.
/// </summary>
public static class MovementConstants
{
    // ---- Ground movement ----------------------------------------------
    public static readonly Fx WalkSpeed        = Fx.FromInt(3);
    public static readonly Fx DashSpeed        = Fx.FromInt(6);
    public static readonly Fx RunSpeed         = Fx.FromInt(5);
    public static readonly Fx GroundAccel      = Fx.Ratio(1, 2);
    public static readonly Fx GroundTraction   = Fx.Ratio(3, 4); // deceleration when stick is neutral/reversed

    /// <summary>Ticks after a dash initiates before it's locked into Run if the stick is still held past the dash threshold.</summary>
    public const int DashInitiateFrames = 12;

    // ---- Air movement ---------------------------------------------------
    public static readonly Fx AirSpeedMax  = Fx.FromInt(4);
    public static readonly Fx AirAccel     = Fx.Ratio(1, 4);

    // ---- Gravity / falling ----------------------------------------------
    public static readonly Fx Gravity          = Fx.Ratio(1, 2);
    public static readonly Fx FallSpeedCap     = Fx.FromInt(7);
    public static readonly Fx FastFallSpeedCap = Fx.FromInt(12);

    // ---- Jumping ----------------------------------------------------------
    public const int JumpSquatFrames = 3;
    public static readonly Fx ShortHopVelocity  = Fx.FromInt(8);  // magnitude; applied as -Y (up)
    public static readonly Fx FullHopVelocity   = Fx.FromInt(14);
    public static readonly Fx DoubleJumpVelocity = Fx.FromInt(12);
    public const int ExtraJumps = 1; // 1 = Melee-standard single double-jump

    /// <summary>Ticks the PlayerActionState.Landing window holds after touching down —
    /// purely observational right now (no action lockout exists yet; that needs
    /// Phase 7's move system), but Phase 8 (Animation) needs a named window to play
    /// a landing clip in, so it's tracked from here rather than invented twice.</summary>
    public const int LandingFrames = 4;

    public static readonly FxVec2 HalfSize = new(Fx.FromInt(20), Fx.FromInt(30));
}
