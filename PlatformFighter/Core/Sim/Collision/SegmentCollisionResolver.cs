using PlatformFighter.Core.Math;

namespace PlatformFighter.Core.Sim.Collision;

/// <summary>Which list a fighter's current standing surface came from. Kept
/// alongside the index because ground and platform are separate lists on
/// <see cref="StageGeometry"/> and an index alone is ambiguous — the same
/// split MeleeLight carries as its <c>["g"|"p", index]</c> pair.</summary>
public enum SurfaceKind : int
{
    None = 0,
    Ground = 1,
    Platform = 2,
}

/// <summary>
/// Result of one Step 2 resolve. <see cref="Position"/> is the FEET origin
/// (see Ecb.cs on why the origin moved from centre to feet).
/// </summary>
public struct SegmentCollisionResult
{
    public FxVec2 Position;
    public FxVec2 Velocity;
    public bool Grounded;
    /// <summary>Index into <see cref="StageGeometry.Ground"/> or
    /// <see cref="StageGeometry.Platforms"/> per <see cref="SurfaceKind"/>.
    /// -1 when airborne. Must be round-tripped by the caller into the next
    /// tick's resolve — it is the standing-surface identity, and losing it
    /// turns "walk along this ledge" into "re-search every surface and maybe
    /// pick a different one," which is how fighters teleport between platforms.</summary>
    public int SurfaceIndex;
    public SurfaceKind Surface;
    public bool HitCeiling;
    public bool HitWall;
    /// <summary>True on the tick the fighter walked off the end of its surface
    /// with nothing adjacent — MeleeLight's <c>fallOffGround</c>. Distinct from
    /// simply being airborne, because Step 3 (locomotion) needs the EDGE event
    /// to drive teeter/ledge behaviour, not just the resulting state.</summary>
    public bool WalkedOffEdge;
}

/// <summary>
/// Step 2 of the Melee Lite Translation Directive: real segment collision,
/// replacing <see cref="CollisionResolver"/>'s AABB-vs-box model. Step 1 landed
/// Battlefield's true geometry (diagonal chamfers, sloped ceilings, real
/// ledges) and then could not resolve against ANY of it; that gap — flagged in
/// StageGeometry.cs, FxSegment.cs and Battlefield.cs alike — is what this file
/// closes.
///
/// MODEL, ported from <c>src/physics/physics.js</c>'s <c>dealWithGround</c> /
/// <c>fallOffGround</c> / <c>dealWithCeilingCollision</c>:
///  - A grounded fighter is ON A NAMED SURFACE and stays on it, following its
///    Y as X changes (so slopes work), until its X leaves that surface's span —
///    at which point it falls off. It does not re-search the stage every tick.
///    This is the single most important structural difference from the old
///    resolver and the reason ledges can exist at all.
///  - An airborne fighter lands when its ECB bottom point CROSSES a surface
///    from above during the tick. Topmost crossed surface wins, since that is
///    the one it reached first.
///  - Ceilings block upward head movement; walls block horizontal movement.
///
/// WHAT IS DELIBERATELY NOT PORTED IN STEP 2, stated plainly so it is not
/// mistaken for finished work:
///  - The full ECB-corner sweep. MeleeLight's <c>environmentalCollision.js</c>
///    (1343 lines) sweeps all four ECB points against every surface with corner
///    cases, edge connectivity, ECB squashing and interpolated sub-steps. Here,
///    GROUND uses the true bottom point (which is the part that must be exact,
///    and is), while CEILINGS and WALLS use the ECB's bounding box. That is an
///    approximation, and it is wrong in exactly one visible way: against the
///    diagonal underside chamfers, a fighter will stop slightly early rather
///    than sliding along the slope. Correct for "you cannot pass through the
///    stage," imprecise for recovery tech that hugs the underside.
///  - <c>connected</c> edge adjacency (walking from one ground piece onto a
///    touching one). Battlefield defines no adjacency — verified in Step 1,
///    it has a single ground segment — so there is nothing to test it against
///    yet. <see cref="SegmentCollisionResult.WalkedOffEdge"/> is where it would
///    hook in.
///  - Teetering, ledge grabs, wall jumps/clings, ECB squash. Step 6.
///  - Continuous/swept motion. Still discrete, so the tunnelling caveat from
///    CollisionResolver.cs still applies — and matters MORE now, because
///    Step 1's real units make the stage 136.8 wide while a knockback launch
///    can exceed that in a handful of ticks. Flagged for Step 4 (knockback).
///
/// <see cref="CollisionResolver"/> is intentionally left in place and untouched:
/// Debug/TestBody.cs and the Phase 4 F9 tests are built on it and still pass,
/// and deleting a verified system to make room for an unverified one is exactly
/// the move this project's own rules forbid. Migrate callers once Step 2's own
/// tests pass in-engine.
/// </summary>
public static class SegmentCollisionResolver
{
    /// <summary>
    /// Snap epsilon — MeleeLight's <c>additionalOffset</c>. After landing, the
    /// feet are placed this far INTO the surface rather than exactly on it, so
    /// that the next tick's "am I still on this surface" test is not sitting on
    /// an exact-equality boundary. Value is arbitrary but must be much smaller
    /// than any real movement; 1/1024 of a unit is ~0.0001 of Fox's ECB width.
    /// </summary>
    public static readonly Fx SurfaceSnapEpsilon = Fx.Ratio(1, 1024);

