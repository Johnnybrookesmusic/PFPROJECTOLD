# Roadmap (canonical numbering)

**⚠ Read `Docs/CURRENT_GOAL.md` first.** That file states the active
directive (Master Directive v2 — scope narrowed to a pure Fox-vs-Fox match
on Battlefield, hybrid Fox/Falco work paused) and exactly what to do next.
This file is the historical log of how the project got here; it is not
itself the current marching orders.

This is the authoritative phase list and status. It supersedes any phase
number mentioned in passing inside another doc (e.g. an older draft of
`DETERMINISM.md` once called the state-machine work "Phase 3" before
Input System had grown into its own phase — that reference has been
corrected there; this file is the source of truth going forward).

| # | Phase                        | Status |
|---|-------------------------------|--------|
| 1 | Project Foundation            | ✅ Done |
| 2 | Deterministic Simulation       | ✅ Done |
| 3 | Input system                  | ✅ Done |
| 4 | Custom collision engine        | ✅ Done |
| 5 | Player movement physics        | 🛠 In progress |
| 6 | Player state machine           | 🛠 In progress |
| 7 | Combat system                  | 🛠 In progress |
| 8 | Animation framework            | Not started |
| 9 | Character framework            | 🛠 In progress |
| 10 | First fighter implementation  | 🛠 In progress (pure Fox self-play demo playable — see Master Directive v2 note below; hybrid demo still exists but is paused, not default) |
| 11 | Second fighter implementation | Not started (paused by directive — see `Docs/CURRENT_GOAL.md`; "second fighter" was previously Falco data folded into the hybrid, not a standalone character) |
| 12 | Stage implementation          | Not started |
| 13 | Camera system                 | Not started |
| 14 | HUD/UI                        | Not started |
| 15 | Training mode                 | Not started |
| 16 | Rollback networking            | Not started |
| 17 | Replay system                  | Not started |
| 18 | Optimization and polish        | Not started |

## Notes on 2 → 4

Phase 2 (Deterministic Simulation) ended up delivering most of Phase 3
(Input system) as a side effect — the tick/sim architecture and the
input pipeline turned out to be the same piece of work in practice, and
the input side went further than planned (multi-device support,
rebinding, persistence) because the ring-buffer/provider seam Phase 16
(rollback) needs was cheap to build in immediately rather than retrofit.

Phase 4 (Custom collision engine) is a genuinely new phase: static solid
AABBs and one-way platforms, axis-separated fixed-point resolution, and
a `TestBody` debug rig proving it lands, rests, and drops through
platforms deterministically. See `Docs/COLLISION.md`.

Phase 4 also closed a real gap left over from Phase 2: `ISimObject.Tick()`
had no way to actually reach `SimWorld.GetInput()`, despite that being
part of the documented contract. `Tick()` now takes the owning `SimWorld`
as a parameter.

## Phase 5 status

`Gameplay/PlayerMover.cs` + `Gameplay/MovementConstants.cs` are in:
ground state machine (idle/walk/dash/run, dash-initiate/dash-stop/
turn-around via `InputDecode.IsDashInitiate`), jump-squat-gated short-hop
vs. full-hop, a single air (double) jump, air control, and fast-fall —
all on top of Phase 4's `CollisionResolver`. It replaces `TestMover` in
the live scene (`Main.cs`); `Debug/TestBody.cs` stays as-is, purely for
the Phase 4 acceptance tests. New F9 tests: 5.1 (twin-run) and 5.2 (jump
arc leaves the ground and resettles), in `Debug/DeterminismTest.cs`.

**Every constant in `MovementConstants.cs` is a placeholder** — picked to
make the state machine exercisable and deterministic, not to feel like
any specific character. What's NOT done yet, and is fair game for
whoever picks this back up:
- Real feel tuning (this is generic movement, not any one fighter's).
- Landing lag / L-cancel — no hitbox/animation system exists yet
  (Phases 7-8), so there's nothing to cancel into.
- Preserving air-speed momentum beyond `AirSpeedMax` when it arrives
  from external velocity (e.g. a dash off a ledge) — right now
  `TickAirborne` always steers toward the capped target, which will
  quietly kill dash-off-stage momentum once that matters.
- Ledge-grabbing, wall-jumps/wall-cling, crouch, shield — later phases.
- Per-character stats (Phase 9 replaces the shared `MovementConstants`
  with per-instance data).

## Phase 6 status

`PlayerActionState` (in `Gameplay/PlayerMover.cs`) is a derived view over
Phase 5's movement fields — `Idle`/`Walk`/`Dash`/`Run`/`JumpSquat`/`Jump`/
`Fall`/`FastFall`/`Landing` — computed once per tick by `DeriveActionState()`
after movement and collision are resolved, never assigned anywhere else.
`CurrentState` and `StateFrame` (consecutive ticks in that state) are part
of saved sim state, so they're rollback- and replay-safe like everything
else. New F9 test: 6.1 (`PlayerActionStateTest`), which drives a full
short-hop-and-land and asserts every state got visited in order.

`_landingFramesRemaining` (`MovementConstants.LandingFrames`, 4 ticks) is
purely observational right now — nothing locks the player out of acting
during it, because there's no move system yet to lock out (Phase 7). It
exists now because Phase 8 (Animation) needs a named window to play a
landing clip in, not because landing lag is "done."

What's NOT done: nothing reads `CurrentState` yet (no animation, no combat
gating) — this phase is the naming/plumbing, not the consumer. Hitstun,
shield, tech, ledge-hang, and crouch states don't exist yet; they arrive
with Phase 7 (Combat) and later.

## Phase 7 status

See `Docs/COMBAT.md` for the full writeup. Short version: `Core/Combat/`
has the generic hit math (`Hitbox`, `KnockbackMath` — explicitly a
placeholder formula, not any specific game's real one), and
`PlayerMover.ApplyHit` is the receiving half (damage, knockback launch,
a fixed input-less `TickHitstun` window, new `PlayerActionState.Hitstun`
with top priority in the tick dispatch). New F9 test: 7.1
(`PlayerMoverHitstunTest`) — twin-run determinism through a hit and
recovery back to a normal state.

**No hitbox spawning or hit-detection exists yet** — `ApplyHit` is only
called directly, by tests. Turning it into an actual move requires
Phase 9/10 (Character framework / first fighter).

**Update:** `KnockbackMath` now implements the real Melee knockback
formula (was a linear placeholder) — see `Docs/COMBAT.md`. Real Fox
attribute data (`Characters/Fox/FoxAttributes.cs`, parsed from `PlFx.dat`)
and a hand-transcribed Falco move list (`Characters/Falco/FalcoMoveData.json`,
from fightcore.gg) now exist, but neither is wired into `PlayerMover` or
`MovementConstants` yet — that's still Phase 9's job, not something to
smuggle into Phase 5/7 as a side effect. `MovementConstants` is unchanged
and still the placeholder every live `PlayerMover` actually uses.

**Update 2:** cross-validated the above against MeleeLight, a real
open-source Melee-style engine (`src/` uploaded whole) — not just another
data source, an actual working reference implementation of the physics.
This caught a real bug (knockback was using post-hit percent instead of
pre-hit — now fixed in `ApplyHit`/`KnockbackMath`), resolved two of the
DAT dump's "Unknown" fields in `FoxAttributes.cs` (double-jump momentum
retention, ground-jump takeoff horizontal speed), flagged three places
where the DAT dump and MeleeLight's own attributes.js disagree outright,
and added two systems that didn't exist in this engine at all before:
`HitlagMath` (not wired in yet) and a knockback-decay term in
`TickHitstun` (ported as an adaptation, since this engine uses direction
vectors instead of angles — see `Docs/COMBAT.md`). MeleeLight's real DI
algorithm is now documented in `Docs/COMBAT.md` as a reference for when
Phase 9/10 tackles fixed-point trig, rather than needing to be re-derived
from scratch then.

