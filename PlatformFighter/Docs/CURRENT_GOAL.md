# READ THIS FIRST — CURRENT GOAL (above ROADMAP.md, above everything else)

**Last updated:** this session. If you are a fresh AI picking this project
up, read this file before ROADMAP.md, before any source file.

## The directive that governs everything right now

**Master Directive v3** supersedes v2 (v2's own text is still preserved
below for history, but treat v3 as current). Full original text is in
`Master_directive.txt` at the project root of whatever upload included it —
reproduce/consult that file directly if it's present; the essential points:

- Not an original platform fighter. A faithful port of **MeleeLight's**
  engine into Godot. Do not invent values, simplify systems, or redesign
  systems — whenever MeleeLight has a working implementation, PORT IT.
- **Standing rule:** never invent gameplay constants. Verify frame data,
  knockback, landing lag, hitbox size, etc. from the community (FightCore,
  MeleeFrameData, SmashWiki) or from MeleeLight's/real Melee's own source.
  Only use inferred values when nothing authoritative exists, and mark them
  clearly as hypotheses.
- **FightCore/MeleeFrameData are now the standing tiebreaker** whenever two
  authoritative sources disagree and MeleeLight itself doesn't settle it —
  explicit user instruction from the BackAir landing-lag resolution (see
  below). Don't stop-and-ask every single time two sources differ by a
  frame or two; use this tiebreaker and keep moving, UNLESS the disagreement
  is large/structural (not just "which of two close numbers").
- **Current sole objective (v3, narrower than v2): finish Fox.** Phase 1 of
  v3 is Fox vs. Fox on Battlefield; Phase 2 is "port Fox completely" — every
  animation, action state, hitbox, hurtbox, attribute, interrupt, landing
  behavior, cancel, velocity, friction, traction, jump, aerial, special.
  Hybrid Fox/Falco, multiple stages, AI, menus, online, polish: forget all
  of it until Fox is genuinely done.
- Rule 3: when blocked, STOP, EXPLAIN, ASK. Never guess.
- If a session is about to run out of context: stop with room to spare,
  repackage the zip, update this file + ROADMAP.md with exactly one
  CURRENT OBJECTIVE, so a fresh AI can pick up in minutes.

## What actually happened this session (important — the last handoff was wrong)

The user handed off a zip believed to include a completed session's work
(BackAir landing-lag resolution + real Illusion/Side-B decomp data), plus a
pasted transcript of that session as evidence. **The zip did NOT actually
contain that work** — `Core/Combat/Hitbox.cs` still had the exact leftover
shell heredoc artifact (`EOF`/`echo done`/etc.) the transcript claimed was
already stripped, `Characters/Fox/FoxMoves.cs` had no `BackAir` MoveDef at
all (not even the earlier undispatched version), and the stray
`PlatformFighter.csproj.old`/`.old.1` files the transcript said were removed
were still present. The session's actual file edits never made it into the
zip that got packaged — almost certainly the mid-session crash the user
warned about. **Lesson for future sessions: don't trust a transcript's
claimed edits without verifying the actual file state first** — this cost
real time re-diagnosing something already correctly diagnosed once.

Re-applied from the transcript, now genuinely in the code (verified by
reading each file after editing, not just trusting the diff):

1. **`Hitbox.cs`** — heredoc artifact removed for real. Scanned the whole
   repo (`grep -rln "^EOF$"`) for the same pattern elsewhere — none found.
2. **Stray `.csproj.old`/`.csproj.old.1`** — deleted for real.
3. **`FoxMoves.cs` — `BackAir` added and wired into `Aerials`**, with real
   decomp data (attributes.js/ATTACKAIRB.js: frames 4-7 active, offset
   (-0.02, 8.00) Y-negated, radius 3.660, damage 15, angle 361/Sakurai, base
   knockback 10, growth 100) and `landingLagFrames: 20` (10 L-cancelled) —
   the user's explicit resolution of the FightCore(20)-vs-SmashWiki(15)
   disagreement, with FightCore as the going-forward tiebreaker per v3.