    public static SegmentCollisionResult Resolve(
        FxVec2 previousOrigin,
        FxVec2 movedOrigin,
        FxVec2 velocity,
        in Ecb ecb,
        StageGeometry stage,
        bool wasGrounded,
        SurfaceKind currentSurface,
        int currentSurfaceIndex,
        bool passThroughPlatforms)
    {
        var r = new SegmentCollisionResult
        {
            Position = movedOrigin,
            Velocity = velocity,
            Grounded = false,
            Surface = SurfaceKind.None,
            SurfaceIndex = -1,
        };

        // ---- 1. Walls (horizontal block) --------------------------------
        ResolveWalls(ref r, previousOrigin, ecb, stage, wasGrounded);

        // ---- 2. Ceilings (upward block) ---------------------------------
        ResolveCeilings(ref r, previousOrigin, ecb, stage, wasGrounded);

        // ---- 3. Ground ---------------------------------------------------
        // Two guards before the stay-on-surface path, both found by running
        // this algorithm against real Battlefield data before it shipped:
        //
        //  a) Moving UP means the fighter has left the ground this tick (a
        //     jump's takeoff velocity is applied before resolve runs). Without
        //     this, the surface-follow snaps them straight back down and a
        //     jump silently does nothing — the fighter never leaves the floor.
        //  b) When passing through platforms, a fighter STANDING on one must be
        //     released, not re-snapped. Otherwise down-through-platform inputs
        //     are eaten by the same follow logic.
        bool holdingSurface =
            wasGrounded
            && currentSurface != SurfaceKind.None
            && r.Velocity.Y >= Fx.Zero
            && !(passThroughPlatforms && currentSurface == SurfaceKind.Platform);

        if (holdingSurface)
        {
            if (TryStayOnSurface(ref r, stage, currentSurface, currentSurfaceIndex))
                return r;

            // Walked past the end of the surface with nothing adjacent.
            r.WalkedOffEdge = true;
        }

        TryLand(ref r, previousOrigin, ecb, stage, passThroughPlatforms);
        return r;
    }

