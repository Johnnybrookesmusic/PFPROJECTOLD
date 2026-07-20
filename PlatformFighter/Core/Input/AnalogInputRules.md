# Analog → Digital Input Rules

Numbers enforced by Core/Input/InputDecode.cs. Stick axes are -100..100
("controller units"); triggers are 0..255.

## Deadzones
- Main stick: radial, 23 units. Below this radius reads as (0,0) — a
  circle, not a per-axis box, so diagonal drift can't sneak through.
- C-stick: same, 23 units.
- Triggers: linear, ~5% (13/255). Below this reads as fully released.

## Dash
A dash initiates on the frame the stick crosses ±79 units AWAY from
neutral-or-opposite, while grounded. "Away from neutral-or-opposite" means:
the previous frame was inside the deadzone, OR pointed the other direction.
Holding past 79 while already dashing that direction does not re-trigger.

## Tilt vs. Smash
Not a position check — a TIMING check. From the frame the stick leaves the
23-unit deadzone, if it reaches 64+ units within 2 frames, it's a Smash;
if it takes longer, it's a Tilt. This needs 2-3 frames of history per
player, hence SmashTiltClassifier is stateful rather than a pure function
of one FrameInput.

## C-Stick
Always resolves to a Smash-equivalent input (grounded smash attack, or
aerial attack, depending on airborne state) — it has no timing window and
no tilt mode. This is a deliberate simplification, not a limitation: it
matches the "instant smash" behavior in reference material for a stick
without spring-loaded travel.

## Shield / Trigger Press
- LAnalog/RAnalog >= 225 (or the LDigital/RDigital button bit) = hard
  press: full shield, "hard shield" reactions.
- Below 225 but above the trigger deadzone: light shield — same shield
  hitbox, reduced shield-stun benefit, more mobility options while held.
  Exact mobility rules land in Gameplay/Shield.md, not here — this file
  only owns the raw analog→state threshold.

## Still open (do not guess ahead of these phases)
- Pivot / dash-dance frame windows: Phase 5 (Movement physics).
- Ledge-specific input rules (wavedash-onto-ledge, etc.): Phase 5 + Ledge.md.
