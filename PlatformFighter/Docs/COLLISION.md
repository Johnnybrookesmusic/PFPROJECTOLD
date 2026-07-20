# Collision Engine (Phase 4)

## The shapes

Everything is an `FxAabb` — a fixed-point center + half-size
(`Core/Sim/Collision/FxAabb.cs`). Position convention matches `FxVec2`:
+X right, +Y DOWN, so `Top` is the smaller-Y edge and `Bottom` is the
larger-Y edge (where a grounded body's feet rest).

## Stage geometry

`StageGeometry` (`Core/Sim/Collision/StageGeometry.cs`) holds two kinds
of static collision:

- **Solids** — full `FxAabb`s that block from every direction (floor,
  walls, ceiling).
- **One-way platforms** — an X range at a single Y height, solid ONLY
  when a body falls onto the top surface from above. Walking off the
  edge, jumping up through from below, and holding the drop-through
  gesture all correctly pass through.

A `StageGeometry` instance is immutable and shared by reference — stage
layouts don't change at runtime yet (Phase 12 will introduce real,
per-stage, possibly moving/destructible geometry). `Debug/TestStage.cs`
is today's only instance: one wide floor plus one Battlefield-style
platform, used purely to exercise the resolver and the acceptance tests.

## Resolution

`CollisionResolver.Resolve()` (`Core/Sim/Collision/CollisionResolver.cs`)
is a pure function: previous position, moved position, velocity,
half-size, stage, and a pass-through flag in; resolved position,
velocity, and a `Grounded` bool out. It's axis-separated — X is resolved
first (against the old Y), then Y is resolved against the X-resolved
position — which is standard for fixed-timestep platformer physics and
exact in fixed-point (no epsilon fudging anywhere; every check is an
integer compare under the hood).

**Known limitation:** this is discrete, not swept/continuous. A body
moving further than its own half-size in a single tick can tunnel
through a thin solid. Fine at gravity/movement speeds; revisit if a
later phase introduces very high-speed knockback through solids.

## The Phase 2 → Phase 4 gap this phase also closed

`ISimObject.Tick()` used to take no parameters, even though
`Docs/INPUT.md` always said objects read input via
`world.GetInput(playerIndex)` — there was no way for an object to
actually reach a `SimWorld` to call that. `Tick()` now takes the owning
`SimWorld` as a parameter (handed in fresh each call, not stored — a
rebuilt object from `RestoreSnapshot` must behave identically to a live
one). `Debug/TestBody.cs` is the first `ISimObject` that reads input for
real: player 0's stick X drives horizontal movement, and holding down
triggers platform drop-through.

## Verifying

See `Docs/DETERMINISM.md`'s "Verifying Phase 4" section — F9 now also
runs a collision twin-run test and a grounded-rest test.