## Phase 9 / 10 status (Fox/Falco hybrid, first playable demo)

`Characters/` now has real per-character data replacing every shared
placeholder Phase 5-7 left behind:

- `MoveSlot.cs` / `MoveData.cs` (`MoveCategory`, `MoveDef`) / `AngleTable.cs` --
  the generic move-definition machinery. `MoveDef` collapses real multi-hit
  Melee moves to one representative hit (see its own doc comment); `AngleTable`
  bakes Melee-style degree angles into fixed-point direction vectors offline
  (Fx has no trig), including a documented flat-45° placeholder for the real
  Sakurai angle (361).
- `CharacterPhysics.cs` -- per-instance movement stats, replacing
  `Gameplay/MovementConstants.cs`'s shared statics. `FromFox()` is real data
  (`Characters/Fox/FoxAttributes.cs`). `FromFalco()` is MeleeLight-only,
  not cross-validated, kept ready for Phase 11.
- `CharacterData.cs` -- bundles physics + a `MoveSlot -> MoveDef` table.
- `Characters/Fox/FoxMoveData.json` + `FoxMoves.cs` -- Fox's grounded normals
  (jab1/tilts/smashes/dash attack) plus Up-B/Down-B data, sourced from
  MeleeLight's real engine source (`attributes.js` hitbox tables + each
  move's own frame-timer logic) rather than screenshots -- see the JSON's
  `_readme` for exactly what was transcribed and what was simplified.
- `Characters/Falco/FalcoMoves.cs` -- the five moves the hybrid actually
  uses (four aerials + Blaster) transcribed from the existing (Phase 7)
  `FalcoMoveData.json`.
- `Characters/Hybrid/FoxFalcoHybrid.cs` -- the milestone character itself:
  Fox physics + Fox grounded normals + Falco aerials + Falco Blaster for
  Neutral B, per the project synopsis. Side-B/Up-B/Down-B/grab exist as data
  but aren't dispatched by input yet (see `PlayerMover.TryStartAttack`'s doc
  comment) -- **not silently missing, an explicit follow-up.**

`Gameplay/PlayerMover.cs` now takes a `CharacterData?` (defaulting to the
hybrid) instead of the static `MovementConstants`, and gained a full attack
sub-state: `CurrentMove`/`MoveFrame`, hitlag (`HitlagFramesRemaining`,
frozen ahead of even hitstun in `Tick()`'s dispatch), `TryGetActiveHitbox`/
`MarkHitApplied` for the combat system to consume, and `PlayerActionState.Attack`.
Grounded attacks lock horizontal velocity for the move's duration; aerials
keep normal air physics running underneath and apply their own landing lag
on early touchdown instead of the default `LandingFrames`.

`Gameplay/CombatSystem.cs` is the new piece that actually closes the loop:
a small `ISimObject` holding two `PlayerMover` references, ticked *after*
both of them (registration order = tick order), that checks each mover's
active hitbox against the other's whole-body AABB (documented simplification
-- no real per-hitbox spatial placement yet, see its own doc comment) and
calls `ApplyHit`/`ApplyHitlag` on a connect. Both directions are checked
every tick, so two attacks can trade in the same frame.

`Main.cs` now spawns **two** `PlayerMover`s (both defaulting to the hybrid)
plus one `CombatSystem` -- P1 on input slot 0 (keyboard), P2 on slot 1 (a
second controller if present, otherwise idle) -- so the milestone goal from
the synopsis ("a working demo soon even if it's just the spacie hybrid by
itself to play against itself") is live: press attack as P1 and P2 visibly
takes damage, flies into hitstun, and recovers, deterministically.

New F9 test: **9.1** (`HybridSelfPlayCombatTest` in `DeterminismTest.cs`) --
twin-world run with the exact Main.cs setup (two hybrid movers + CombatSystem),
P1 mashing attack against a neutral P2, asserting both twin-hash equality
*and* that P2 actually took damage by frame 300. This is the first test that
exercises attack-dispatch -> hitbox-window -> CombatSystem -> ApplyHit as one
real chain, not each piece in isolation.

**What's explicitly NOT done, for whoever picks this up next:**
- Real per-hitbox spatial offsets (the raw offset data exists in MeleeLight's
  `offsets[...]` tables, pulled during this phase's research but not wired
  through `MoveDef`/`CombatSystem` yet -- hit detection is whole-body-overlap).
- Side-B/Up-B/Down-B/grab/throws aren't reachable via input (`TryStartAttack`
  only ever maps the special button to `NeutralB`).
- No L-cancel timing, no true auto-cancel window, no multi-hit sequencing
  (every move is one representative hit -- see `MoveDef`'s doc comment).
- No animation (Phase 8 is still not started) -- the demo is two colored
  boxes with real Melee-derived numbers underneath, not a visual fighter yet.
- `CombatSystem`'s `SimObjectTypes` factory throws -- cold snapshot rebuild
  of a from-scratch world past what's already live isn't supported (see its
  own doc comment); every real path today reuses the live object in place.

**Post-Phase-9 build fix (this session):** the zip handed off after Phase 9
still had the *pre*-Phase-9 `PlayerMover.cs` (using the old static
`MovementConstants.HalfSize` instead of `CharacterData`/`Attack` dispatch),
even though `CombatSystem.cs`, `Main.cs`, and the full `Characters/` framework
were already updated for it -- a save/zip mismatch, not new work. Re-applied
the Phase 9 `PlayerMover.cs` (CharacterData-backed physics, `CurrentMove`/
`MoveFrame`, `TryGetActiveHitbox`, `ApplyHitlag`, `Weight`/`HalfSize`) so it
matches what the rest of the codebase already expects. No design changes --
next session should pick up at the "explicitly NOT done" list above.

## Phase 11 status (directional specials: Up-B/Side-B/Down-B, dash attack)

Real MeleeLight source (src/characters/fox/moves/UPSPECIALLAUNCH.js and
SIDESPECIALGROUND.js) was available this session, so Up-B (Fire Fox) and
Side-B (Illusion) are now REAL self-propelled movement, not just inert
hitbox data sitting unused:

- `MoveDef` gained two optional fields, `LaunchSpeed`/`LaunchDecayPerTick`
  (Characters/MoveData.cs) — zero for every ordinary move, nonzero only for
  Fox's two self-launching specials.
- `FoxMoves.FireBird` (Up-B): LaunchSpeed 3.8, decay 0.1/tick — transcribed
  directly from UPSPECIALLAUNCH.js's `cVel = 3.8*cos/sin(angle)`. Aim is
  two-way only (straight up, or a 45-degree diagonal toward held stick X) —
  no arbitrary-angle trig (Fx has none). Helplessness after the move ends
  mid-air is approximated by zeroing `JumpsRemaining`, not a real dedicated
  action state.
- `FoxMoves.Illusion` (Side-B, NEW this phase): LaunchSpeed 18.72, transcribed
  from SIDESPECIALGROUND.js's burst velocity. Ground-only (no air Illusion
  yet). No hitbox/damage data transcribed this pass — it moves Fox but can't
  hit anyone yet. Decay rate (0.468/tick) is a deliberate departure from
  MeleeLight's real two-stage 18.72->2.1->0 curve (this engine's model can
  only do one decay phase) — tuned to fully stop within the move's own
  TotalFrames instead of leaving a wrong-looking residual slide.
