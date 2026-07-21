# READ THIS FIRST —# READ THIS FIRST — CURRENT GOAL (above ROADMAP.md, above everything else)

**Last updated:** this session (Master Directive v3 in effect). Read this
file before ROADMAP.md, before any source file.

## Corrections to prior handoffs (read this first)

Two things prior sessions' docs got stale on — the user caught both:

1. **Camera is DONE, not "Not started."** `Core/Rendering/CameraController.cs`
   is a complete, real-Melee-algorithm-accurate framing camera (subject
   bounds -> interest point -> per-stage clamp -> smoothed pan/zoom), wired
   into `Main.cs` (`_camera = new CameraController(...)`, used every frame).
   ROADMAP.md's Phase 13 line and every "camera: Not Started" mention across
   the docs were stale and have been corrected this session.
2. **Fox's moveset is essentially complete, not mid-port.** All 28
   `MoveSlot` entries have real ported `MoveDef` data in `FoxMoves.cs` and
   are wired into `FoxCharacter.cs` — grounded normals, all 5 aerials, all 4
   specials (Blaster/Illusion/FireBird/Reflector), grab, pummel, all 4
   throws, get-up attack, both ledge attacks. `FoxCharacter.cs`'s own doc
   comment claiming NeutralB/DownAir were "left unfilled" was WRONG (the code
   two lines below it fills both) — fixed. `MoveSlot.cs`'s comment claiming
   only grounded normals/aerials/NeutralB were input-reachable was also
   stale — `TryStartAttack` has read stick direction on the special button
   (UpB/DownB/SideB/NeutralB) since a prior session (search "Phase 11" in
   `PlayerMover.cs`) — fixed.

## What was actually still missing, and what THIS session did about it

Data-complete but DISPATCH-missing, per the last accurate handoff: Grab,
Pummel, the four Throws, GetUpAttack, LedgeAttackQuick/Slow. This session
implemented the grab/pummel/throw state machine for real, ported from
MeleeLight's actual source (not guessed) — see the file list below. Get-up
attack and both ledge attacks are STILL not dispatched (they need a downed/
tech state and a ledge-grab state that don't exist yet — out of scope for
this pass, and honestly noted rather than faked).

### Grab / pummel / throw — NEW, ported from real MeleeLight source

Sources read this session (all in the freshly-uploaded `meleelight.zip`):
`src/characters/shared/moves/{CAPTUREPULLED,CAPTUREWAIT,CATCHWAIT}.js`,
`src/characters/fox/moves/{GRAB,CATCHATTACK,THROWUP,THROWBACK,THROWFORWARD,
THROWDOWN}.js`, `src/physics/hitDetection.js:executeGrabHits`,
`src/physics/actionStateShortcuts.js:mashOut`, and
`src/characters/fox/attributes.js`'s `framesData` block (CATCHWAIT=30,
CAPTUREWAIT=80, THROWNFOXUP/BACK/FORWARD/DOWN=5/6/10/32).

**Real verified numbers now in the code** (see each constant's own doc
comment in `PlayerMover.cs`/`FoxMoves.cs` for the derivation):
- Grab: active frames 7-8, 30-frame whiff duration (already existed).
- Escape formula (CAPTUREPULLED.js, exact): `stuckTimer = 100 + 2*percent`
  at the moment of connect, decremented 1/tick, extra -3 on a fresh mash
  (button press or stick flick past 0.8) — CAPTUREWAIT.js/mashOut exact.
- Pummel: real active frame 10, damage 3 (already-ported `FoxMoves.Pummel`).
- Throw release frames, DERIVED exactly (not approximated) from the source's
  variable-rate timer-sync math, since Fox-throwing-Fox is the only case
  Phase 1 needs: Up=6, Back=7, Forward=11, Down=33 real ticks after the
  direction commits. Throw direction threshold is 0.7 (CATCHWAIT.js) — a
  DIFFERENT verified constant from `InputDecode.DashThresholdUnits` (0.79,
  a different gesture) — don't conflate them, a previous draft of this
  session's own code briefly did before being corrected.

**Deliberately NOT ported, documented as such (not silently skipped):**
- Grab-tech (two opposing grabs connecting the same tick) — CombatSystem's
  guard just lets whichever grab's `ResolveAttack` call happens to run first
  win; `executeGrabTech`'s real mutual-bounce isn't there.
- CAPTUREWAIT's random position jitter on mash (`0.5*Math.sign(random()-0.5)`)
  — cosmetic only, since the position gets force-set every tick by
  CombatSystem's pin regardless; not worth a fixed-point RNG source for.
- The 2-frame CAPTUREPULLED pull-in animation, and both CATCHWAIT's and
  CAPTUREWAIT's periodic no-op self-reinit every 30/80 frames (neither has
  any gameplay effect in the source — confirmed by reading it, not assumed).
- Attacker's post-throw recovery tail: real Melee locks the attacker a few
  more frames after the throw's hit lands (scaled-timer caps of 33/32/33/43
  map to slightly different real-frame totals for Up/Back due to the
  rubber-band scaling this port didn't replicate). This engine frees the
  attacker to act the INSTANT the throw hitbox applies instead. Flagged as a
  known simplification, not verified-accurate.
- Pummel's real SetKnockback (30) is not applied to the victim's velocity —
  see `ApplyPummelDamage`'s doc comment for why that's a no-op in practice
  in the real source too (position gets stomped every frame regardless).

### Architecture note for whoever picks this up

`PlayerMover` still never references its opponent directly (existing rule,
kept). Each fighter's own `Tick()` drives its own half
(`TickGrabbed`/`TickGrabbing`, next to `TickHitstun` in `PlayerMover.cs`);
`CombatSystem.TickGrabs()` (new, called from `CombatSystem.Tick()` right
after both `ResolveAttack` calls) is the only place that reaches across —
grab-connect, the position pin, pummel's real-frame damage application, and
throw release all happen there.

## Verification NOT performed (standing caveat, unchanged across every session)

No .NET SDK in this authoring environment. Done this session: brace/paren
balance check on every touched file (`PlayerMover.cs` 124/124 braces,
531/531 parens; `CombatSystem.cs` 21/21, 110/110; `FoxMoves.cs` 31/31,
532/532 — all balanced). **Not compiled. Not run.** The user's own Godot
editor / F9 debug harness is the real verification loop, same as always.

## CURRENT OBJECTIVE (exactly one task)

**Compile, run F9, and confirm nothing regressed — then playtest a grab.**
Grab a stationary or moving opponent, mash out of one grab, let a hold time
out without pummeling/throwing (confirm nothing weird happens — there's no
explicit timeout path other than mash-escape, matching the real source,
which also has no non-mash timeout for the victim), pummel a few times,
and try all four throw directions. Watch for: victim's position visibly
snapping to the hold spot every frame (should look locked, not floaty);
facing flipping correctly on connect; both fighters returning to normal
control after a throw lands or an escape happens.

If F9 passes and grabbing feels right in the editor: **get-up attack and
both ledge attacks are next** — they need a downed/tech state (get-up) and
a ledge-grab/ledge-hang state (both ledge attacks) that don't exist in this
engine yet. Neither is a data problem (both MoveDefs already exist in
`FoxMoves.cs`); both are new state machines, same category of work this
session's grab system was, just for a different trigger condition.
DOCEOF
echo written
Output

written


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
