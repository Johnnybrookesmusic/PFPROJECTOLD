using System.Collections.Generic;

namespace PlatformFighter.Characters;

/// <summary>
/// Phase 9: everything PlayerMover needs to act as a specific fighter —
/// physics constants plus a MoveSlot → MoveDef table. Deliberately a thin
/// wrapper (no behavior beyond the lookup) so it stays a plain, comparable
/// bundle of sim data, consistent with ISimObject's ban on anything but
/// Fx/int/enum-shaped state — PlayerMover holds a `readonly CharacterData`
/// reference, not owned/mutated sim state itself, the same way it already
/// treats FoxAttributes-derived constants.
///
/// Dictionary lookup here is safe despite Core/Sim's usual "no dictionary
/// iteration order dependence" rule (see ISimObject.cs) — this is never
/// iterated, only keyed-looked-up by a MoveSlot PlayerMover already decided
/// deterministically (via TryStartAttack's input-driven branch), and the
/// table itself is built once at class-load and never mutated afterward.
/// </summary>
public sealed class CharacterData
{
    public readonly CharacterPhysics Physics;
    private readonly Dictionary<MoveSlot, MoveDef> _moveset;

    public CharacterData(CharacterPhysics physics, Dictionary<MoveSlot, MoveDef> moveset)
    {
        Physics = physics;
        _moveset = moveset;
    }

    public bool TryGetMove(MoveSlot slot, out MoveDef move) => _moveset.TryGetValue(slot, out move);
}
