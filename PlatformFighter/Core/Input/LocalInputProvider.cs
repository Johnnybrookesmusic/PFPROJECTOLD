using Godot;
namespace PlatformFighter.Core.Input;

/// <summary>
/// Translates the local keyboard into a FrameInput. This is the ONLY
/// class in the project allowed to call Godot.Input — it exists
/// specifically to convert Godot's continuous, floating-point input state
/// into the small deterministic integers the simulation is allowed to see.
///
/// Reads physical keys directly rather than InputMap actions for now, so
/// this works out of the box with no project.godot Input Map configuration.
/// Swapping to named actions (Project Settings -> Input Map) later is a
/// drop-in change confined to this one file.
///
/// Default bindings:
///   Move:    WASD or Arrow Keys
///   Attack:  J        Special: K        Jump: L or Space        Shield: Shift
/// </summary>
public sealed class LocalInputProvider : IInputProvider
{
	public FrameInput Sample()
	{
		ButtonFlags buttons = ButtonFlags.None;
		bool left  = Godot.Input.IsPhysicalKeyPressed(Key.A) || Godot.Input.IsPhysicalKeyPressed(Key.Left);
		bool right = Godot.Input.IsPhysicalKeyPressed(Key.D) || Godot.Input.IsPhysicalKeyPressed(Key.Right);
		bool up    = Godot.Input.IsPhysicalKeyPressed(Key.W) || Godot.Input.IsPhysicalKeyPressed(Key.Up);
		bool down  = Godot.Input.IsPhysicalKeyPressed(Key.S) || Godot.Input.IsPhysicalKeyPressed(Key.Down);

		if (Godot.Input.IsPhysicalKeyPressed(Key.J)) buttons |= ButtonFlags.Attack;   // = A
		if (Godot.Input.IsPhysicalKeyPressed(Key.K)) buttons |= ButtonFlags.B;        // Special
		if (Godot.Input.IsPhysicalKeyPressed(Key.L) || Godot.Input.IsPhysicalKeyPressed(Key.Space))
			buttons |= ButtonFlags.Jump; // = X

		// Shield is no longer a button bit — it's read from the analog
		// trigger bytes (see InputDecode.IsShieldHardPress). A keyboard has
		// no in-between, so Shift synthesizes a full trigger press, same
		// as the digital stick below synthesizes full stick deflection.
		byte lAnalog = Godot.Input.IsPhysicalKeyPressed(Key.Shift) ? (byte)255 : (byte)0;

		// No analog device wired up yet, so derive a fully-deflected digital
		// stick from the same keys. Swapping in Input.GetVector() for a real
		// analog stick later only touches this method.
		// Y+ is UP in sim space (see DeviceInputProvider), so up = +100.
		sbyte stickX = (sbyte)((right ? 100 : 0) - (left ? 100 : 0));
		sbyte stickY = (sbyte)((up ? 100 : 0) - (down ? 100 : 0));

		return new FrameInput(buttons, stickX, stickY, cx: 0, cy: 0, lAnalog: lAnalog, rAnalog: 0);
	}
}
