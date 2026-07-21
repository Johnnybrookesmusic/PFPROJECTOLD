# Step 2 — Core physics: segment collision + the unit-system fix

Melee Lite Translation Directive, Step 2. Step 1 landed Battlefield's real
geometry and could not resolve against any of it. This step closes that.

## The headline finding: body size was in the wrong units

Step 1 transcribed Battlefield in MeleeLight's native units (ground spans
±68.4). Velocities, gravity and traction were **already** in those same units
and internally consistent — Fox dashes the full stage in ~68 ticks, which is
correct. **Body size was the sole outlier**, still a pixel-scale placeholder:

| | shipped | real (MeleeLight `fox/ecb.js` WAIT `[4,3,9,13]`) |
|---|---|---|
| width | 40 (29.2% of stage) | 6 (4.4% of stage) |
| height | 60 | 13 |

Everything distance-shaped was wrong by ~6.7×: spacing, hit reach, platform
coverage, ledge reachability.

`CharacterPhysics.FromFox()`'s own doc comment diagnosed this correctly last
pass, then reverted the fix because swapping `HalfSize` alone broke
`DeterminismTest`'s combat check (P1/P2 spawn a fixed 30 apart; a hit only
lands at the inflated size). That reasoning was right — a half-measure is worse
— but the conclusion should have been *"fix the spawn distance too"*, not
*"keep the wrong body"*. There is no whole-engine rescale to do; only the spawn
constants move.

**Render scale:** MeleeLight draws Battlefield with `scale: 4.5, offset:
[600,480]` (`battlefield.js`, real fields, consumed only by `stagerender.js`).
That is the authoritative sim→screen transform. The render layer multiplies by
it; the sim does not inflate itself to suit a camera.

## What Step 2 delivers

| file | what |
|---|---|
| `Core/Sim/Collision/Ecb.cs` | The real 4-point ECB diamond. **Origin is the feet, not the centre** — that is what the MeleeLight algorithm assumes everywhere, and what makes slope-following expressible. Y-down, same negation Step 1 applied to the stage. |
| `Core/Sim/Collision/SegmentMath.cs` | Fixed-point segment queries (`SpansX`, `TryYAt`, `Side`). MeleeLight's `coordinateIntercept`/`extremePoint` family, with angle comparisons re-expressed as sign tests since `Fx` has no trig. |
| `Core/Sim/Collision/SegmentCollisionResolver.cs` | The resolver. Ports the *model* from `physics.js`'s `dealWithGround`/`fallOffGround`/`dealWithCeilingCollision`. |
| `Characters/Fox/FoxEcb.cs` | Fox's real ECB, transcribed. |

### The structural change that matters

A grounded fighter is **on a named surface** and stays on it, following its Y
as X changes, until its X leaves that surface's span — then it falls off. It
does not re-search the stage every tick. This is why ledges can exist at all,
and it is the single biggest difference from the old `CollisionResolver`.

`SegmentCollisionResult.SurfaceIndex`/`Surface` **must be round-tripped by the
caller** into the next tick's resolve. Losing it turns "walk along this ledge"
into "re-search and maybe pick a different surface", i.e. fighters teleporting
between platforms.

## Verification actually performed

No .NET SDK and no network in the authoring environment, so **this was not
compiled**. Instead the algorithm was re-implemented in Python against the real
Battlefield data and run — which caught **two real bugs before they shipped**:

1. **Jumping did nothing.** A grounded fighter that jumps was re-snapped to the
   floor on the same tick, because surface-follow ran regardless of upward
   velocity. Fixed with a `Velocity.Y >= 0` guard.
2. **Platform drop-through was eaten.** A fighter standing on a platform was
   re-snapped by surface-follow even when passing through. Fixed by releasing
   the surface when `passThroughPlatforms` and the surface is a platform.

Passing checks: full hop leaves the ground and resettles (apex 31.3 units —
matches `v²/2g` = 29.4 for `v=3.68, g=0.23` plus discrete-integration
overshoot); walking off the right ledge falls rather than hovering, with the
edge event firing; falling onto a side platform lands on the *platform* not
through it; drop-through reaches the ground in 14 ticks; identical inputs give
identical results.

Brace/paren balance checked on every file. **That is the extent of it — an
actual build and an in-engine F9 run are still required before trusting this.**

## Deliberately NOT ported in Step 2

- **The full ECB-corner sweep.** `environmentalCollision.js` is 1343 lines that
  sweep all four ECB points with corner cases, edge connectivity, ECB squashing
  and interpolated sub-steps. Here, **ground uses the true bottom point** (the
  part that must be exact, and is); **ceilings and walls use the ECB's bounding
  box**. Wrong in one visible way: against the diagonal underside chamfers a
  fighter stops slightly early rather than sliding along the slope. Correct for
  "you cannot pass through the stage", imprecise for recovery tech that hugs
  the underside.
- **Per-frame ECB animation.** MeleeLight has a distinct ECB per action state
  per frame; there is no animation system yet (Phase 8) so there is no frame
  cursor to index with. One static ECB per character (Fox's WAIT).
- **`connected` edge adjacency.** Battlefield defines none (single ground
  segment), so there is nothing to test against. `WalkedOffEdge` is the hook.
- **Teetering, ledge grabs, wall jump/cling, ECB squash** — Step 6.
- **Swept/continuous motion.** Still discrete. This matters *more* now: the
  stage is 136.8 wide and a knockback launch can exceed that in a few ticks.
  Flagged for Step 4.

`CollisionResolver.cs` is left in place and untouched — `Debug/TestBody.cs` and
the Phase 4 tests are built on it and still pass. Migrate callers only once
Step 2's own tests pass in-engine.

## Integration still required (not done in this pass)

`PlayerMover.cs` (757 lines) still uses centre-origin `HalfSize` and the old
resolver. Rewiring it means changing the meaning of `Position` from centre to
feet, which touches rendering, combat reach, and every spawn constant in
`Main.cs` and `DeterminismTest.cs` at once. Doing that blind — without a
compiler, on top of a resolver that has itself never been built — is how the
last few sessions produced files that didn't compile. It should be one focused
pass with a build available.