    /// <summary>
    /// MeleeLight's <c>dealWithGround</c> "else" branch: still within the
    /// surface's X span, so follow its Y. Returns false when X has left the
    /// span, which is <c>fallOffGround</c>'s trigger.
    /// </summary>
    private static bool TryStayOnSurface(
        ref SegmentCollisionResult r, StageGeometry stage, SurfaceKind kind, int index)
    {
        if (kind == SurfaceKind.Ground)
        {
            if (index < 0 || index >= stage.Ground.Count) return false;
            var seg = stage.Ground[index];
            if (!SegmentMath.SpansX(seg, r.Position.X)) return false;
            if (!SegmentMath.TryYAt(seg, r.Position.X, out Fx y)) return false;

            r.Position = new FxVec2(r.Position.X, y + SurfaceSnapEpsilon);
            r.Velocity = new FxVec2(r.Velocity.X, Fx.Zero);
            r.Grounded = true;
            r.Surface = SurfaceKind.Ground;
            r.SurfaceIndex = index;
            return true;
        }

        if (kind == SurfaceKind.Platform)
        {
            if (index < 0 || index >= stage.Platforms.Count) return false;
            var plat = stage.Platforms[index];
            if (r.Position.X < plat.XMin || r.Position.X > plat.XMax) return false;

            r.Position = new FxVec2(r.Position.X, plat.Y + SurfaceSnapEpsilon);
            r.Velocity = new FxVec2(r.Velocity.X, Fx.Zero);
            r.Grounded = true;
            r.Surface = SurfaceKind.Platform;
            r.SurfaceIndex = index;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Airborne landing. The ECB bottom point must cross the surface downward
    /// during this tick; the TOPMOST surface crossed wins, because that is the
    /// one the fighter reached first. Y-down, so "topmost" is smallest Y.
    /// </summary>
    private static void TryLand(
        ref SegmentCollisionResult r,
        FxVec2 previousOrigin,
        in Ecb ecb,
        StageGeometry stage,
        bool passThroughPlatforms)
    {
        // Rising: cannot land on anything this tick.
        if (r.Velocity.Y < Fx.Zero) return;

        // Feet-based crossing (Ecb.Bottom returns the feet — see its doc comment
        // on why the airborne rise is not applied here).
        Fx prevBottom = ecb.Bottom(previousOrigin, false).Y;
        Fx newBottom = ecb.Bottom(r.Position, false).Y;

        bool found = false;
        Fx bestY = Fx.Zero;
        var bestKind = SurfaceKind.None;
        int bestIndex = -1;

        for (int i = 0; i < stage.Ground.Count; i++)
        {
            var seg = stage.Ground[i];
            if (!SegmentMath.SpansX(seg, r.Position.X)) continue;
            if (!SegmentMath.TryYAt(seg, r.Position.X, out Fx y)) continue;
            if (prevBottom > y || newBottom < y) continue; // did not cross downward

            if (!found || y < bestY)
            {
                found = true;
                bestY = y;
                bestKind = SurfaceKind.Ground;
                bestIndex = i;
            }
        }

        if (!passThroughPlatforms)
        {
            for (int i = 0; i < stage.Platforms.Count; i++)
            {
                var plat = stage.Platforms[i];
                if (r.Position.X < plat.XMin || r.Position.X > plat.XMax) continue;
                if (prevBottom > plat.Y || newBottom < plat.Y) continue;

                if (!found || plat.Y < bestY)
                {
                    found = true;
                    bestY = plat.Y;
                    bestKind = SurfaceKind.Platform;
                    bestIndex = i;
                }
            }
        }

        if (!found) return;

        // Place the FEET on the surface. Ecb.Bottom returns the feet in both
        // grounded and airborne cases (see its doc comment), so no offset is
        // re-applied here — re-applying one is the classic way to end up
        // floating or sunk by exactly that offset.
        r.Position = new FxVec2(r.Position.X, bestY + SurfaceSnapEpsilon);
        r.Velocity = new FxVec2(r.Velocity.X, Fx.Zero);
        r.Grounded = true;
        r.Surface = bestKind;
        r.SurfaceIndex = bestIndex;
        r.WalkedOffEdge = false;
    }

    /// <summary>
    /// Ceilings block upward head movement. Bounding-box approximation — see
    /// the class header on why, and on what it costs against diagonals.
    /// </summary>
    private static void ResolveCeilings(
        ref SegmentCollisionResult r, FxVec2 previousOrigin, in Ecb ecb, StageGeometry stage, bool wasGrounded)
    {
        if (r.Velocity.Y >= Fx.Zero) return; // not moving up

        Fx prevTop = ecb.Top(previousOrigin).Y;
        Fx newTop = ecb.Top(r.Position).Y;
        Fx left = r.Position.X - ecb.HalfWidth;
        Fx right = r.Position.X + ecb.HalfWidth;

        for (int i = 0; i < stage.Ceiling.Count; i++)
        {
            var seg = stage.Ceiling[i];
            if (right < seg.MinX || left > seg.MaxX) continue;

            // Sample the ceiling at the head's X, clamped into the segment so a
            // partially-overlapping box still gets a sensible height rather
            // than being skipped.
            Fx sampleX = Fx.Clamp(r.Position.X, seg.MinX, seg.MaxX);
            if (!SegmentMath.TryYAt(seg, sampleX, out Fx ceilY)) continue;

            // Crossed upward through it this tick?
            if (prevTop < ceilY || newTop > ceilY) continue;

            r.Position = new FxVec2(r.Position.X, ceilY + ecb.TopHeight + SurfaceSnapEpsilon);
            r.Velocity = new FxVec2(r.Velocity.X, Fx.Zero);
            r.HitCeiling = true;
            return;
        }
    }

    /// <summary>
    /// Walls block horizontal movement. Bounding-box approximation, same caveat
    /// as ceilings. WallL and WallR are checked identically here rather than by
    /// their named orientation: MeleeLight's names describe the wall's own
    /// facing, and Battlefield puts wallL segments on both sides of the stage
    /// (see StageGeometry.cs), so deciding the push direction from the
    /// PREVIOUS position is both simpler and harder to get backwards than
    /// trusting the list a segment happens to live in.
    /// </summary>
    private static void ResolveWalls(
        ref SegmentCollisionResult r, FxVec2 previousOrigin, in Ecb ecb, StageGeometry stage, bool wasGrounded)
    {
        if (r.Velocity.X == Fx.Zero) return;

        Fx bottom = ecb.Bottom(r.Position, wasGrounded).Y;
        Fx top = ecb.Top(r.Position).Y;

        ResolveWallList(ref r, stage.WallL, previousOrigin, ecb, top, bottom, wasGrounded);
        ResolveWallList(ref r, stage.WallR, previousOrigin, ecb, top, bottom, wasGrounded);
    }

    private static void ResolveWallList(
        ref SegmentCollisionResult r,
        System.Collections.Generic.List<FxSegment> walls,
        FxVec2 previousOrigin,
        in Ecb ecb,
        Fx top,
        Fx bottom,
        bool wasGrounded)
    {
        for (int i = 0; i < walls.Count; i++)
        {
            var seg = walls[i];

            Fx segTop = Fx.Min(seg.A.Y, seg.B.Y);
            Fx segBottom = Fx.Max(seg.A.Y, seg.B.Y);

            // A GROUNDED fighter cannot walk into a wall that lies entirely at or
            // below the surface it is standing on. Battlefield's underside
            // chamfers start exactly at the ground line (wallR runs (68.4,0) ->
            // (65,6), straight down from the lip), so without this a fighter
            // walking toward the ledge slams into an invisible wall at x≈62 and
            // the ledge is unreachable.
            //
            // NOTE: this compares against previousOrigin.Y, NOT `bottom`, and that
            // is not interchangeable — a real bug lived in that gap. `bottom` is
            // ecb.Bottom(movedOrigin), i.e. THIS tick's position BEFORE the
            // ground-snap step (step 3) runs — walls resolve first (step 1).
            // PlayerMover.TickGrounded re-adds gravity to Velocity.Y every tick,
            // even while resting (see its own comment: the resolver only
            // re-confirms Grounded when Velocity.Y is downward, so resting
            // contact needs re-triggering every tick), so `bottom` here sits
            // gravity-below the true ground line, not epsilon-below it. Comparing
            // `segTop >= bottom - SurfaceSnapEpsilon` therefore came out FALSE
            // for a fighter standing at rest — the skip never fired, and the
            // original invisible-wall-at-the-ledge bug came right back even
            // though the epsilon math alone reads correct in isolation.
            //
            // previousOrigin.Y is last tick's actual RESOLVED resting position
            // (surface + SurfaceSnapEpsilon, written by TryStayOnSurface at the
            // end of the previous tick) — untouched by this tick's gravity nudge,
            // which is exactly "was the surface I was just standing on at/above
            // this wall's top." Scoped to the grounded case on purpose: while
            // AIRBORNE the full test still applies, so the thing these walls
            // exist for — a fighter recovering from BELOW the stage running into
            // the underside — is still blocked.
            if (wasGrounded && segTop >= previousOrigin.Y - SurfaceSnapEpsilon) continue;
            if (bottom <= segTop || top >= segBottom) continue;

            Fx left = r.Position.X - ecb.HalfWidth;
            Fx right = r.Position.X + ecb.HalfWidth;
            if (right < seg.MinX || left > seg.MaxX) continue; // no horizontal overlap

            // Push out along the axis the fighter came FROM, using the previous
            // position rather than the velocity sign — velocity can be zeroed by
            // an earlier resolve in the same tick, the previous position cannot.
            if (previousOrigin.X + ecb.HalfWidth <= seg.MinX)
            {
                r.Position = new FxVec2(seg.MinX - ecb.HalfWidth - SurfaceSnapEpsilon, r.Position.Y);
                r.Velocity = new FxVec2(Fx.Zero, r.Velocity.Y);
                r.HitWall = true;
            }
            else if (previousOrigin.X - ecb.HalfWidth >= seg.MaxX)
            {
                r.Position = new FxVec2(seg.MaxX + ecb.HalfWidth + SurfaceSnapEpsilon, r.Position.Y);
                r.Velocity = new FxVec2(Fx.Zero, r.Velocity.Y);
                r.HitWall = true;
            }
        }
    }
}
