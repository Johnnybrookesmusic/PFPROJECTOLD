using System.Collections.Generic;
using PlatformFighter.Core.Math;

namespace PlatformFighter.Core.Sim.Collision;

/// <summary>
/// One-way platform: solid ONLY when a body falls onto its top surface
/// from above. Never blocks horizontal movement and never blocks passing
/// through from below — matches Battlefield-style side platforms.
/// </summary>
public readonly struct OneWayPlatform
{
    public readonly Fx Y;
    public readonly Fx XMin;
    public readonly Fx XMax;

    public OneWayPlatform(Fx y, Fx xMin, Fx xMax)
    {
        Y = y;
        XMin = xMin;
        XMax = xMax;
    }
}

/// <summary>
/// Static collision geometry for a stage: fully solid blocks (floor,
/// walls, ceiling — collide from every direction) plus one-way
/// platforms. Immutable and shared by reference; stage layouts don't
/// change at runtime in Phase 4, so this needs no SaveState/LoadState of
/// its own — every ISimObject that references one gets the SAME
/// instance back from its factory (see Debug/TestStage.cs), which is
/// deterministic by construction. Real per-stage data and destructible/
/// moving geometry are Phase 12's job.
/// </summary>
public sealed class StageGeometry
{
    public readonly List<FxAabb> Solids = new();
    public readonly List<OneWayPlatform> Platforms = new();

    public StageGeometry AddSolid(FxAabb solid)
    {
        Solids.Add(solid);
        return this;
    }

    public StageGeometry AddPlatform(OneWayPlatform platform)
    {
        Platforms.Add(platform);
        return this;
    }
}