4. **`FoxMoves.cs` — `Illusion` (Side-B) hit data replaced with real decomp
   data**, superseding the old SmashWiki-description approximation. Source:
   `meleelight/src/physics/article.js`'s `ILLUSION` article definition
   (NOT `attributes.js` — a different subsystem, the traveling-dash-entity
   object, which is why the earlier `setHitBoxes` search came up empty) plus
   `main/util/createHitBox.js`'s real parameter order
   `(offset, size, dmg, angle, kg, bk, sk, type, clank, hG, hA)`. Final real
   values: damage 7, angle 80, knockback base 68, knockback growth 40
   (Fox's ground-only override applies), radius 4.160, offset (0,0) — real
   spatial data now, not the flat-reach-box fallback. Full derivation,
   including the `options.isFox || true` bug discovered in MeleeLight
   itself (doesn't affect Fox's own result, but matters for a future Falco
   pass), is in `Illusion`'s own doc comment in `FoxMoves.cs` — don't
   re-derive this, it's already worked out there.
5. `FoxCharacter.cs`'s stale comment (claiming BackAir was unfilled) fixed
   to match — no functional change needed there, it already picks up new
   `FoxMoves.Aerials` entries automatically via its `foreach` loop.

**Confirmed still correct, unaffected by the crash** (this predates the
crashed session): `PlayerMover.cs`'s default character and `Main.cs`'s
instantiation both already point at `FoxCharacter.Instance`, not the
hybrid. No `Characters/Falco` references anywhere in `FoxCharacter.cs`.

**Verification performed:** brace-balance check on every touched file
(`Hitbox.cs`, `FoxMoves.cs`, `FoxCharacter.cs`) — all balanced. Still no
compiler in this environment — same standing caveat as every prior session;
the user's own Godot editor is the real verification loop.

## Fixed this session: the one real F9 failure (stale test spawn distance)

User compiled and ran F9 for the first time against this session's fixes.
Everything passed except `HybridSelfPlayCombatTest`: hashes matched (still
deterministic) but P2 took 0 damage from P1's Jab1 by frame 300 — a genuine
gameplay-assertion failure, not flakiness.

**Root cause, confirmed by hand-computing the exact geometry, not guessed:**
`DeterminismTest.cs` spawned P1/P2 ±6 apart (12 total) — a constant that
predates real per-move spatial hitbox data. At that distance, Jab1's real
ported hitbox (offsetX 5.49, radius 3.328) reaches to absolute x=2.818
against P2's real hurtbox (ECB halfWidth 3) starting at x=3 — a fixed
0.182-unit gap that can NEVER connect (the X distance alone, 3.51, already
exceeds the 3.328 radius, independent of Y). This has nothing to do with
`FoxMoves.cs`'s data or `CombatSystem`'s hit check — both are correct; the
test's own spawn constant was stale, same category of issue Step 2b's own
note already called out ("the spawn constants move," not the real
transcribed values). Fixed: spawn ±4 apart instead, which lands the real
hitbox center INSIDE the real hurtbox span outright (comfortable margin,
not a razor's-edge fix). Full math is in the test's own updated comment.

**Follow-up — the spawn-distance fix alone did NOT fix it (user re-ran,
same failure, P2 percent=0).** Diagnosed for real this time by reading the
actual dispatch code rather than re-guessing: `TryStartAttack`'s
`attackPressed` is a RISING EDGE (`attackHeld && !_prevAttackHeld` —
identical convention to `jumpPressed`). The test held Attack continuously
for the entire run and never released it, so that edge fires exactly ONCE
total, on frame 0 — while P1 is still airborne from the y=-20 spawn (so it
likely dispatches NeutralAir, not Jab1) — and then never fires again for
the rest of the 300-frame run. Holding a button down does not repeat-press
it; this engine (correctly, matching real player input) requires a fresh
press per move. The spawn-distance fix was real and correct and is kept,
but couldn't have worked on its own while the input pattern only ever
pressed once.

**Fixed:** the test now mashes (`(frame % 4) < 2`, alternating held/
released), giving a fresh edge roughly every 4 frames — plenty of
opportunities for Jab1 (17 total frames) to fire and connect repeatedly
across 300 frames, matching how mashing a button actually generates input.

**Not yet re-verified in the real editor a third time** — same standing
caveat. This is now the very next thing to compile/run; if it still fails,
read the code before touching anything else, the same way this pass and
the spawn-distance pass both should have from the start.

## CURRENT OBJECTIVE (exactly one task)

**Compile, run F9, and report the `Fox moveset:` line.** Nothing else until that
passes. Everything below describes work that has NEVER been compiled.

---

## What happened this session: Fox's moveset is complete

Master Directive **v3** governs (Phase 2: "Port Fox completely"). Both
long-standing blockers named by the previous handoff are CLOSED.

### The two blockers, resolved

1. **Set-knockback** — `KnockbackMath.ComputeMagnitude` now has the `sk != 0`
   branch from `hitDetection.js:getKnockback`. That was the *sole* reason Down
   Air could not exist (every dair hitbox uses `sk=30`). Verified:
   set-knockback produces 43.60 at both 0% and 150%, versus the normal formula's
   19.20 -> 79.20 over the same range. Percent-independence is the whole point of
   the branch and is what makes drill a combo tool rather than a kill move.
2. **Projectiles** — `Gameplay/Projectile.cs` is new. Fox's Blaster works.

### Multi-hitbox is real (the big fidelity change)

`MoveDef` carried exactly ONE hit before; its own comment called that a
deliberate scope cut. New `Characters/HitboxSpec.cs` gives each move its true
hitbox array. Verified hit counts against the real generated data:

| move | boxes | hits | why |
|---|---|---|---|
| DownAir | 2 | **7** | re-arms every 3 frames over 5..25 (ATTACKAIRD.js) |
| UpTilt | 4 | **1** | four simultaneous boxes = ONE hit event |
| ForwardAir | 10 | **5** | fair1..fair5, sequential |
| Jab3 | 15 | **5** | five-position rapid jab |
| UpSmash / Nair / Bair / FSmash | 4-6 | **2** | two stages each |

### Data provenance

97 hitboxes across 24 moves were extracted **programmatically** from
`attributes.js` (`setHitBoxes` joined against `setOffsets`, including the late
`.push()` appends), not hand-typed. Frame windows were read individually from
each move's own JS. Parameter order is
`createHitbox(offset, size, dmg, angle, kg, bk, sk, type, clank, hG, hA)`.

### Moves that did not exist before and now do

DownAir, Jab2, Jab3, Grab, Pummel, GetUpAttack, LedgeAttackQuick,
LedgeAttackSlow, all four throws, Blaster (Neutral-B).

### Two bugs I found in my OWN work before shipping — do not reintroduce

1. **Hit key was slot-indexed.** Keying "already hit" on the hitbox slot index
   let a two-box move alternate boxes and land a hit EVERY frame — the drill
   would have dealt 21 hits instead of 7. MeleeLight tracks `hitList` per hitbox
   OBJECT, not per box. The key is now `(FirstActiveFrame << 16) | HitGroup`,
   which groups simultaneous boxes into one event while keeping sequential hits
   distinct.
2. **Only the first live box was ever spatially tested.** Up-tilt has four boxes
   at four different offsets; if box 0 missed, boxes 1-3 were never checked and
   the move whiffed at ranges it should reach. `CombatSystem` now walks every
   slot via `PlayerMover.ActiveHitboxSlots` and takes the first that OVERLAPS.

### Deliberate non-implementations (data ported, dispatch absent)

- **Grab / throws / ledge / get-up.** `CombatSystem` explicitly SKIPS hitbox
  types 2 (grab), 7 (reflect) and 8 (inert). Applying a grab box as a normal hit
  would make Fox's grab a 0-damage knockback move that launches and causes
  hitlag — worse than not connecting. The data is correct and complete; only the
  state machines are missing.
- **Reflection.** The reflector's type-7 field is carried but does nothing.
  Its type-4 DAMAGING box (5 damage, sk 80) is now wired — previously
  `ReflectorHit` carried the 0-damage reflect box, so shine did nothing at all
  on contact.

### Fox's laser has ZERO knockback — this is correct, do not "fix" it

`articles.LASER`'s hitbox is
`createHitbox(..., 3, 361, isFox ? 0 : ..., 0, isFox ? 0 : ..., ...)`. For Fox
both knockback growth and set-knockback are 0. Fox's laser deals damage with no
flinch whatsoever; that is the defining difference from Falco's and the reason
Fox can lasercamp without interrupting his own follow-ups.

### Verification actually performed

No .NET SDK in the authoring environment — standing caveat, unchanged. Done:
brace/paren balance across all files, duplicate-type scan, symbol existence
checks, save/load write/read parity (37/37 on PlayerMover), and an algorithm
mirror run against the REAL generated data confirming the hit counts in the
table above. **Not compiled. Not run.**

### Files added / changed

- NEW `Characters/HitboxSpec.cs`, `Gameplay/Projectile.cs`
- REWRITTEN `Characters/Fox/FoxMoves.cs` (generated; 24 MoveDefs, 96 hitboxes)
- `Core/Combat/Hitbox.cs` (+SetKnockback, HitType, HitsGrounded/Airborne)
- `Core/Combat/KnockbackMath.cs` (+set-knockback branch)
- `Characters/MoveData.cs` (+Hitboxes array, HasHitboxes, LastHitboxFrame)
- `Characters/MoveSlot.cs` (+Jab2, Jab3, Pummel, GetUpAttack, both ledge attacks)
- `Characters/AngleTable.cs` (+angles 78, 84, 92)
- `Characters/Fox/FoxCharacter.cs` (every slot filled)
- `Gameplay/PlayerMover.cs` (indexed hitbox API, jab combo, laser spawn request)
- `Gameplay/CombatSystem.cs` (walks all hitboxes, owns projectile pool)
- `Debug/DeterminismTest.cs` (+`FoxMovesetTest`)

### Expected first-compile trouble spots, in order of likelihood

1. `CombatSystem.SaveState` changed from empty to writing 16 projectile slots.
   Any test asserting a LITERAL hash will now differ. Twin-run comparisons are
   unaffected (they compare two live worlds).
2. `TryGetActiveHitbox` changed signature — old single-arg callers are gone, but
   check `Main.cs`'s debug HUD if it grew one.
3. `MoveDef` gained two fields; it is a `readonly struct`, so any other
   constructor call site must still compile.

### Immediate next step after F9 passes

Grab/throw state machine. Data is ready (`FoxMoves.GrabsAndSituational`); what's
needed is a grabbed state on the victim, a hold timer, and throw dispatch. After
that, ledge states unblock both ledge attacks.

## Full text of Master Directive v2 (superseded by v3, kept for history)

> PFPROJECT — MASTER DEVELOPMENT DIRECTIVE (v2)
>
> **Project Goal**
> This project is not an original platform fighter. The immediate goal is to
> faithfully recreate Melee Light's engine inside Godot while preserving
> Melee gameplay. Do not redesign systems. Do not simplify systems. Do not
> approximate Melee. Whenever Melee Light already contains a working
> implementation, that implementation is the source of truth. The objective
> is to finish with a Godot engine that behaves identically to Melee Light
> before expanding the project.
>
> **Current Development Strategy**
> We are intentionally narrowing the scope. Forget every future feature for
> now. Forget custom characters. Forget hybrid Fox/Falco. Forget multiple
> stages. Forget AI. Forget menus. Forget online. Forget polish. Build one
> thing correctly.
>
> **Phase 1** — Produce one fully functioning playable match. Requirements:
> Battlefield only; Fox only; Two Foxes; No AI required; Local controls;
> Stocks; Respawning; Blast zones; Camera; Full Melee movement; Full Melee
> combat; Full Melee hitboxes; Full Melee knockback. Nothing else matters
> until this works.
>
> **Phase 2** — Port Fox completely: every animation, state, action, hitbox,
> interrupt, landing behavior, cancel, attribute, velocity, friction value,
> traction value, jump, aerial, special. Everything from Melee Light. Do not
> invent values.
>
> **Phase 3** — Battlefield: port exactly (collision, ledges, blast zones,
> platforms, camera, spawn points). Gameplay first, graphics later.
>
> **Phase 4** — Gameplay Parity, full movement list (walking, initial dash,
> run, dash dance, pivot, turnaround, crouch, crawl if applicable, jump
> squat, full hop, short hop, double jump, fast fall, wavedash, waveland,
> L-cancel, shield, spot dodge, roll, air dodge, grab, throws, hitstun,
> hitlag, knockback, DI, SDI, ASDI, teching, ledge mechanics). Do not skip
> systems.
>
> **Repository usage:** the repo has both the Godot project and the Melee
> Light source — use both. Read Melee Light → understand implementation →
> recreate inside Godot. Do not reverse-engineer behavior from memory.
>
> **Coordinate system:** gameplay stays in native Melee/Melee Light units;
> rendering scales independently; do not scale gameplay to match graphics,
> rendering adapts to gameplay.
>
> One additional standing rule the user has given every session: **if a
> session is about to run out of usable context/data before finishing the
> current task, stop, finish only what's safely completable without running
> out mid-edit, repackage the entire Godot project as a zip, and update this
> file + ROADMAP.md so a completely fresh AI can read them and know exactly
> where things stand — without burning extra resources getting re-oriented.**
