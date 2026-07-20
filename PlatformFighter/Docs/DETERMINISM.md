# Determinism Ground Rules

These are the laws every later phase must obey. Breaking any of them will
eventually manifest as a rollback-netcode desync or a replay that diverges
from what actually happened — often invisibly, and always at the worst time.

1. **No floats in sim code.** Only `Fx`, `FxVec2`, `int`, `long`, `bool`,
   `enum`s are allowed inside anything reachable from `ISimObject.Tick()`.
2. **No Godot API inside `Tick()`.** No nodes, no `Time.*`, no input
   polling — inputs arrive as latched snapshots (Phase 2).
3. **No `System.Random`, no `DateTime`, no `Environment.TickCount`.**
   Only `SimWorld.Rng` (a `DeterministicRandom`) may produce randomness
   inside the simulation.
4. **Stable iteration order everywhere.** `List<T>` ticked by index; never
   `foreach` over a `Dictionary`/`HashSet` in sim code.
5. **Sim state is plain data.** Every field must be copyable/serializable —
   this is what makes rollback save states nearly free later.
6. **The view never writes to the sim.** One-way data flow:
   `input -> sim -> render`.
7. **Input is sampled exactly once per tick, only in `SimDriver`.** A
   `FrameInput` is built by an `IInputProvider` immediately before each
   `SimWorld.Tick()` call and handed in as a parameter. `ISimObject.Tick()`
   implementations read it back via `SimWorld.GetInput(playerIndex)` — they
   never call `Godot.Input` themselves. See `Docs/INPUT.md`.

## Verifying Phase 1

Run the project and confirm:
- The sprite glides smoothly regardless of monitor refresh rate.
- "Ticks this render frame" averages 1 at ~60 FPS (and alternates 0/1 above it).
- The sim frame counter advances at exactly 60/s against a stopwatch.

That last check is your first determinism regression test — worth keeping
around permanently as later phases add state.

## Verifying Phase 2

Run the project and, with the game window focused, hold movement/attack
keys (see `Docs/INPUT.md` for bindings):
- "P1 input" on the debug label updates immediately and reflects exactly
  the keys currently held — nothing latches "stuck" after release.
- Behavior is identical regardless of render FPS, same as Phase 1's sprite
  check — input has no dependency on frame rate because it's sampled once
  per fixed 60Hz tick, not once per render frame.

## Verifying Phase 4

Press F9 again — the same output now also includes:
- "Collision twin run" at frames 0 / 60 / 500: identical hashes, same
  idea as Phase 2's twin-run but exercising `CollisionResolver` and, for
  the first time, an `ISimObject` that actually reads `world.GetInput()`.
- "Body grounded after falling": a dropped test body must land on and
  rest exactly on `TestStage`'s floor, not hover, sink, or tunnel through.

See `Docs/COLLISION.md` for the collision engine itself and
`Docs/ROADMAP.md` for what's next.

## What's next

See `Docs/ROADMAP.md` for the canonical phase list and status. As of
Phase 4, the next phase is Player movement physics (Phase 5): real
dash/air-control/fast-fall curves replacing `TestBody`'s placeholder
gravity and stick-scaled drift, built on top of the grounded/platform
detection this phase delivered.
