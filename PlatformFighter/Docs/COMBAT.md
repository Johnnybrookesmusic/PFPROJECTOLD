# Combat (Phase 7)

## What exists

`Core/Combat/Hitbox.cs` and `Core/Combat/KnockbackMath.cs` are the
generic engine pieces: a hitbox (damage + knockback tuning + a
pre-normalized launch direction) and the magnitude/hitstun-duration math.
`Gameplay/PlayerMover.ApplyHit(in Hitbox, bool attackerFacingRight)` is
the receiving half — it adds damage, launches the mover via knockback,
and starts a fixed input-less airborne window (`TickHitstun`, top
priority in `Tick()`, above jump-squat).

**No hitbox spawning or hit-detection loop exists yet.** `ApplyHit` is
called directly by tests (see `Debug/DeterminismTest.cs` 7.1); nothing
in the live scene generates a `Hitbox` or scans for overlaps between one
and a hurtbox. That's Phase 9/10 (Character framework / first fighter) —
this phase is the math and the receiving state machine, not moves.

## Knockback math: now the real formula, verified against a real engine

`KnockbackMath` was a linear placeholder through most of Phase 7; it now
implements the real Melee knockback formula — and as of this pass, that
formula has been checked bit-for-bit against `getKnockback` in MeleeLight
(`src/physics/hitDetection.js`), a real working open-source Melee-style
engine, not just re-derived from memory:

```
Kb = ((((p/10) + (p*d)/20) * (200/(w+100)) * 1.4) + 18) * (kbg/100) + kbb
```

(see the doc comment on `KnockbackMath` for the full breakdown). The
hitstun formula (`floor(knockback * 0.4)`) also matches MeleeLight's
`getHitstun` exactly.

**Bug fix found via that cross-check:** `p` above must be the defender's
percent BEFORE this hit's damage is added, not after. MeleeLight calls
`getKnockback` with the pre-hit percent and only adds this hit's damage to
the running total afterward. The previous pass of this codebase had it
backwards (`ApplyHit` incremented `Percent` before computing magnitude).
Both `KnockbackMath.ComputeMagnitude`'s parameter name and `ApplyHit`'s
ordering are corrected now.

Also newly ported from MeleeLight: the crouch-cancel (`×0.67`) and
vectoring/V-cancel (`×0.95`) multipliers, and the 2500 knockback cap.
`ComputeMagnitude` takes optional `crouching`/`vectorCancel` bools for
these — nothing sets them to `true` yet, since crouch and V-cancel aren't
implemented as mechanics in `PlayerMover`.

`ApplyHit` still has its `Fx? defenderWeight` overload — `null` falls back
to `KnockbackMath`'s default (Fox's weight, the only fighter with real
data). Still a stopgap, not Phase 9: no `Hitbox` is generated anywhere
live yet, and `PlayerMover` has no `Weight` field of its own.

Not yet ported: staling/freshness, electric-effect quirks, and
`SetKnockback` (a few real Melee moves ignore the formula entirely and use
a fixed value — `Hitbox` has no field for it, and nothing in the extracted
Fox/Falco data needs it yet).

## Hitlag: new, not wired in

`Core/Combat/HitlagMath.cs` is new — nothing computed hitlag before.
Ported from MeleeLight (applied identically on every regular-hit path
there): `hitlag = floor(damage / 3 + 3)`. **Not called from anywhere
yet** — wiring it in means freezing both the attacker's and defender's
`Tick()` for the computed duration before `ApplyHit`'s launch actually
takes effect, which is a real change to `Tick()`'s dispatch order best
left to whoever adds actual hit-detection (Phase 9/10) rather than
bolted on speculatively here.

## Knockback decay: ported, adapted to this engine's direction vectors

