using System.Collections.Generic;

namespace PlatformFighter.Characters.Fox;

/// <summary>
/// Master Directive v2 (see project root): scope narrowed to a pure Fox vs.
/// Fox match on Battlefield only. This replaces Characters/Hybrid/
/// FoxFalcoHybrid.cs as the character PlayerMover/Main.cs actually spawn.
/// The hybrid class is NOT deleted (still valid data, still Phase 11's
/// milestone, may return post-Phase-4-of-the-directive), it's just no
/// longer the default — per the directive: "Forget hybrid Fox/Falco."
///
/// Every entry here is Fox's own MoveDef, from FoxMoves.cs — nothing from
/// Characters/Falco is referenced anywhere in this file. Slots FoxMoves.cs
/// doesn't have real data for (NeutralB/laser, BackAir, DownAir — see each
/// one's own doc comment in FoxMoves.cs for exactly why) are left unfilled
/// rather than approximated or borrowed from Falco. CharacterData.TryGetMove
/// already handles a missing slot safely (PlayerMover just can't dispatch
/// that button yet) — same convention the hybrid already relied on for gaps
/// like Down Air.
/// </summary>
public static class FoxCharacter
{
    public static readonly CharacterData Instance = Build();

    private static CharacterData Build()
    {
        var moveset = new Dictionary<MoveSlot, MoveDef>(FoxMoves.GroundedNormals);
        foreach (var kv in FoxMoves.Aerials)
            moveset[kv.Key] = kv.Value;

        moveset[MoveSlot.UpB] = FoxMoves.FireBird;
        moveset[MoveSlot.SideB] = FoxMoves.Illusion;
        moveset[MoveSlot.DownB] = FoxMoves.ReflectorHit;
        // NeutralB (laser), BackAir, DownAir: no real Fox MoveDef exists yet
        // (see FoxMoves.cs) — intentionally absent, not stubbed.

        return new CharacterData(CharacterPhysics.FromFox(), moveset);
    }
}
