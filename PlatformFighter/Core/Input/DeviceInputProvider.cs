using Godot;

namespace PlatformFighter.Core.Input;

/// <summary>
/// Turns one physical device (joypad id, or -1 for keyboard) plus an
/// InputBinding into a FrameInput every Sample() call. This is the ONLY
/// place raw Godot.Input / deadzone math is allowed to live — see
/// Core/Input/AnalogInputRules.md for the numbers enforced here.
///
/// NOTE: this file lives in namespace PlatformFighter.Core.Input, which
/// shares its last segment with Godot's own Input class — so every call
/// below is Godot.Input.___, fully qualified, never bare Input.___.
/// </summary>
public sealed class DeviceInputProvider : IInputProvider
{
    public const float StickDeadzone = 0.23f;   // radial, matches InputDecode.StickDeadzoneUnits
    public const float CStickDeadzone = 0.23f;
    public const float TriggerDeadzone = 0.05f;

    public readonly int JoyId;
    public readonly DeviceKind Kind;
    public InputBinding Binding;

    public DeviceInputProvider(int joyId, DeviceKind kind, InputBinding binding)
    {
        JoyId = joyId;
        Kind = kind;
        Binding = binding;
    }

    public FrameInput Sample() => JoyId < 0 ? SampleKeyboard() : SampleJoypad();

    private FrameInput SampleKeyboard()
    {
        ButtonFlags buttons = ButtonFlags.None;
        foreach (var (action, key) in Binding.ButtonMap)
            if (Godot.Input.IsPhysicalKeyPressed((Key)key))
                buttons |= ActionBit(action);

        // Digital stick: full deflection or nothing, no in-between — a
        // keyboard physically can't do partial tilt, so don't pretend it can.
        sbyte x = 0, y = 0;
        if (Godot.Input.IsPhysicalKeyPressed((Key)Binding.ButtonMap[InputAction.DPadLeft])) x = -100;
        else if (Godot.Input.IsPhysicalKeyPressed((Key)Binding.ButtonMap[InputAction.DPadRight])) x = 100;
        if (Godot.Input.IsPhysicalKeyPressed((Key)Binding.ButtonMap[InputAction.DPadDown])) y = -100;
        else if (Godot.Input.IsPhysicalKeyPressed((Key)Binding.ButtonMap[InputAction.DPadUp])) y = 100;

        // Shield is read from the analog trigger bytes, never a button bit
        // (see InputDecode.IsShieldHardPress) — a keyboard has no in-between,
        // so a held Shield key synthesizes a full hard-press trigger value,
        // same idea as the digital stick synthesis above.
        byte lAnalog = Godot.Input.IsPhysicalKeyPressed((Key)Binding.ButtonMap[InputAction.Shield]) ? (byte)255 : (byte)0;

        return new FrameInput(buttons, x, y, cx: 0, cy: 0, lAnalog: lAnalog);
    }

    private FrameInput SampleJoypad()
    {
        ButtonFlags buttons = ButtonFlags.None;
        foreach (var (action, btn) in Binding.ButtonMap)
            if (Godot.Input.IsJoyButtonPressed(JoyId, (JoyButton)btn))
                buttons |= ActionBit(action);

        (sbyte x, sbyte y) = ReadStick(StickAxis.MainX, StickAxis.MainY, StickDeadzone);
        (sbyte cx, sbyte cy) = ReadStick(StickAxis.CX, StickAxis.CY, CStickDeadzone);
        byte l = ReadTrigger(StickAxis.LTrigger);
        byte r = ReadTrigger(StickAxis.RTrigger);

        return new FrameInput(buttons, x, y, cx, cy, l, r);
    }

    private (sbyte, sbyte) ReadStick(StickAxis axX, StickAxis axY, float deadzone)
    {
        float rawX = Godot.Input.GetJoyAxis(JoyId, (JoyAxis)Binding.AxisMap[axX]);
        float rawY = -Godot.Input.GetJoyAxis(JoyId, (JoyAxis)Binding.AxisMap[axY]); // Godot Y+ is down; sim Y+ is up

        // Radial deadzone (not per-axis) — a real control stick's dead
        // center is a circle, not a square; per-axis deadzone lets diagonal
        // drift through that a radial check catches.
        float mag = new Vector2(rawX, rawY).Length();
        if (mag < deadzone) return (0, 0);

        float scale = System.Math.Min(1f, (mag - deadzone) / (1f - deadzone)) / mag;
        sbyte qx = (sbyte)System.Math.Clamp(rawX * scale * 100f, -100f, 100f);
        sbyte qy = (sbyte)System.Math.Clamp(rawY * scale * 100f, -100f, 100f);
        return (qx, qy);
    }

    private byte ReadTrigger(StickAxis ax)
    {
        // NOTE: some SDL backends report trigger axes as -1(released)..1(full)
        // rather than 0..1. If a given controller's shield feels backwards or
        // maxes out at half-press, remap here with (raw + 1f) * 0.5f for that
        // DeviceKind rather than globally — don't blanket-fix a per-driver quirk.
        float raw = Godot.Input.GetJoyAxis(JoyId, (JoyAxis)Binding.AxisMap[ax]);
        if (raw < TriggerDeadzone) return 0;
        return (byte)System.Math.Clamp(raw * 255f, 0f, 255f);
    }

    private static ButtonFlags ActionBit(InputAction a) => a switch
    {
        InputAction.Jump or InputAction.JumpAlt => ButtonFlags.X,
        InputAction.Attack   => ButtonFlags.A,
        InputAction.Special  => ButtonFlags.B,
        InputAction.Shield   => ButtonFlags.None, // shield reads from analog triggers, see InputDecode
        InputAction.Grab     => ButtonFlags.Z,
        InputAction.Start    => ButtonFlags.Start,
        InputAction.DPadUp    => ButtonFlags.DPadUp,
        InputAction.DPadDown  => ButtonFlags.DPadDown,
        InputAction.DPadLeft  => ButtonFlags.DPadLeft,
        InputAction.DPadRight => ButtonFlags.DPadRight,
        InputAction.LDigital  => ButtonFlags.LDigital,
        InputAction.RDigital  => ButtonFlags.RDigital,
        _ => ButtonFlags.None,
    };
}
