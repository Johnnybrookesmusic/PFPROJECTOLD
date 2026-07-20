using Godot;

namespace PlatformFighter.Core.Input;

/// <summary>
/// Persists InputBinding to user://input_bindings.cfg, keyed by DeviceKind
/// plus (for joypads) the device's own GUID, so a specific rebound
/// controller keeps its bindings across reconnects/reboots while a new,
/// never-seen device just gets that DeviceKind's default.
/// </summary>
public static class InputBindingStore
{
    private const string Path = "user://input_bindings.cfg";

    public static InputBinding LoadOrDefault(DeviceKind kind, string deviceGuid)
    {
        var cfg = new ConfigFile();
        if (cfg.Load(Path) != Error.Ok) return InputBinding.Default(kind);

        string section = Section(kind, deviceGuid);
        if (!cfg.HasSection(section)) return InputBinding.Default(kind);

        var binding = new InputBinding { Kind = kind };
        foreach (InputAction action in System.Enum.GetValues<InputAction>())
        {
            string key = "action_" + action;
            if (cfg.HasSectionKey(section, key))
                binding.ButtonMap[action] = (int)cfg.GetValue(section, key);
        }
        foreach (StickAxis axis in System.Enum.GetValues<StickAxis>())
        {
            string key = "axis_" + axis;
            if (cfg.HasSectionKey(section, key))
                binding.AxisMap[axis] = (int)cfg.GetValue(section, key);
        }
        return binding;
    }

    public static void Save(InputBinding binding, string deviceGuid)
    {
        var cfg = new ConfigFile();
        cfg.Load(Path); // ok if this fails — first save creates it
        string section = Section(binding.Kind, deviceGuid);
        foreach (var (action, phys) in binding.ButtonMap)
            cfg.SetValue(section, "action_" + action, phys);
        foreach (var (axis, phys) in binding.AxisMap)
            cfg.SetValue(section, "axis_" + axis, phys);
        cfg.Save(Path);
    }

    private static string Section(DeviceKind kind, string deviceGuid) =>
        kind == DeviceKind.Keyboard ? "keyboard" : $"{kind}_{deviceGuid}";
}