- `PlayerMover.TryStartAttack` now reads stick direction on the special
  button (up/down/side/neutral -> UpB/DownB/SideB/NeutralB) instead of always
  going to NeutralB, and dash-attack now fires whenever attack is pressed
  while actually dashing/running (DashAttack MoveDef data existed since
  Phase 9 but was unreachable until now).
- `FoxFalcoHybrid`'s moveset now includes UpB/SideB/DownB.

**What's explicitly NOT done, for whoever picks this up next:**
- Illusion has no hitbox — can't damage an opponent yet.
- No real per-hitbox spatial offsets anywhere (still whole-body-overlap hit
  detection — this is the single biggest accuracy gap left, see Phase 9/10's
  own note on this).
- Grab/throws still not modeled/dispatched at all.
- No animation (Phase 8 still not started).
- Air Illusion (SIDESPECIALAIR.js, a gradual accel curve) not modeled.

## Post-Phase-11 status (placeholder visuals — the sim was invisible)

Everything through Phase 11 was real and tested but genuinely unwatchable:
`Main.tscn` had one `Sprite2D` with no texture assigned, no P2 node, and no
stage geometry drawn — "just the code behind the scenes." This session
closes that gap with the cheapest honest fix, not new art:

- `Main.cs` now builds flat-color placeholder boxes at runtime (`Image`/
  `ImageTexture` generated in code, no asset files) for P1 (blue) and P2
  (red), sized directly from `CharacterPhysics.HalfSize` — the box you see
  IS the hurtbox size, not a guess.
- `TestStage.Default`'s solids and one-way platform are now drawn the same
  way (gray boxes), so the floor and platform are visible instead of empty
  space.
- `Main.tscn` gained the missing `TestSprite2` node so P2 actually renders
  (previously `_sprite2` silently stayed null).

This should be enough to open the project in the Godot editor and watch/
play an actual 1v1 hybrid match, not just read numbers in the debug label.
**Explicitly still a placeholder, not Phase 8:** no animation, no sprite
flipping on facing change, no camera framing, no HUD/percent display beyond
the debug label. Real character art and Phase 8 (Animation) are still open.

## Post-visuals status (Illusion hitbox, controls reminder)

Two small follow-ups from the same session that added placeholder visuals:

- **Illusion (Side-B) can now actually hit someone.** It previously moved
  Fox but had all-zero damage/knockback/active-frame data (see FoxMoves.cs's
  own note). Gave it real numbers -- 3% damage, low knockback -- sourced from
  SmashWiki/Smashpedia's move description, since fightcore.gg's own Illusion
  page returned a server error mid-session and couldn't be fetched directly;
  flagged in FoxMoves.cs as an approximation, not a verified transcription,
  same as this file's other honesty-first placeholders. Active window (frames
  2-14) is deliberately NOT fightcore's real frames 22-25 -- this engine's
  burst fires on frame 1 (a documented gap from Phase 11), so the hitbox is
  active while Fox is actually moving fast in THIS engine's timing, not
  copying frame numbers that assumed a startup this engine doesn't have.
  `AngleTable` gained a `[40]` entry to back it (computed the same offline-
  Python way as every other entry there).
- **Default keyboard bindings** (already implemented since Phase 3, just
  worth restating since testers keep finding jump and stopping there):
  Move WASD/Arrows, Attack J, Special K, Jump W/Space, Shield L, Grab `;`.
  Attack+direction picks jab/tilt/smash; Special+direction picks Neutral-B
  (Falco Blaster)/Up-B (Fire Fox)/Side-B (Illusion)/Down-B (Reflector).

## Post-Illusion-fix status (Fox's own aerials, Falco override rewire, real Phantasm)

This session picked back up mid-transcription: `FoxMoves.cs`/`FalcoMoves.cs`/
`AngleTable.cs`/`MoveData.cs` had already been re-transcribed directly from
MeleeLight's real source per the Melee Lite Translation Blueprint, but
`FoxFalcoHybrid.cs` (the piece that actually wires them together) hadn't
been updated to match, and the handoff had a couple of real breaks in it:

- **`FoxMoves.cs` gained Fox's own Nair/Fair/Uair** (real data from
  `attributes.js`/`ATTACKAIRN.js`/`ATTACKAIRF.js`/`ATTACKAIRU.js`) — the
  hybrid previously had zero Fox aerials and silently fell back to Falco's
  for all five, which was only correct by the blueprint for Bair/Uair.
  Fox's Dair still isn't modeled (needs `SetKnockback` support this engine's
  `Hitbox`/`KnockbackMath` don't have — same documented gap as Fox's Uair1).
- **`FalcoMoves.cs` was trimmed** to exactly the moves the blueprint's
  override list calls for (DTilt/UTilt/DSmash/BAir/UAir/Blaster/Phantasm) —
  its old Nair/Fair/DownAir/`Aerials` dictionary are gone now that Fox
  supplies its own aerials. **This broke the build**: `FoxFalcoHybrid.cs`
  still referenced `FalcoMoves.Aerials`, which no longer exists.
- **The uploaded `FalcoMoves.cs` was also missing its closing brace** for
  the class (a genuine truncation, not a stale-comment issue) — fixed.
- **`FoxFalcoHybrid.cs` rewired**: base moveset is now Fox's grounded
  normals + Fox's own aerials, with Falco's DTilt/UTilt/DSmash/BAir/UAir
  layered on top, matching the blueprint's override list exactly. **Side B
  now routes to Falco's Phantasm instead of Fox's Illusion** — the
  blueprint's "Specials: Laser, Side B" line, and `FalcoMoves.cs`'s own
  header comment ("Phantasm (Side B)") both call for this, not just
  Neutral B. Up B and Down B stay Fox's own (FireBird/ReflectorHit) since
  they're not in the blueprint's Falco override list.
- **Known regression from this swap**: Fox's Illusion was a real
  self-propelled dash (`LaunchSpeed` 18.72, transcribed from
  `SIDESPECIALGROUND.js`); Falco's Phantasm has a real hit but
  `LaunchSpeed` 0 — its own doc comment already flags that the movement
  burst wasn't ported this pass. So post-swap, Side-B in the actual hybrid
  hits but no longer moves Fox. Fox's own Illusion MoveDef is untouched in
  `FoxMoves.cs` (unused by the hybrid now, kept for a Fox-only build).
- `PlayerMover.TryStartAttack`'s doc comments and the grounded-only Side-B
  guard updated to reference Phantasm as the hybrid's actual Side-B move,
  rather than stale references to Illusion.

**What's explicitly NOT done, for whoever picks this up next:**
- Phantasm's movement burst (`cVel.x = 16.50*face`, from
  `SIDESPECIALGROUND.js`) isn't ported to `LaunchSpeed`/`LaunchDecayPerTick`
  — same single-phase-decay re-derivation work Fox's Illusion already did,
  not yet done for Falco's version. Until then, Side-B in the hybrid is a
  stationary hit, not a dash.
- Fox's Dair still unmodeled (SetKnockback gap).
- No `dotnet build` was run to confirm this compiles — no .NET SDK
  available in this environment. Verified by hand instead: every symbol
  `FoxFalcoHybrid.cs` references exists in the new `FoxMoves.cs`/
  `FalcoMoves.cs`, every angle either file's moves use has an `AngleTable`
  entry, and brace-balance was checked on all four rewritten files. Still
  worth an actual build before trusting this.

## Post-hybrid-rewire status (the "flies off the map" knockback bug)

User reported combat "feels slow and terrible" and that any hit sends the
dummy flying off the map — supplied a debug-HUD screen recording as
evidence. The HUD only showed sim telemetry (state/percent/hash), not the
actual viewport, so diagnosis went through the numbers rather than the
visual, then confirmed against real MeleeLight source rather than guessed:

