# Roadmap (canonical numbering)

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
| 10 | First fighter implementation  | 🛠 In progress (hybrid self-play demo playable) |
| 11 | Second fighter implementation | Not started |
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
