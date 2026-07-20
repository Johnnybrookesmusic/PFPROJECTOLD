namespace PlatformFighter.Core.Input;

/// <summary>Logical, remappable digital actions. Physical source is decided by InputBinding.</summary>
public enum InputAction
{
    Jump, JumpAlt, Attack, Special, Shield, Grab, Start,
    DPadUp, DPadDown, DPadLeft, DPadRight, LDigital, RDigital,
}

/// <summary>Logical analog axes. Physical joypad axis index decided by InputBinding.</summary>
public enum StickAxis
{
    MainX, MainY, CX, CY, LTrigger, RTrigger,
}