**Root cause found in `Core/Combat/KnockbackMath.cs` / `Gameplay/
PlayerMover.cs`, confirmed by reading `src/physics/hitDetection.js` directly:**
`KnockbackMath.ComputeMagnitude`'s return value is a knockback MAGNITUDE, not
a velocity — real Melee/MeleeLight scales it by 0.03 before using it as
launch speed (`getHorizontalVelocity`/`getVerticalVelocity`:
`initialVelocity = knockback * 0.03`). `PlayerMover.ApplyHit` was applying
the raw, unscaled magnitude directly as `Velocity` — every hit launched the
defender at roughly **33x** real Melee speed. Worked the exact numbers from
the clip: Fox's Up-B connecting with a 7%-damage, weight-75 target computes
a knockback magnitude of ~87 — real Melee turns that into a ~2.6-units/tick
launch (about 1.4x dash speed, a solid hit); this engine was applying ~87
units/tick directly (about 46x dash speed), which crosses a normal stage in
a single tick. That's the whole "sent flying off the map" symptom, and since
the fixed 0.051/tick knockback decay then has ~33x more (wrong-magnitude)
velocity to bleed off, it also explains "feels slow" — a hit that should
resolve in about a dozen frames of pushback was instead leaving the target
in a runaway launch far longer than that (when it wasn't already past the
blast zone).

**Fix:** added `KnockbackMath.VelocityScale` (0.03, doc-commented with the
exact source lines) and multiplied it into `ApplyHit`'s launch velocity.
Deliberately did NOT touch `ComputeHitstunFrames` — confirmed by reading
`getHitstun` directly that real Melee's hitstun formula takes the RAW
(unscaled) magnitude, so hitstun timing was never part of this bug, only
the velocity was.

**What's explicitly NOT verified yet, for whoever picks this up next:**
- No `dotnet build`/actual playtest confirms this fixes the feel in-engine —
  verified by hand-computing the formula against the clip's own numbers
  (P2 was at 7%, weight matches Fox's 75), not by running the game.
- The "feels slow" complaint was only partly explained by this bug (runaway
  launch + slow decay). Movement-only feel (dash dance, wavedash, jump
  arcs) wasn't re-audited this pass — worth a dedicated pass through the
  Movement checklist below now that combat isn't instantly flinging players
  off-stage and making that hard to observe.
- Debug HUD doesn't surface `MoveFrame` (only the move NAME and category) —
  made isolating a separate hunch (whether Up-B's attack state was
  outlasting its own 45-frame `TotalFrames`) slower than it needed to be.
  Worth adding to the HUD.

**Verification checklist status** (from the Melee Lite Translation
Blueprint), reassessed against what's actually been built/tested so far —
unchecked items are real gaps, not just untested:
- Movement: not re-verified this pass (see above) — Fox physics constants
  are real transcribed MeleeLight values (see FoxAttributes.cs), but
  dash-dance/wavedash/short-hop timing haven't been played and confirmed.
- Combat: Jab/Tilts/Smashes/Aerials/Fire Fox have real transcribed frame
  data (see FoxMoves.cs/FalcoMoves.cs); Shine (Reflector) has no reflect
  behavior modeled, only a placeholder hit; Laser is a narrowed 2-frame
  adaptation, not a real projectile; Illusion works in FoxMoves.cs but is
  unused by the hybrid as of the last session's rewire (Phantasm replaced
  it in Side-B, and Phantasm doesn't move Fox yet — see that session's note).
- Physics: knockback magnitude formula is real and now correctly scaled to
  velocity (this session's fix); DI/vectoring multiplier exists but isn't
  wired to real stick input yet; hitlag IS wired in (`CombatSystem.cs` calls
  `HitlagMath.ComputeHitlagFrames` on every connect — its own doc comment
  said otherwise, that was stale from before `CombatSystem` existed, fixed
  this pass); shield behavior isn't modeled at all.
- Stage: Battlefield itself isn't ported (`TestStage.Default` is a flat
  placeholder, not real Battlefield geometry) — platforms/ledges/blast
  zones only exist in that simplified form.

## Melee Lite Translation Directive — Step 1: Battlefield collision + blast zones

New direction as of this session: the Fox/Falco hybrid is paused/ignored
entirely (not deleted, just not being worked on), and the project is
re-scoped to a strict 6-step, Fox+Battlefield-only foundation, built
bottom-up so each layer is verified before the next depends on it. This
entry covers Step 1 only: Battlefield's collision geometry and blast zone.

**What was ported, transcribed directly from `src/stages/vs-stages/
battlefield.js`** (not approximated): the main ground segment, all 5
ceiling segments and all 6+6 wallL/wallR segments that make up the
underside chamfer/notch shape, all 3 one-way platforms, the blast zone box,
both ledge positions, and all 4 starting-point/respawn-point pairs with
their facing directions. Every value cross-checked programmatically against
an independently-computed conversion table before being trusted, and one
real transcription bug (a sign flip on the blast zone's upper-Y corner)
was caught and fixed this way rather than shipped.

**Key finding, confirmed by reading the source rather than assumed:**
MeleeLight's stage/physics data is Y-UP (gravity is `cVel.y -= gravity`,
i.e. falling makes Y more negative; the blast-zone floor check is
`pos.y < blastzone.min.y`). This engine's `FxVec2`/`FxAabb` are explicitly
Y-DOWN (stated in `FxVec2.cs`'s own doc comment). Every Y coordinate in the
new `Stages/Battlefield.cs` is therefore the source value negated — same
adaptation this codebase already made for Gravity's sign when Fox's
attributes were first ported, not a new convention invented for this file.

**Architecture gap surfaced, not yet fixed (flagging clearly, not silently
patching over it):** Battlefield's real shape needs arbitrary line-segment
collision — most of the ceiling/wall segments are diagonal, not
axis-aligned — but this engine's existing `CollisionResolver.cs` only knows
how to resolve an AABB body against AABB solids and flat one-way platforms.
It cannot walk Battlefield's real geometry at all as it stands. Also,
MeleeLight's actual ground-collision algorithm (`physics.js`:
`dealWithGround`/`fallOffGround`/`moveAlongGround`) is a full ECB-corner-vs-
segment resolver with edge connectivity and teetering — a fundamentally
different model from `CollisionResolver.cs`'s current one. Rewriting the
resolver to consume real segments is explicitly **Step 2's job** (Core
physics), not this one — porting the resolution algorithm now, before
ground/air/gravity/jump physics are themselves verified, is exactly the
"fixing dozens of interacting systems at once" the 6-step ordering exists
to avoid.

**What Step 1 actually delivers, concretely:**
- `Core/Sim/Collision/FxSegment.cs` — new 2-point line-segment primitive
  (real ground/ceiling/wall shapes aren't axis-aligned; `FxAabb` can't
  represent them).
- `Core/Sim/Collision/FxAabb.cs` — added `FromMinMax` (for blast-zone-style
  corner data) and `Contains` (point-in-box, for blast zone checks) as
  pure additions; existing center+halfsize API untouched.
- `Core/Sim/Collision/StageGeometry.cs` — added `Ground`/`Ceiling`/`WallL`/
  `WallR` segment lists, `BlastZone` (nullable — `TestStage`'s debug floor
  still has none), `Ledges`, `StartingPoints`/`RespawnPoints` (paired
  position+facing via a new `FacingPoint` struct instead of parallel
  arrays), and `IsPastBlastZone`. All additive — `Solids`/`Platforms` and
  every existing consumer (`TestStage.cs`, `TestBody.cs`,
  `CollisionResolver.cs`, `DeterminismTest.cs`) are untouched and unaffected.
