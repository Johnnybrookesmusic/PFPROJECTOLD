using System.Collections.Generic;
using PlatformFighter.Characters.Falco;
using PlatformFighter.Characters.Fox;

namespace PlatformFighter.Characters.Hybrid;

/// <summary>
/// The milestone character, per the Melee Lite Translation Blueprint's
/// "Falco Hybrid Layer" override list. Concretely:
///
///   - Physics: CharacterPhysics.FromFox() exclusively — weight, movement,
///     jumps, gravity are all Fox's. Falco is not blended into movement feel
///     at all; "Fox base gameplay" is read as movement ownership, not just
///     "Fox is the default."
///   - Grounded normals: Fox's base table (FoxMoves.GroundedNormals — jab,
///     tilts, smashes, dash attack), with Falco's Down Tilt/Up Tilt/Down
///     Smash layered on top per the blueprint's "Ground: Down Tilt, Up
///     Tilt, Down Smash" override line.
///   - Aerials: Fox's own base table (FoxMoves.Aerials — Nair/Fair/Uair),
///     with Falco's Back Air layered in (a slot Fox has no data for) and
///     Falco's Up Air overriding Fox's, per the blueprint's "Aerial: Back
///     Air, Up Air" line. Down Air stays unfilled — see FoxMoves.cs's own
///     note on why (needs SetKnockback support this engine doesn't have).
///   - Neutral B: Falco's Blaster, overriding Fox's own laser — the
///     blueprint's "Specials: Laser" line.
///   - Side B: Falco's Phantasm, overriding Fox's own Illusion — the
///     blueprint's "Specials: ... Side B" line, and FalcoMoves.cs's own
///     header comment states this file exists specifically to back this
///     override. KNOWN REGRESSION vs. the pre-blueprint build: Fox's
///     Illusion was a real self-propelled dash (LaunchSpeed 18.72, see
///     FoxMoves.cs); Phantasm carries a real hit but LaunchSpeed 0 (see
///     FalcoMoves.cs's own note — the movement burst wasn't ported this
///     pass), so post-swap Side-B hits but no longer moves Fox. Not
///     silently accepted: flagged here and in FalcoMoves.cs as follow-up
///     work, not a design choice. Fox's own Illusion MoveDef is untouched
///     in FoxMoves.cs (unused by the hybrid, kept for a Fox-only build —
///     same convention as FoxMoves.UpAir's note on Falco overriding it).
///   - Up B / Down B: Fox's own (FireBird, ReflectorHit) — not in the
///     blueprint's Falco override list, so they stay Fox's by default.
///   - Grab/throws: still not modeled (no MoveDef data transcribed for
///     Fox's grab/throws in FoxMoves.cs, and Z isn't dispatched by
///     PlayerMover either) — later phase.
///
/// Instance is a single shared, immutable CharacterData — safe to hand to
/// every PlayerMover (P1 self-play-vs-P2) since nothing here is mutated
/// after construction; see CharacterData's own doc comment on why the
/// backing Dictionary is safe despite Core/Sim's usual anti-dictionary rule.
/// </summary>
public static class FoxFalcoHybrid
{
    public static readonly CharacterData Instance = Build();

    private static CharacterData Build()
    {
        var moveset = new Dictionary<MoveSlot, MoveDef>(FoxMoves.GroundedNormals);
        foreach (var kv in FoxMoves.Aerials)
            moveset[kv.Key] = kv.Value;

		// Falco's ground/aerial overrides, per the blueprint's override list.
        moveset[MoveSlot.DownTilt] = FalcoMoves.DownTilt;
        moveset[MoveSlot.UpTilt] = FalcoMoves.UpTilt;
        moveset[MoveSlot.DownSmash] = FalcoMoves.DownSmash;
        moveset[MoveSlot.BackAir] = FalcoMoves.BackAir;
        moveset[MoveSlot.UpAir] = FalcoMoves.UpAir;

		// Specials: Falco's laser for Neutral B, Falco's Phantasm for Side B
        // (see class doc comment re: the lost self-launch on the Side B swap).
		// Up B / Down B stay Fox's own — not in the blueprint's override list.
        moveset[MoveSlot.NeutralB] = FalcoMoves.Blaster;
        moveset[MoveSlot.SideB] = FalcoMoves.Phantasm;
        moveset[MoveSlot.UpB] = FoxMoves.FireBird;
        moveset[MoveSlot.DownB] = FoxMoves.ReflectorHit;

        return new CharacterData(CharacterPhysics.FromFox(), moveset);
    }
}
