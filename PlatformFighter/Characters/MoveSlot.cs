namespace PlatformFighter.Characters;

/// <summary>
/// Phase 9: every attack slot a CharacterData's moveset table can be keyed
/// by. Deliberately covers more slots than PlayerMover.TryStartAttack can
/// currently reach (Grab, the four throws, SideB, UpB, DownB) — see that
/// method's doc comment for exactly which ones are wired to input today
/// (grounded normals, the four aerials, and NeutralB only). Having the
/// slot — and MoveDef data behind it, where extracted — costs nothing and
/// means the next phase that adds grab/throw/directional-special dispatch
/// isn't also blocked on transcribing frame data from scratch.
/// </summary>
public enum MoveSlot
{
    Jab1,
    ForwardTilt,
    UpTilt,
    DownTilt,
    ForwardSmash,
    UpSmash,
    DownSmash,
    DashAttack,

    NeutralAir,
    ForwardAir,
    BackAir,
    UpAir,
    DownAir,

    NeutralB,
    SideB,
    UpB,
    DownB,

    Grab,
    ThrowForward,
    ThrowBack,
    ThrowUp,
    ThrowDown,
}