- `Stages/Battlefield.cs` — the real transcribed data itself, thoroughly
  doc-commented with exactly what was and wasn't ported and why (render-only
  fields, the undefined `connected` graph, and the genuinely-nonexistent
  camera-bounds constant — see the file's own header for details on each).

**Explicitly NOT done / NOT verified, for Step 2 (or whoever picks this
up):**
- No collision RESOLUTION against this geometry exists yet — only the data
  and two simple, movement-independent queries (`IsPastBlastZone`, and the
  raw segment/ledge/spawn lists themselves for Step 2 to consume). Nothing
  in `PlayerMover`/`CollisionResolver` reads `Battlefield.Geometry` yet.
- No camera-bounds constant — flagged in `Battlefield.cs` as genuinely
  absent from MeleeLight's own Battlefield definition, not invented.
- No `dotnet build` — no .NET SDK in this sandbox. Verified by hand: brace
  balance on every touched/new file, every literal cross-checked against an
  independently computed conversion table (catching the blast-zone sign
  bug), and confirmed `FxVec2`'s constructor/field names and the project's
  glob-based `.csproj` (new files under `Stages/` are picked up
  automatically, no manual file-list edit needed).

## Melee Lite Translation Directive — Step 2: Core physics (segment collision + ECB)

Step 1 landed Battlefield's real geometry and then could not resolve against
any of it — a gap flagged in StageGeometry.cs, FxSegment.cs and Battlefield.cs
alike. Step 2 closes it. Full writeup: `Docs/STEP2_PHYSICS.md`.

**Headline finding — body size was in the wrong unit system.** Velocities,
gravity and traction were ALREADY in MeleeLight units and internally consistent
with Step 1's stage data (Fox dashes the full 136.8-wide stage in ~68 ticks,
which is correct). Body size was the sole outlier, still the pixel-scale
`HalfSize = (20,30)` placeholder: that made Fox **40 units wide on a 136.8-wide
stage, 29.2% of the entire stage**, against a real figure of 4.4% (Fox's true
ECB is 6 wide, 13 tall — `fox/ecb.js` WAIT `[4,3,9,13]`). Every distance-shaped
quantity in the engine was off by ~6.7x.

`CharacterPhysics.FromFox()`'s own doc comment diagnosed this last pass and
then reverted, because swapping HalfSize alone broke DeterminismTest's combat
check (P1/P2 spawn a fixed 30 apart; a hit only lands at the inflated size).
That reasoning was right — a half-measure IS worse — but the conclusion should
have been "fix the spawn distance too", not "keep the wrong body size". There
is no whole-engine rescale to do; only the spawn constants move.

Render scale for whoever wires the camera: MeleeLight draws Battlefield with
`scale: 4.5, offset: [600,480]` (battlefield.js — real fields, confirmed
consumed only by stagerender.js). That is the authoritative sim-to-screen
transform; the render layer multiplies by it rather than the sim inflating
itself to suit a camera.

**Delivered (all ADDITIVE — nothing existing was modified or deleted):**
- `Core/Sim/Collision/Ecb.cs` — the real 4-point ECB diamond, transcribed from
  physics.js's own construction. Origin is the FEET, not the centre (what the
  MeleeLight algorithm assumes everywhere, and what makes slope-following
  expressible at all); Y-down, same negation Step 1 applied to stage data.
- `Core/Sim/Collision/SegmentMath.cs` — fixed-point segment queries. The
  `coordinateIntercept`/`extremePoint` family, with MeleeLight's angle
  comparisons re-expressed as sign tests since `Fx` has no trig.
- `Core/Sim/Collision/SegmentCollisionResolver.cs` — the resolver. Ports the
  MODEL from `dealWithGround`/`fallOffGround`/`dealWithCeilingCollision`: a
  grounded fighter is ON A NAMED SURFACE and follows its Y as X changes until
  its X leaves the span, then falls off. It does not re-search the stage every
  tick. That is the structural difference from the old box resolver and the
  reason ledges can exist.
- `Characters/Fox/FoxEcb.cs` — Fox's real ECB (WAIT + FALL).

**Verification actually performed — read this before trusting any of it.**
No .NET SDK in the authoring environment, so THIS WAS NOT COMPILED. Instead the
algorithm was re-implemented in Python against the real Battlefield data and
run (`Docs/verify_step2.py`), which caught two real bugs before they shipped:
  1. **Jumping did nothing** — a grounded fighter that jumped was re-snapped to
     the floor the same tick, because surface-follow ran regardless of upward
     velocity. Fixed with a `Velocity.Y >= 0` guard.
  2. **Platform drop-through was eaten** by that same surface-follow. Fixed by
     releasing the surface when passing through a platform.
Passing checks: full hop leaves the ground and resettles (apex 31.3 units,
matching v^2/2g = 29.4 for v=3.68/g=0.23 plus discrete-integration overshoot);
walking off the right ledge falls rather than hovering, with the edge event
firing; falling onto a side platform lands on the PLATFORM not through it;
drop-through reaches the ground in 14 ticks; identical inputs give identical
results. Brace/paren balance checked on every file. That is the whole extent of
it — an actual build and an in-engine F9 run are still required.

**Deliberately NOT ported in Step 2:**
- The full ECB-corner sweep. `environmentalCollision.js` (1343 lines) sweeps all
  four ECB points with corner cases, edge connectivity, ECB squashing and
  interpolated sub-steps. Here GROUND uses the true bottom point (the part that
  must be exact, and is); CEILINGS and WALLS use the ECB's bounding box. Wrong
  in exactly one visible way: against the diagonal underside chamfers a fighter
  stops slightly early instead of sliding along the slope.
- Per-frame ECB animation (MeleeLight has one ECB per state per frame; no
  animation system exists yet to index it, so one static ECB per character).
- `connected` edge adjacency — Battlefield defines none (single ground
  segment), so nothing to test against. `WalkedOffEdge` is the hook.
- Teetering, ledge grabs, wall jump/cling, ECB squash — Step 6.
- Swept/continuous motion. Still discrete, and this matters MORE now: the stage
  is 136.8 wide and a knockback launch can exceed that in a few ticks. Step 4.

`CollisionResolver.cs` is left in place and untouched — `Debug/TestBody.cs` and
the Phase 4 F9 tests are built on it and still pass. Migrate callers only once
Step 2's own tests pass in-engine.

**Integration still required, NOT done in this pass:** `PlayerMover.cs` (757
lines) still uses centre-origin `HalfSize` and the old resolver. Rewiring it
changes the meaning of `Position` from centre to feet, which touches rendering,
combat reach, and every spawn constant in `Main.cs` and `DeterminismTest.cs`
simultaneously. Doing that blind — without a compiler, on top of a resolver
that has itself never been built — is how the last few sessions produced files
that didn't compile. One focused pass, with a build available.

## Step 2b — integration: the engine now RUNS in Melee units on real Battlefield

Step 2 shipped the collision core as additive files nothing called. This
checkpoint wires it in, so the flip to real units is live rather than latent.

**Confirmed direction (user call):** gameplay/collision/physics/knockback all
stay in Melee units; only rendering deals in pixels. Implemented as a single
`World` Node2D in Main.cs carrying `Scale = 4.5` (Battlefield's own `scale`
field) and an origin offset — so every piece of render code keeps working in
plain sim units instead of scattering conversions through the file.

**Changed:**
- `CharacterPhysics` — `HalfSize` is no longer independent data; it is DERIVED
  from a new `Ecb` field and can never drift from the collision shape again.
  `FromFox()` now uses `FoxEcb.Default` (real 6x13) instead of the 20x30 pixel
  placeholder.
