using System;
using System.Collections.Generic;

namespace PlatformFighter.Core.Sim;

/// <summary>
/// Type-id → factory registry so RestoreSnapshot can rebuild objects that
/// no longer exist live (rollback will despawn/respawn retroactively).
/// Dictionary is safe here: keyed lookup only, never iterated in sim code.
/// Type ids are part of the snapshot format — never renumber a shipped id.
/// </summary>
public static class SimObjectTypes
{
    private static readonly Dictionary<int, Func<ISimObject>> Factories = new();

    /// <summary>Idempotent — re-registering the same id just replaces the factory.</summary>
    public static void Register(int typeId, Func<ISimObject> factory)
        => Factories[typeId] = factory;

    public static ISimObject Create(int typeId)
        => Factories.TryGetValue(typeId, out var f)
            ? f()
            : throw new InvalidOperationException($"No factory registered for sim type id {typeId}");
}
