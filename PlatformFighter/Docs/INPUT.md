# Input System (Phase 2)

## The contract

`FrameInput` (`Core/Input/FrameInput.cs`) is the only way gameplay code
ever learns what a player is doing. It is a plain value type — a
`ButtonFlags` bitmask plus a quantized `(StickX, StickY)` in `[-100, 100]`
— with no reference to Godot, so it's safe to store in sim state, put in
a replay file, or (Phase 6) send over the network unchanged.

**Exactly one thing samples real input: `SimDriver.RunOneTick()`.** Right
before each `SimWorld.Tick()` call, it asks every player's `IInputProvider`
for a `FrameInput` and passes the whole set in as a parameter. `SimWorld`
latches it and makes it available for that tick via
`World.GetInput(playerIndex)`. Nothing inside `ISimObject.Tick()` — no
fighter, no projectile, no future hitbox — is allowed to call
`Godot.Input` directly. That single choke point is what makes replays and
rollback resimulation possible later: resimulating a past tick just means
calling `Tick()` again with the same recorded `FrameInput`, and the result
is bit-identical every time.

## Providers

`IInputProvider.Sample()` is the seam. `ControllerManager` owns
connect/disconnect and assigns every device — keyboard (always device -1,
slot 0 by default) and any joypad — a `DeviceInputProvider`, which turns
that device's `InputBinding` into a `FrameInput` each tick. The interface
is what lets later phases add a `NetworkInputProvider` (Phase 6) or a
`ReplayInputProvider` (plays a recorded match back frame-by-frame) without
SimDriver's tick loop changing at all.

`LocalInputProvider.cs` predates `ControllerManager`/`DeviceInputProvider`
and is no longer wired up anywhere; it's kept only as legacy reference.

## Default local bindings

| Action     | Keys              |
|------------|-------------------|
| Move       | WASD or Arrow Keys|
| Attack     | J                 |
| Special    | K                 |
| Jump       | W/Space           |
| Shield     | L                 |
| Grab       | ;                 |
| Start      | Enter             |

These live in `InputBinding.Default(DeviceKind.Keyboard)` rather than
Godot's Input Map, so a fresh clone of this project works with zero editor
configuration. Rebinding at runtime goes through
`ControllerManager.RebindAndSave`, persisted per-device via
`InputBindingStore`.

## Ring buffer

Every player has an `InputRingBuffer` (in `SimDriver.InputHistory`) that
remembers the last 128 ticks (~2.1s) of `FrameInput`, keyed by frame
number. Nothing consumes it yet — it exists now because Phase 6 rollback
netcode needs exactly this shape (predicted input for a remote player,
later overwritten with the confirmed value once it arrives) and it's much
cheaper to have the pipe already in place than to retrofit it later.