MeleeLight decays knockback velocity by a fixed magnitude (0.051/tick)
split into X/Y components via the launch angle's cos/sin. This engine
has no fixed-point trig (see below), so `TickHitstun` applies the same
0.051 magnitude as a straight per-axis decay toward zero on `Velocity.X`
and `Velocity.Y` instead of decomposing an angle — see
`KnockbackMath.KnockbackDecayPerTick`'s doc comment for the reasoning.
This is an adaptation, not a literal port; revisit if a real angle system
ever gets built.

## Why direction vectors instead of angles

Melee-style hitboxes are usually authored as an angle in degrees. `Fx`
(`Core/Math/Fx.cs`) has no sin/cos — sim math is fixed-point only, and a
correctly-rounded fixed-point trig table is its own separate piece of
work, not something to bolt on as a side effect of Phase 7. So `Hitbox`
stores a pre-normalized `(DirX, DirY)` launch direction directly instead
of an angle. `DirX` is mirrored by the attacker's facing at apply-time;
`DirY` is not (negative is always up, per `FxVec2`'s +Y-down convention).
If/when a real angle system is needed, it's an additive change — add an
angle-to-direction table, keep `Hitbox` and `ApplyHit` as they are.

**MeleeLight's real DI algorithm** (`getLaunchAngle` in
`src/physics/hitDetection.js`) is now on hand as a reference for whenever
that trig table happens: stick deadzone at ±0.2875, the angle-361
"Sakurai angle" special case (resolves to 44°/136° above 32.1 knockback,
0°/180° below), and a DI offset capped at ±18° computed from
`sin(trajectory - stickAngle) * stickMagnitude`, squared and scaled. Worth
transcribing into a spec doc if/when Phase 9/10 tackles trig, rather than
re-deriving it from scratch then.

## What's explicitly NOT here yet

- Hitbox spawning, active-frame windows, hit-detection (hitbox vs.
  hurtbox overlap scanning) — Phase 9/10.
- Hitlag is now computed (see above) but not applied — both characters
  don't actually freeze on connect yet.
- Shield, shield-stun, powershield, multi-hit moves — later phases.
  (MeleeLight's shield-stun formula is on hand for later:
  `((floor(damage) * (0.65 * (1 - (shieldAnalog - 0.3) / 0.7) + 0.3)) * 1.5) + 2`
  — depends on shield-button analog amount, which doesn't exist as a
  concept here yet either.)
- Real DI — the algorithm is documented above for later; applying it
  needs Fx trig, which doesn't exist. Knockback decay (unlike DI) has
  been ported as an adaptation — see above.
- Landing during hitstun doesn't do anything special (no tech, no hard
  landing) — it just keeps ticking `TickHitstun` while grounded, same as
  any other airborne-physics-while-touching-ground case elsewhere in
  `PlayerMover`.
- Weight, character-specific knockback resistance is now IN the formula
  (see above), but still only reachable via `KnockbackMath`'s default or
  an explicit `ApplyHit` argument — no `ISimObject` carries its own Weight
  field yet. Real per-character data plumbing is still Phase 9.

## Real character data on hand (not yet wired into any of the above)

- `Characters/Fox/FoxAttributes.cs` — Fox's real attributes, parsed from
  `PlFx.dat` (`fox_physics.json`). Movement-shaped fields only right now;
  no move/hitbox data (Fox's frame data hasn't been transcribed the way
  Falco's was).
- `Characters/Falco/FalcoMoveData.json` — Falco's full move list
  (grounded/tilt/smash/aerial/special), damage/angle/BKB/KBG/frame data,
  hand-transcribed from fightcore.gg screenshots rather than parsed from
  `PlFc.dat`. See the file's own `_readme` for transcription caveats —
  several fields (Falco Phantasm (Air) damage, Up Air's 3-way knockback
  split, the two different "run speed" fields in Fox's data) need
  reconciling against the actual DAT parse before Phase 9/10 treats them
  as ground truth.

Neither file is consumed by any Phase 4-7 system yet. They exist so
Phase 9 (Character framework) has real numbers to build against instead
of starting from nothing.
