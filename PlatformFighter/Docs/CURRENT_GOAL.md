# READ THIS FIRST — CURRENT GOAL (above everything else in Docs/ROADMAP.md)

**Last updated:** this session, immediately before a full project zip was
repackaged and handed off. If you are a fresh AI picking this project up,
read this file before ROADMAP.md, before any source file, before anything
else. This file states what to do next. ROADMAP.md is the historical log of
how we got here — useful for context, not for direction.

## The directive that governs everything right now

The user issued a **Master Directive v2** that narrows scope hard. It is
reproduced in full at the bottom of this file so nothing gets lost in
paraphrase. The short version:

- This is **not** an original platform fighter. It is a faithful port of
  **MeleeLight's** engine into Godot, preserving real Melee gameplay.
- **Do not invent values. Do not simplify systems. Do not redesign systems.**
  Whenever MeleeLight already has a working implementation, that
  implementation is the source of truth — read it, understand it, port it.
  Do not reverse-engineer behavior from memory of Melee.
- Scope is narrowed to ONE deliverable: **a fully playable Fox vs. Fox match
  on Battlefield**, with real Melee-accurate movement, combat, physics,
  hitboxes, knockback, ledges, camera, stocks, respawn, and blast zones.
- **Forget, for now:** custom characters, the Fox/Falco hybrid, multiple
  stages, AI, menus, online, polish. All of that comes after Fox-vs-Fox-on-
  Battlefield is real and verified, not before.

## What this session actually did

The codebase had drifted from this directive — the milestone character in
`Main.cs`/`PlayerMover.cs` was `Characters/Hybrid/FoxFalcoHybrid.cs` (Fox
physics + a mix of Fox and Falco moves), which the directive explicitly says
to forget for now ("Forget hybrid Fox/Falco").

Fixed this session:
- Added `Characters/Fox/FoxCharacter.cs` — a pure-Fox `CharacterData` built
  entirely from `FoxMoves.cs` (grounded normals, aerials, Up-B/Side-B/Down-B).
  **Zero references to `Characters/Falco/` anywhere in it.**
- `Gameplay/PlayerMover.cs`'s default character (used when `Main.cs`
  constructs a `PlayerMover` without an explicit `CharacterData`) now points
  at `FoxCharacter.Instance` instead of `FoxFalcoHybrid.Instance`.
- `Main.cs` updated to match (import, header comment, the ECB-sizing
  reference used for the placeholder-box visuals).
- `Characters/Hybrid/FoxFalcoHybrid.cs` was **not deleted** — it's real,
  previously-verified work (Phase 11 milestone) and the directive's own
  "expand after the foundation is solid" plan will want it later. It's just
  no longer what gets spawned by default. Everything that referenced it only
  in a doc-comment (not functionally) was left alone.

**This was NOT run through the Godot/dotnet compiler this session** — no
network/SDK access in this environment (see the "known limitation" note
below). Read the diff yourself before trusting it compiles; it's a small,
mechanical swap (two `using` changes, one field default, one instantiation
site), not new logic, but "small" is not "verified." **Compiling and running
Phase 1's actual playtest is the very next thing to do.**

## What's genuinely missing for Master Directive v2 Phase 1 (a full match)

