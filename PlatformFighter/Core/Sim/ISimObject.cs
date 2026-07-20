namespace PlatformFighter.Core.Sim;

/// <summary>
/// Anything that participates in the deterministic simulation.
///
/// CONTRACT (violating any of these will desync netplay/replays later):
///  1. Tick() must read/write ONLY sim state (Fx types, ints, enums).
///  2. No Godot node access, no wall-clock time, no System.Random,
///     no floats, no dictionary-iteration-order dependence.
///  3. Tick order across objects must itself be deterministic
///     (SimWorld guarantees this by ticking a stable, ordered list).
///  4. SaveState/LoadState must cover EVERY field Tick() reads or writes,
///     in the same order in both methods. A field you forget here is a
///     field rollback silently corrupts — the state-hash tests exist to
///     catch exactly that.
/// </summary>
public interface ISimObject
{
    /// <summary>Stable type identifier for snapshot restore. Register a
    /// matching factory in SimObjectTypes. Never renumber shipped ids.</summary>
    int TypeId { get; }

    /// <summary>
    /// Advance one tick. Read input via world.GetInput(playerIndex) — see
    /// Docs/INPUT.md. The world reference is handed in fresh each call
    /// rather than stored, since a rebuilt object (RestoreSnapshot) must
    /// behave identically to a live one and shouldn't need to remember
    /// which world it belongs to.
    /// </summary>
    void Tick(SimWorld world);

    /// <summary>Serialize all gameplay state, deterministic order.</summary>
    void SaveState(StateWriter w);

    /// <summary>Mirror of SaveState — identical field order.</summary>
    void LoadState(StateReader r);
}
