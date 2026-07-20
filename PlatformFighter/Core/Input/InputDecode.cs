namespace PlatformFighter.Core.Input;

public enum AttackStrength { None, Tilt, Smash }

public static class InputDecode
{
    public const int StickDeadzoneUnits = 23;
    public const int CStickDeadzoneUnits = 23;
    public const int DashThresholdUnits = 79;
    public const int SmashThresholdUnits = 64;
    public const int SmashWindowFrames = 2;
    public const byte TriggerDeadzoneUnits = 13;
    public const byte TriggerHardPressMin = 225;

    public static bool IsDashInitiate(sbyte prevX, sbyte curX, out int direction)
    {
        direction = System.Math.Sign((int)curX);
        if (direction == 0 || System.Math.Abs((int)curX) < DashThresholdUnits)
            return false;
        bool wasNeutralOrOpposite =
            System.Math.Abs((int)prevX) < StickDeadzoneUnits ||
            System.Math.Sign((int)prevX) != direction;
        return wasNeutralOrOpposite;
    }

    public static bool IsShieldHardPress(FrameInput input) =>
        input.LAnalog >= TriggerHardPressMin ||
        input.RAnalog >= TriggerHardPressMin ||
        (input.Buttons & (ButtonFlags.LDigital | ButtonFlags.RDigital)) != 0;

    /// <summary>C-stick has no timing window — always a smash-equivalent once past deadzone.</summary>
    public static bool CStickActive(FrameInput input) =>
        System.Math.Abs((int)input.CX) >= CStickDeadzoneUnits ||
        System.Math.Abs((int)input.CY) >= CStickDeadzoneUnits;
}

/// <summary>
/// Per-player, per-axis Tilt-vs-Smash timing state. See
/// Core/Input/AnalogInputRules.md for the numbers. Needs to live somewhere
/// that ticks once per sim frame per player — NOT inside FrameInput
/// itself, since it's derived history, not raw input, and must not be
/// part of the snapshot format.
/// </summary>
public sealed class SmashTiltClassifier
{
    private int _framesSinceLeftDeadzoneX = -1;
    private int _framesSinceLeftDeadzoneY = -1;

    public AttackStrength ClassifyHorizontal(sbyte prevX, sbyte curX) =>
        Classify(prevX, curX, ref _framesSinceLeftDeadzoneX);

    public AttackStrength ClassifyVertical(sbyte prevY, sbyte curY) =>
        Classify(prevY, curY, ref _framesSinceLeftDeadzoneY);

    /// <summary>Phase 9: PlayerMover now owns one of these per-instance (it needs it
    /// for grounded-attack move selection — see PlayerMover.TryStartAttack), and
    /// PlayerMover is an ISimObject, so this internal timing state has to be part
    /// of SaveState/LoadState like everything else it reads/writes (ISimObject's
    /// contract, point 4). Raw ints rather than exposing the fields directly —
    /// keeps this class's internals free to change shape later without touching
    /// PlayerMover's (de)serialization call sites.</summary>
    public (int x, int y) SaveRaw() => (_framesSinceLeftDeadzoneX, _framesSinceLeftDeadzoneY);

    public void LoadRaw(int x, int y)
    {
        _framesSinceLeftDeadzoneX = x;
        _framesSinceLeftDeadzoneY = y;
    }

    private static AttackStrength Classify(sbyte prev, sbyte cur, ref int framesSinceLeft)
    {
        bool prevInDeadzone = System.Math.Abs((int)prev) < InputDecode.StickDeadzoneUnits;
        bool curPastSmash = System.Math.Abs((int)cur) >= InputDecode.SmashThresholdUnits;

        if (prevInDeadzone && !curPastSmash)
        {
            // Just left the deadzone but not yet at smash range — start the clock.
            if (System.Math.Abs((int)cur) >= InputDecode.StickDeadzoneUnits)
                framesSinceLeft = 0;
            return AttackStrength.None;
        }

        if (!curPastSmash)
        {
            if (framesSinceLeft >= 0) framesSinceLeft++;
            return AttackStrength.None;
        }

        // Reached smash range this frame.
        AttackStrength result = (framesSinceLeft >= 0 && framesSinceLeft <= InputDecode.SmashWindowFrames)
            ? AttackStrength.Smash
            : AttackStrength.Tilt;
        framesSinceLeft = -1; // consumed — next attack starts a fresh window
        return result;
    }
}