Cross-referencing the directive's Phase 1 checklist against the current
engine (see ROADMAP.md's phase table for the full history):

| Directive Phase 1 requirement | Status |
|---|---|
| Battlefield only | ✅ real geometry ported (`Stages/Battlefield`), collision-verified |
| Fox only, two Foxes | ✅ done, real-compiler-confirmed |
| Full Melee movement | 🛠 walk/dash/run/dash-dance/turn/jump/fast-fall real (MeleeLight-transcribed, real-compiler-confirmed incl. the ledge-wall fix); crouch, wavedash, waveland, L-cancel, teching, ledge-grab mechanics **not started** |
| Full Melee combat | 🛠 real knockback formula, hitlag, hitstun exist; **no per-hitbox spatial placement** (still whole-body-AABB hit detection), no multi-hit moves, no DI/SDI/ASDI, no shield/spot-dodge/roll/air-dodge/grab/throws |
| Full Melee hitboxes | ❌ not real yet — biggest accuracy gap, called out repeatedly in ROADMAP.md |
| Full Melee knockback | ✅ real Melee formula (`Core/Combat/KnockbackMath.cs`), cross-validated against MeleeLight |
| Stocks | 🛠 implemented this session (`PlayerMover.Stocks`, default 4) — **NOT yet compiled/run**, see ROADMAP.md's "Stocks, blast zones, respawn" entry |
| Respawning | 🛠 implemented this session (teleport to `StageGeometry.RespawnPoints`, invincibility window) — **NOT yet compiled/run** |
| Blast zones | 🛠 implemented this session (`StageGeometry.IsPastBlastZone`, already-existing data now actually consumed) — **NOT yet compiled/run** |
| Camera | ❌ not started (Phase 13 in ROADMAP.md's table) — last unstarted Phase 1 checklist item |
| No AI required | ✅ n/a, both slots are player input |
| Local controls | ✅ done (`Core/Input`) |

**Immediate next step: compile and run F9 in the real Godot editor and
report back** — stocks/blast-zone/respawn is genuinely new logic (not a
rewiring), written without compiler access, so it needs the same
"hypothesis until confirmed" treatment the ledge-wall fix needed twice
before it actually stuck. New test is `BlastZoneStockRespawnTest`, in the
same F9 suite as everything else.

After that's confirmed: **camera is the last unimplemented Directive Phase
1 checklist item.** Once it's in, Phase 1 as literally specified is
complete and Phase 2/4's remaining movement-parity gaps (wavedash,
L-cancel, shield, grab, DI/SDI, teching, ledge mechanics) become the
priority, alongside real per-hitbox spatial placement (still the single
biggest combat-accuracy gap).

## Known environment limitation

This container has **no network access for `dotnet`/Godot**, so nothing
written here has ever been compiled by the AI itself — only reasoned
through against the C# source and MeleeLight's real `src/` directly. The
user DOES have a working Godot editor and has been compiling/running the
handed-off zips themselves, including the F9 determinism-test suite
(`Debug/DeterminismTest.cs`) — that's the real verification loop. Treat
"traced by hand, not compiled here" as the honest status of every change
in a given session until the user reports the next test run's output.
**A fix that reads correct in isolation already broke once this way** — see
ROADMAP.md's "Step 3 fix pass, round 2 vs round 3" — because a prior
session's hand-traced/algorithm-mirror check didn't model a per-tick detail
(gravity being re-added while grounded) that only showed up in the real
compiled build. Don't repeat that: when in doubt about a fix, say so
plainly and ask for the next real test run's output rather than asserting
it's fixed.

## Rules that apply to whoever works on this next (from Master Directive v2)

1. Never rewrite a system because it "could be cleaner" — if MeleeLight
   already solved it, port it.
2. Every feature must compile before the next feature begins. Never stack
   unknown bugs.
3. When blocked: stop, explain, ask. Do not guess.
4. Never produce placeholder gameplay — everything moves toward final
   parity. (Note: some placeholders already in the codebase predate this
   directive and are explicitly flagged as such in ROADMAP.md/COMBAT.md —
   that's honest bookkeeping, not new placeholder work. Don't add more.)
5. One completed system beats five partially-working ones.

---

## Full text of Master Directive v2 (as given by the user)

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
> **Development Rules:** (1) never rewrite a system because it "could be
> cleaner" — if Melee Light solved it, port it; (2) every feature must
> compile before the next begins, never stack unknown bugs; (3) when
> blocked, stop, explain, ask, do not guess; (4) never produce placeholder
> gameplay, everything moves toward final parity; (5) one completed system
> beats five partial ones.
>
> **Repository usage:** the repo has both the Godot project and the Melee
> Light source — use both. Read Melee Light → understand implementation →
> recreate inside Godot. Do not reverse-engineer behavior from memory.
>
> **Coordinate system:** gameplay stays in native Melee/Melee Light units;
> rendering scales independently; do not scale gameplay to match graphics,
> rendering adapts to gameplay.
>
> **Verification policy:** after every significant milestone — compile, run,
> test, verify. Only then continue.
>
> **Parallel development:** another engineer (ChatGPT) is building the Stage
> Framework in parallel (architecture, spec, importer, editor, moving
> platforms, validation tools, asset pipeline, gameplay/presentation
> separation) — this work should integrate cleanly with that.
>
> **Immediate priority order:** 1) stable gameplay foundation, 2)
> Battlefield, 3) Fox, 4) movement parity, 5) combat parity, 6) camera, 7)
> stocks, 8) respawn, 9) stage framework integration, 10) expansion.
>
> **What success looks like:** launch the game and play Fox vs. Fox on
> Battlefield with Melee-accurate movement, combat, physics, hitboxes,
> knockback, ledges, camera, and deterministic gameplay. Only after that
> foundation is complete: additional characters, stages, AI, menus, visual
> enhancements.
>
> One additional standing rule the user has given every session, not just
> this one: **if a session is about to run out of usable context/data before
> finishing the current task, stop, finish only what's safely completable
> without running out mid-edit, repackage the entire Godot project as a zip,
> and update this file + ROADMAP.md so a completely fresh AI can read them
> and know exactly where things stand and what to do next — without burning
> extra resources getting re-oriented.**
