using System.Collections.Generic;
using Godot;
using PlatformFighter.Core.Sim;

namespace PlatformFighter.Core.Input;

/// <summary>
/// Owns device connect/disconnect and device-to-player-slot assignment.
/// The ONLY place joy_connection_changed is handled. Assign this a
/// reference to the scene's SimDriver so it can push providers into
/// SimDriver.Providers[] as devices come and go.
///
/// NOTE: this file lives in namespace PlatformFighter.Core.Input, which
/// shares its last segment with Godot's own Input class — so every call
/// below is Godot.Input.___, fully qualified, never bare Input.___.
/// </summary>
public partial class ControllerManager : Node
{
	[Export] public NodePath SimDriverPath = null!;
private SimDriver _driver = null!;

	private const int EmptySlot = -2;

	// Device id -> provider, so reconnect/disconnect can find and drop it.
	private readonly Dictionary<int, DeviceInputProvider> _devices = new();
	// Which device id currently occupies each player slot. EmptySlot = none.
	private readonly int[] _slotForDevice = new int[SimWorld.MaxPlayers];

	public override void _Ready()
	{
		System.Array.Fill(_slotForDevice, EmptySlot);

		_driver = GetNode<SimDriver>(SimDriverPath);

		// Keyboard is always device -1, always available, defaults to slot 0.
		var kb = new DeviceInputProvider(-1, DeviceKind.Keyboard, InputBindingStore.LoadOrDefault(DeviceKind.Keyboard, ""));
		_devices[-1] = kb;
		AssignToFirstOpenSlot(-1);

		Godot.Input.JoyConnectionChanged += OnJoyConnectionChanged;
		for (int id = 0; id < 8; id++)
			if (Godot.Input.IsJoyKnown(id)) OnJoyConnectionChanged(id, true);
	}

	public override void _ExitTree() => Godot.Input.JoyConnectionChanged -= OnJoyConnectionChanged;

	private void OnJoyConnectionChanged(long deviceIdLong, bool connected)
	{
		int deviceId = (int)deviceIdLong;
		if (connected)
		{
			string name = Godot.Input.GetJoyName(deviceId);
			string guid = Godot.Input.GetJoyGuid(deviceId);
			var kind = DeviceKindDetector.Detect(name);
			var binding = InputBindingStore.LoadOrDefault(kind, guid);
			_devices[deviceId] = new DeviceInputProvider(deviceId, kind, binding);
			AssignToFirstOpenSlot(deviceId);
		}
		else
		{
			_devices.Remove(deviceId);
			for (int p = 0; p < SimWorld.MaxPlayers; p++)
			{
				if (_slotForDevice[p] == deviceId)
				{
					_slotForDevice[p] = EmptySlot;
					_driver.SetProvider(p, null);
				}
			}
		}
	}

	private void AssignToFirstOpenSlot(int deviceId)
	{
		for (int p = 0; p < SimWorld.MaxPlayers; p++)
		{
			if (_slotForDevice[p] == EmptySlot)
			{
				_slotForDevice[p] = deviceId;
				_driver.SetProvider(p, _devices[deviceId]);
				return;
			}
		}
		// All slots full — device stays connected but unassigned until a
		// menu (Phase 14) calls AssignDevice explicitly.
	}

	/// <summary>Explicit reassignment, for a future controller-select menu.</summary>
	public void AssignDevice(int playerIndex, int deviceId)
	{
		if (!_devices.TryGetValue(deviceId, out var provider)) return;
		_slotForDevice[playerIndex] = deviceId;
		_driver.SetProvider(playerIndex, provider);
	}

	public void RebindAndSave(int deviceId, InputAction action, int newPhysicalControl)
	{
		if (!_devices.TryGetValue(deviceId, out var provider)) return;
		provider.Binding.ButtonMap[action] = newPhysicalControl;
		string guid = deviceId < 0 ? "" : Godot.Input.GetJoyGuid(deviceId);
		InputBindingStore.Save(provider.Binding, guid);
	}
}
