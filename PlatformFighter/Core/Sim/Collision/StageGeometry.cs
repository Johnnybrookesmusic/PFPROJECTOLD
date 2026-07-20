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

/// <summary>A world position paired with the facing direction (+1 = right,
/// -1 = left) a fighter should spawn/respawn with there — matches MeleeLight's
/// parallel <c>startingPoint</c>/<c>startingFace</c> (and <c>respawnPoints</c>/
/// <c>respawnFace</c>) arrays, collapsed into one struct per point instead of
/// two parallel lists that could silently drift out of index-sync.</summary>
public readonly struct FacingPoint
{
    public readonly FxVec2 Position;
    public readonly int Face;

    public FacingPoint(FxVec2 position, int face)
    {
        Position = position;
        Face = face;
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
///
/// STEP 1 ADDITION (Melee Lite Translation Directive, "Battlefield collision
/// and blast zones"): the fields below (Ground/Ceiling/WallL/WallR/BlastZone/
/// Ledges/StartingPoints/RespawnPoints) hold real per-stage data transcribed
/// from MeleeLight (see Stages/Battlefield.cs), additive to the Solids/
/// Platforms model above rather than replacing it — TestStage.cs's simple
/// flat-floor stage (used by DeterminismTest etc.) still only populates
/// Solids/Platforms and leaves these new lists empty, and nothing currently
/// reads them (see the class-level scope note on why: the real ECB collision
/// RESOLUTION algorithm these segments need is Step 2's job, not this one).
/// </summary>
public sealed class StageGeometry
{
    public readonly List<FxAabb> Solids = new();
    public readonly List<OneWayPlatform> Platforms = new();

    /// <summary>Solid ground line segments — MeleeLight's <c>stage.ground</c>.
    /// Battlefield has exactly one: the main platform's top edge.</summary>
    public readonly List<FxSegment> Ground = new();
    /// <summary>Solid ceiling line segments (collide from below) — MeleeLight's
    /// <c>stage.ceiling</c>. On Battlefield these are the underside chamfers'
    /// flat-ish caps, not a "sky" ceiling.</summary>
    public readonly List<FxSegment> Ceiling = new();
    /// <summary>Solid wall segments facing right (block leftward movement) —
    /// MeleeLight's <c>stage.wallL</c> (named for the wall's own orientation,
    /// not which side of the stage it's on — Battlefield has wallL segments on
    /// BOTH the left and right underside chambers; see Battlefield.cs).</summary>
    public readonly List<FxSegment> WallL = new();
    /// <summary>Solid wall segments facing left (block rightward movement) —
    /// MeleeLight's <c>stage.wallR</c>. Same left/right-name-vs-position note
    /// as <see cref="WallL"/> applies.</summary>
    public readonly List<FxSegment> WallR = new();

    /// <summary>Null if this stage has no blast zone (e.g. TestStage's debug
    /// floor) — <see cref="IsPastBlastZone"/> always returns false in that
    /// case rather than throwing, since a lot of existing collision-only
    /// tests construct stages with no KO concept at all.</summary>
    public FxAabb? BlastZone;

    /// <summary>Ledge grab-point positions — MeleeLight's <c>ledgePos</c>.
    /// On Battlefield these are exactly the main ground segment's own two
    /// endpoints (confirmed by comparing the source arrays), stored
    /// separately here only because MeleeLight itself authors them as a
    /// separate field rather than deriving them.</summary>
    public readonly List<FxVec2> Ledges = new();

    public readonly List<FacingPoint> StartingPoints = new();
    public readonly List<FacingPoint> RespawnPoints = new();

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

    public StageGeometry AddGround(FxSegment segment) { Ground.Add(segment); return this; }
    public StageGeometry AddCeiling(FxSegment segment) { Ceiling.Add(segment); return this; }
    public StageGeometry AddWallL(FxSegment segment) { WallL.Add(segment); return this; }
    public StageGeometry AddWallR(FxSegment segment) { WallR.Add(segment); return this; }
    public StageGeometry AddLedge(FxVec2 position) { Ledges.Add(position); return this; }
    public StageGeometry AddStartingPoint(FxVec2 position, int face) { StartingPoints.Add(new FacingPoint(position, face)); return this; }
    public StageGeometry AddRespawnPoint(FxVec2 position, int face) { RespawnPoints.Add(new FacingPoint(position, face)); return this; }
    public StageGeometry SetBlastZone(FxAabb blastZone) { BlastZone = blastZone; return this; }

    /// <summary>True once a position has crossed outside the blast zone box —
    /// MeleeLight's own KO check (<c>physics.js</c>) additionally requires
    /// upward knockback velocity ≥2.4 for the TOP edge specifically (so a
    /// slow drift that barely crosses the top line doesn't instantly KO);
    /// that velocity-aware nuance is a knockback/physics concern for whoever
    /// wires this into Step 2/4, not a geometry concern — this method is the
    /// plain "is the point outside the box at all" half of that check.</summary>
    public bool IsPastBlastZone(FxVec2 position) => BlastZone.HasValue && !BlastZone.Value.Contains(position);
}