- `PlayerMover` — resolves against `SegmentCollisionResolver` instead of
  `CollisionResolver`; `Position` now means the FEET; gained `Surface` /
  `SurfaceIndex`, both round-tripped into the next resolve **and serialized**
  (they are sim state — omitting them from SaveState/LoadState would desync
  rollback the moment a fighter stood on a platform).
- `CombatSystem` — new `AttackReach` (8 units, placeholder). This had to exist
  now, not later: the old whole-body overlap was accidentally about the right
  reach only because the body was 6.7x too wide. Reach is also now applied in
  the direction the attacker FACES, so a jab no longer hits someone behind you.
- `Main.cs` — uses `Battlefield.Geometry`, draws the real ground/platforms,
  anchors sprites feet-first, spawns P1/P2 at x=-12/+12 dropping in from above.
- `Debug/TestStage.cs` — gained a ground SEGMENT matching its existing solid's
  top edge. Added, not substituted: `Solids` is untouched so TestBody and the
  Phase 4 tests keep passing on the old resolver.
- `Debug/DeterminismTest.cs` — PlayerMover tests moved to Battlefield with
  feet-origin spawns; resting-Y expectations are now `0 + SurfaceSnapEpsilon`.

**One non-obvious thing worth knowing:** fighters must spawn ABOVE the ground,
not exactly on it. The airborne ECB hangs 4 units below the origin, so a
fighter spawned at exactly y=0 starts already "through" the floor and never
registers a landing. All spawns therefore drop in from y=-20 — which is also
how a real match starts.

**Verified before shipping** (still no .NET SDK — algorithm mirror, not a
build): settling lands on ground at exactly `0 + 1/1024`, matching the new test
expectation; full hop leaves the ground, apexes at 31.3, and resettles at the
same value; jab connects at the new 12-apart spacing and provably would NOT
have at the old 30-apart spacing. Brace balance and duplicate-type scan clean
across all 51 files.

**Next (Step 3, Fox locomotion):** `dAccA` 0.1 / `dAccB` 0.02 two-stage dash
accel, `StopTurnInitialSpeed`, `TiltTurnForcedVelocity` and `runTurnBreakPoint`
are all already transcribed and sitting unused in `FoxAttributes.cs`.

## Step 2b fix pass — two bugs caught by the first in-engine run

The Step 2b build compiled clean but F9 failed two checks and fighters fell
through the floor on the live stage. Both were mine; both are fixed.

**1. ECB airborne-bottom sign was transcribed backwards (the real one).**
MeleeLight is Y-UP and builds the airborne bottom point as `pos.y + offset[0]`,
which in Y-UP means ABOVE the origin — Melee characters tuck their legs up when
airborne. Translating the "+" literally into this engine's Y-DOWN space put the
airborne bottom BELOW the feet. Consequence: a fighter spawned level with the
ground started already through the floor, its bottom point never crossed the
surface downward, and it fell forever. That is exactly the observed
"characters fell through the floor immediately."

Fixed by renaming the field to `AirborneBottomRise` (documented so it cannot be
re-flipped) and, more importantly, by having `Ecb.Bottom()` return the FEET in
both grounded and airborne cases. The rise is a refinement that only pays off
inside the full ECB-corner sweep this engine has not ported; applying it in
isolation buys nothing and costs a visible pop on every landing (touchdown
would trigger with the feet still short of the surface, then snap them down).
Feet-based crossing is exact, pop-free, and makes "spawn standing on the
ground" work with no special case.

**2. Tests guessed a settle time instead of waiting for the landing.**
5.1/6.1 assumed the fighter spawned already resting and gave it a fixed handful
of neutral ticks before jumping. With Step 2b's airborne spawns it was still
falling, so the "jump" became a mid-air DOUBLE jump — which from y=-20 overshoots
and lands on the top platform at y=-54.4. That is precisely the reported
`resettled at Y = -54.399 (expected 0.001)`, and it also explains 6.1's
`Idle=NO JumpSquat=NO` (never grounded, so neither state was ever entered).
Both now settle with `for (...; !mover.Grounded; ...)` before jumping.

Also fixed: `Main.cs` spawned at `Fx.Zero` while its own comment and the
roadmap both claimed y=-20. Now drops in from y=-10.

**Verified against the observed failure:** the algorithm mirror reproduces
`-54.399` exactly for the mid-air-double-jump case, confirming the diagnosis
rather than guessing at it; and with the fixes, spawn-at-y=0 lands in 2 ticks,
Main's y=-10 spawn lands in 10, and the ground jump apexes at 31.3 and
resettles on GROUND at `0 + 1/1024` — matching 5.1's assertion. The ground jump
correctly does NOT reach the top platform (31.3 < 54.4), which is right for Fox.

## Melee Lite Translation Directive — Step 3: Fox locomotion (idle through dash dance)

`PlayerMover.TickGrounded` is now a direct transcription of MeleeLight's ground
movement rather than the Phase 5 placeholder approximation. Sources:
`src/characters/shared/moves/{WALK,DASH,RUN,TILTTURN,SMASHTURN}.js` and
`src/physics/actionStateShortcuts.js` (`reduceByTraction`, `checkForSmashTurn`).

**The formulas are gap-proportional, not flat steps.** The old code used a
`MoveToward(current, target, accel)` for everything. Real WALK is
`(tempMax - vx) * (1/(walkMaxV*2)) * (walkInitV + walkAcc)` and real RUN is
`(dMaxV*lsX - vx) * (1/(dMaxV*2.5)) * (dAccA + dAccB/|lsX|)` — acceleration
proportional to the REMAINING gap. A flat step reaches the same top speed with
the wrong ramp, which is most of why the old movement felt off.

**New stats wired into `CharacterPhysics`** (all previously transcribed in
`FoxAttributes.cs` and sitting unused): `WalkInitialSpeed`, `DashInitialSpeed`,
`DashTurnSpeed`, `DashAccelA/B`, `DashFrameMin/Max`, `DashTotalFrames`. Falco's
equivalents came from `src/characters/falco/attributes.js` this pass.

**New states:** `GroundState.TiltTurn` and `GroundState.SmashTurn`, plus a
`GroundTimer` counting UP from 1 (MeleeLight expresses every dash decision as
`timer == n` / `timer > n`, so counting up is a transcription rather than a
re-derivation of the old count-down `DashFramesRemaining`).

**Two frames of stick history, not one.** `checkForSmashTurn` reads
`input[p][2].lsX` — TWO frames back. Without `_prevStickX2` a held stick reads
as a fresh flick every frame and dash-dance is impossible. Both new fields are
serialized (31 writes / 31 reads).

**Dash-dance is a one-frame gate, and getting that wrong is visible.** An early
draft let SMASHTURN dash out over a >=5 frame window; the mirror showed the
fighter sliding 26 units (19% of the stage) instead of holding position.
`SMASHTURN.js` actually allows it on `timer === 2` EXACTLY. With the real gate:
tight flick periods (6-12 frames) drift 2-8%, slow ones (14-16) drift 16-25% —
i.e. tight dash-dancing holds position and sloppy dash-dancing travels, which is
the real behaviour.

**Also transcribed faithfully and flagged in-code:** `DASH.js` genuinely adds
`tempAcc` TWICE on its non-overshoot branch (once before the bounds check, again
inside the else). It looks like a bug in the source; it is kept as-is with a
comment telling the next reader to check the source before "fixing" it.

