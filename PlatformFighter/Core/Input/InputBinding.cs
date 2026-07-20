using System.Collections.Generic;
using Godot;

namespace PlatformFighter.Core.Input;

/// <summary>
/// One physical control scheme: which joypad button index (or Key, for
/// keyboard) fires each InputAction, and which joypad axis index feeds
/// each StickAxis. Polarity/deadzone/scale is DeviceInputProvider's job —
/// a binding only answers "which control", never "how to read it".
/// </summary>
public sealed class InputBinding
{
    public DeviceKind Kind;
    public readonly Dictionary<InputAction, int> ButtonMap = new();
    public readonly Dictionary<StickAxis, int> AxisMap = new();

    public static InputBinding Default(DeviceKind kind)
    {
        var b = new InputBinding { Kind = kind };

        if (kind == DeviceKind.Keyboard)
        {
            b.ButtonMap[InputAction.Jump]      = (int)Key.Space;
            b.ButtonMap[InputAction.JumpAlt]   = (int)Key.W;
            b.ButtonMap[InputAction.Attack]    = (int)Key.J;
            b.ButtonMap[InputAction.Special]   = (int)Key.K;
            b.ButtonMap[InputAction.Shield]    = (int)Key.L;
            b.ButtonMap[InputAction.Grab]      = (int)Key.Semicolon;
            b.ButtonMap[InputAction.Start]     = (int)Key.Enter;
            b.ButtonMap[InputAction.DPadLeft]  = (int)Key.A;
            b.ButtonMap[InputAction.DPadRight] = (int)Key.D;
            b.ButtonMap[InputAction.DPadUp]    = (int)Key.W;
            b.ButtonMap[InputAction.DPadDown]  = (int)Key.S;
            // No sticks on keyboard: DeviceInputProvider synthesizes MainX/Y
            // from A/D/W/S as a full-deflection digital stick. No c-stick.
            return b;
        }

        // Xbox / PlayStation / GameCube-adapter-in-standard-mode share the
        // same SDL button/axis layout in Godot; only the face-button
        // semantics differ slightly, which doesn't matter since we bind by
        // physical button INDEX, not label.
        b.ButtonMap[InputAction.Jump]      = (int)JoyButton.A;
        b.ButtonMap[InputAction.JumpAlt]   = (int)JoyButton.Y;
        b.ButtonMap[InputAction.Attack]    = (int)JoyButton.B;
        b.ButtonMap[InputAction.Special]   = (int)JoyButton.X;
        b.ButtonMap[InputAction.Grab]      = (int)JoyButton.LeftShoulder;
        b.ButtonMap[InputAction.Start]     = (int)JoyButton.Start;
        b.ButtonMap[InputAction.LDigital]  = (int)JoyButton.LeftShoulder;
        b.ButtonMap[InputAction.RDigital]  = (int)JoyButton.RightShoulder;
        b.ButtonMap[InputAction.DPadUp]    = (int)JoyButton.DpadUp;
        b.ButtonMap[InputAction.DPadDown]  = (int)JoyButton.DpadDown;
        b.ButtonMap[InputAction.DPadLeft]  = (int)JoyButton.DpadLeft;
        b.ButtonMap[InputAction.DPadRight] = (int)JoyButton.DpadRight;

        b.AxisMap[StickAxis.MainX]    = (int)JoyAxis.LeftX;
        b.AxisMap[StickAxis.MainY]    = (int)JoyAxis.LeftY;
        b.AxisMap[StickAxis.CX]       = (int)JoyAxis.RightX;
        b.AxisMap[StickAxis.CY]       = (int)JoyAxis.RightY;
        b.AxisMap[StickAxis.LTrigger] = (int)JoyAxis.TriggerLeft;
        b.AxisMap[StickAxis.RTrigger] = (int)JoyAxis.TriggerRight;
        return b;
    }
}
