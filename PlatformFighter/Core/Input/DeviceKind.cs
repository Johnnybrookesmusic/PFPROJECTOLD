namespace PlatformFighter.Core.Input;

public enum DeviceKind
{
    Keyboard,
    GameCubeAdapter, // standard "Wired Fight Pad" mode Mayflash/official adapter
    Xbox,
    PlayStation,
    GenericSdl,
}

public static class DeviceKindDetector
{
    public static DeviceKind Detect(string joyName)
    {
        string n = joyName.ToLowerInvariant();
        if (n.Contains("mayflash") || n.Contains("gamecube") || n.Contains("gc adapter") || n.Contains("wup-028"))
            return DeviceKind.GameCubeAdapter;
        if (n.Contains("xbox") || n.Contains("xinput"))
            return DeviceKind.Xbox;
        if (n.Contains("playstation") || n.Contains("dualshock") || n.Contains("dualsense") || n.Contains("ps3") || n.Contains("ps4") || n.Contains("ps5"))
            return DeviceKind.PlayStation;
        return DeviceKind.GenericSdl;
    }
}