**Verified (algorithm mirror, still no SDK):** walk converges on
`walkMaxV * stick` exactly (0.96 for a 0.6 stick); dash impulse lands on frame 2
at 2.2 (`dInitV` 2.02 clamped to `dMaxV`); dash into run converges on 2.2;
dash-dance reverses facing and holds position. New F9 test **3.1
`FoxLocomotionTest`** asserts all four in-engine plus twin-run determinism.

**NOT done in Step 3:** RUNBRAKE is collapsed into Idle + double traction (it
needs an animation to be worth its own state — Phase 8); no dash buffer, so
TILTTURN's frame-6 dash-out is ungated where the source checks
`phys.dashbuffer`; no teeter/OTTOTTO at ledges; crouch/SQUAT absent. Shield,
grab and the analog-trigger interrupts that DASH.js checks first are Step 6.

## Step 3 fix pass — the invisible wall at the ledge

Step 3's own F9 test (3.1) failed on first run with `walk=0` and `run=0` while
dash-dance passed cleanly (12 facing changes, 2.72 drift). The asymmetry was the
clue: dash-dance stays near x=0, walk and run travel.

**Root cause, in `SegmentCollisionResolver.ResolveWallList`: touching counted as
overlapping.** Battlefield's underside chamfer has a `wallR` segment running
(68.4, 0) -> (65, 6) — straight down from the ground lip. A grounded fighter's
feet sit at exactly y=0, which IS that wall's top edge, and the vertical-overlap
test used a strict `<`. So walking right hit an invisible wall at **x = 62.0**
(confirmed exactly, by replaying the real wall data), velocity was zeroed, and
**the ledge at 68.4 could never be reached at all**. Changed to `<=` / `>=`,
which requires the ECB to genuinely extend into the segment's span.

Verified the fix does not lose what those walls are for: a fighter recovering
from BELOW the lip (feet y=+4 at x=66) is still blocked, and so is one deep
under the stage at the central notch. Only the exactly-touching case changed.

**A second, smaller bug in the test itself:** it drove 150 ticks from x=0, which
walks clean off a stage that is only +/-68.4 wide, so it was measuring a fighter
in mid-air. Speed tests now spawn at x=-50 with runway-appropriate durations
(walk 50 ticks -> converges at 0.9575 with 40 units to spare; run 40 ticks ->
2.2000, still on stage).

**Added a ledge-reach assertion** to 3.1, since this class of bug is invisible
unless something deliberately tries to walk off the edge.

## Step 3 fix pass, round 2 — the epsilon the first fix forgot

The `<=` fix above did NOT work in-engine: 3.1 still reported
`ledge reachable (x=61.999, grounded=True) *** NO ***`. That number is
`65 - 3 - 1/1024`, i.e. the wall push-out, unchanged.

**Why the first fix missed:** `TryLand`/`TryStayOnSurface` snap grounded feet to
`surface + SurfaceSnapEpsilon` — a hair BELOW the ground line. So a grounded
fighter's feet are genuinely inside the underside chamfer's Y span by 1/1024,
and an exact-touch test (`bottom <= segTop`) still reports a collision. The
algorithm mirror modelled feet at exactly y=0 and therefore agreed with the
broken code — the same failure mode as the ECB sign bug earlier: **a harness
written from the same mental model as the code cannot catch a bug in that
model.** The mirror now models the snap epsilon explicitly and reproduces the
x=62.0 block exactly.

**The real fix, stated as a rule rather than a tolerance:** a GROUNDED fighter
cannot walk into a wall lying entirely at or below the surface it is standing on
(`if (wasGrounded && segTop >= bottom - SurfaceSnapEpsilon) continue;`). The
epsilon term cancels the known snap and nothing else. Scoped to the grounded
case on purpose — while airborne the full test still runs.

Verified across every case these walls exist for: airborne recovery below the
lip (feet +4 at x=66) still blocked; airborne under the central notch still
blocked; a hypothetical wall rising ABOVE the ground line still stops a grounded
fighter; and walking from x=40 now reaches x=68.6, past the ledge.

## Master Directive v2 — scope narrowed, hybrid paused, pure Fox is now default

The user issued a new master directive (full text in `Docs/CURRENT_GOAL.md`,
which is now the first thing to read, ahead of this file) that narrows scope
to a single deliverable: a fully playable Fox-vs-Fox match on Battlefield.
It explicitly says to forget the Fox/Falco hybrid for now.

The live default character had drifted from that: `PlayerMover`'s default
and `Main.cs`'s two-fighter demo were both `Characters/Hybrid/
FoxFalcoHybrid.cs`. Added `Characters/Fox/FoxCharacter.cs` — a pure-Fox
`CharacterData` (grounded normals + aerials + Up-B/Side-B/Down-B, all from
`FoxMoves.cs`, zero Falco references) — and repointed `PlayerMover`'s
default and `Main.cs`'s instantiation/imports/ECB-sizing reference at it.
`FoxFalcoHybrid.cs` itself is untouched and not deleted (still real,
previously-verified Phase 11 work; the directive's own priority order puts
"expansion" after the foundation, not never) — it's just no longer what
gets spawned by default.

**Not compiled/run this session** — no Godot/dotnet network access in this
environment (see `Docs/CURRENT_GOAL.md`'s "known environment limitation").
This is a small, mechanical swap (two `using`s, one field default, one
instantiation site) reasoned through by reading every call site, not new
logic — but per the directive's own Rule 2 ("every feature must compile
before the next feature begins"), **compiling this in the actual Godot
editor is the first thing the next session needs to do**, before adding
anything on top.

See `Docs/CURRENT_GOAL.md` for the full priority queue against Master
Directive v2's Phase 1 checklist (stocks/blast-zones/respawn/camera are
entirely unstarted; real per-hitbox spatial placement is the biggest
existing combat-accuracy gap).

## Step 3 fix pass, round 3 — the actual bug: gravity is added while grounded too

The user compiled and ran this in the real Godot editor for the first time
(previous sessions only had an algorithm mirror, never the real compiler —
see `Docs/CURRENT_GOAL.md`'s "known environment limitation" from before this
was resolved). F9's test suite passed on everything **except** 3.1's ledge
assertion, which reported the exact pre-"round 2" number again:
`x=61.999, grounded=True *** NO ***`. That exact number recurring, byte for
byte, was the tell that round 2's fix was not actually being reached.

**Root cause: `bottom` in `ResolveWallList` is not what the round-2 fix
assumed.** Walls resolve (step 1) BEFORE the ground-snap (step 3) in the
same tick (`SegmentCollisionResolver.Resolve`). `PlayerMover.TickGrounded`
re-adds gravity to `Velocity.Y` every tick, even while resting at rest on
flat ground (its own comment: the resolver only re-confirms `Grounded` when
`Velocity.Y` is downward, so resting contact needs re-triggering every
tick). So by the time `ResolveWalls` reads `bottom = ecb.Bottom(movedOrigin)`,
that position is `restingY + gravity`, not `restingY` — a real, sizeable
step below the ground line, not the ~epsilon round 2 assumed. `segTop(0) >=
bottom - SurfaceSnapEpsilon` was therefore false every single tick a
grounded fighter was at rest, the skip never fired, and the original
invisible-wall bug was never actually gone — round 2 read correct in
isolation (the algorithm mirror modeled a resting fighter without
reproducing the per-tick gravity re-add) but didn't match what the real
code does across a full tick.

**Fix:** compare against `previousOrigin.Y` instead — last tick's actual
RESOLVED resting position (written by `TryStayOnSurface`), untouched by
this tick's gravity nudge, which is exactly "was the surface I was just
standing on at/above this wall's top." Still scoped to `wasGrounded` only,
so the airborne-recovery-blocked-from-below case is untouched.

**Not yet re-verified against the real Godot build** — this session still
had no compiler access (see `Docs/CURRENT_GOAL.md`). Traced by hand against
the exact tick order and the gravity-re-add line PlayerMover.cs already
comments on, and the fix targets the specific gap between "algorithm mirror
that doesn't model the gravity nudge" and "real per-tick behavior" that
caused round 2 to falsely read as fixed. **Compiling and re-running F9's
3.1 test in the real editor is the immediate next step — don't trust this
by reasoning alone a third time**; that's exactly what went wrong last
round.

**Update: user compiled and confirmed round 3 fixed it** — full F9 pass,
`ledge reachable (x=122.82, grounded=False) yes`. The real compiler loop is
now live for this project; treat future AI-side fixes as hypotheses until
the user's next test run confirms them, same as this one was.

## Stocks, blast zones, respawn (Directive Phase 1 checklist items)

All three of Battlefield's real MeleeLight-derived data (`BlastZone`,
`RespawnPoints`) already existed in `StageGeometry`/`Stages/Battlefield.cs`
from earlier work — nothing there needed porting, just consuming. Added to
`PlayerMover`:

- `Stocks` (default `StartingStocks = 4` — competitive standard, a
  documented default, not transcribed data; real Melee stock count is a
  match-setup option, not a fixed constant).
- Blast-zone check at the very top of `Tick()`, ahead of hitlag/hitstun/
  elimination-immunity — same priority real Melee gives it (you can die out
  of hitstun, mid-attack, mid-hitlag). Checked against the position the
  fighter actually ended LAST tick, so it can't miss a frame.
- On death with stocks remaining: full reset (position to that player's
  `RespawnPoints[playerIndex]`, velocity zero, percent zero, facing from the
  respawn point's data, all attack/hitstun/hitlag state cleared) plus
  `RespawnInvincibilityFrames` (120 ticks / ~2s, a documented placeholder —
  real Melee's invincibility is tied to the star-platform hold/steer
  sequence, which this does NOT model; this engine drops the fighter with
  full normal airborne control immediately, just briefly unhittable).
  `CombatSystem.ResolveAttack` now skips a hit entirely against an
  invincible defender.
- On death at 0 stocks: `IsEliminated = true`, terminal — `Tick()` returns
  immediately every future tick (no physics, no input, not hittable since
  `CurrentMove` never gets set again).
- Two new `PlayerActionState` values (`Respawning`, `Eliminated`) so this is
  externally visible the same way every other state already is.
- All new fields added to `SaveState`/`LoadState` (rollback/hash safety —
  `ComputeStateHash` derives from `SaveState`, so skipping this would have
  made stock/respawn state invisible to determinism checks even though it's
  real gameplay state).
- New F9 test: `BlastZoneStockRespawnTest` — forces a fighter past the
  blast zone `StartingStocks` times in a row (position set directly, not
  driven by falling — only the reaction is under test), asserting each
  stock decrements, invincibility engages, respawn lands back in bounds,
  the last death eliminates, an eliminated fighter is provably frozen
  (forced back into the blast zone again and confirmed nothing changes),
  and twin-run hash equality through the whole sequence.
- `Main.cs`'s debug HUD now prints `stocks:` alongside percent for both
  fighters.

**Not compiled/run this session** — same standing caveat as everything
else written without the real Godot editor; see `Docs/CURRENT_GOAL.md`.
This is genuinely new logic (not a rewiring like the FoxCharacter swap), so
it deserves more suspicion, not less, until the next real test run.

**What's explicitly NOT done:** camera (still Not Started — the last
Directive Phase 1 checklist item); no visual "star KO" or spawn-platform
animation (Phase 8, not started); no match-end/game-over state once one
fighter is eliminated (both movers just keep ticking — CombatSystem still
runs, the eliminated one just can't be hit or act); stock count isn't
configurable from anywhere external yet (compile-time constant only).

## Master Directive v3, Phase 2 — Fox's moveset ported complete

**Read `Docs/CURRENT_GOAL.md` first** — it carries the single current objective
and the full session handoff. This entry is the historical log.

Fox now has every move MeleeLight gives him: 24 MoveDefs, 96 hitboxes.

**Both previously-documented blockers closed.**
- *Set-knockback.* `KnockbackMath` gained the `sk != 0` branch from
  `hitDetection.js:getKnockback`. This was the sole reason Down Air could not be
  represented (every dair hitbox uses sk=30). The branch is percent-independent
  by design: 43.60 knockback at 0% AND at 150%, versus 19.20 -> 79.20 for the
  normal formula. Also unblocks upair1 (sk 30), pummel (30), the reflector's
  real hit (80) and both ledge attacks (90).
- *Projectiles.* New `Gameplay/Projectile.cs`: a fixed-capacity pool rather than
  dynamic `SimWorld.Register`/`Unregister`. Deliberate — dynamic spawn/despawn
  makes the sim's object ordering depend on history, which is exactly what
  desyncs rollback later. Fixed slots also keep the snapshot a constant size.

**Multi-hitbox is real.** `MoveDef` previously carried ONE hit (its own comment
called that a scope cut). New `Characters/HitboxSpec.cs` gives each move its
true array, with a `RefreshPeriod` for boxes that re-arm. Verified hit counts:
DownAir 7 (every 3 frames over 5..25), UpTilt 1 (four simultaneous boxes are one
hit event), ForwardAir 5, Jab3 5, and two hits each for UpSmash/Nair/Bair/FSmash.

**Data was extracted programmatically**, not hand-typed — `setHitBoxes` joined
against `setOffsets` (including the late `.push()` appends). At 97 hitboxes,
hand-transcription reliably introduces errors that later look like gameplay bugs
rather than typos. Frame windows were the exception; they live as `timer`
comparisons inside each move's own JS and were read individually.

**New moves:** DownAir, Jab2, Jab3, Grab, Pummel, GetUpAttack,
LedgeAttackQuick, LedgeAttackSlow, all four throws, Blaster.

**Two bugs caught in my own work before packaging.** (a) Keying "already hit" on
the hitbox SLOT INDEX let a two-box move alternate boxes and hit every frame —
the drill would have dealt 21 hits instead of 7. MeleeLight tracks `hitList` per
hitbox OBJECT; the key is now `(FirstActiveFrame << 16) | HitGroup`. (b) Only
the first live box was ever spatially tested, so up-tilt's other three boxes
could never connect. `CombatSystem` now walks every slot and takes the first
that actually overlaps.

**Deliberate non-implementations.** `CombatSystem` skips hitbox types 2 (grab),
7 (reflect) and 8 (inert): applying a grab box as a normal hit would make Fox's
grab a 0-damage knockback move that launches and causes hitlag, which is worse
than it not connecting. Data is complete; the state machines are not written.

**Fox's laser has zero knockback and that is correct** — `isFox ? 0 : 100` in
`articles.LASER`. It damages without flinch, which is the defining difference
from Falco's laser. Documented emphatically in `ProjectileSpec.FoxLaser` so a
future session does not "fix" it.

**Verification:** brace/paren balance, duplicate-type scan, symbol existence,
save/load parity (37/37), and an algorithm mirror run against the real generated
data confirming every hit count above. Still no .NET SDK here — **NOT COMPILED**.
New F9 test `FoxMovesetTest` covers slot completeness, hitbox count,
set-knockback percent-independence, drill multi-hit, laser damage-without-flinch,
and twin-run determinism including projectiles.

**Known consequence:** `CombatSystem.SaveState` changed from empty to writing 16
projectile slots, so any test asserting a LITERAL state hash will differ.
Twin-run comparisons are unaffected.
