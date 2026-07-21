using PlatformFighter.Core.Combat;
using PlatformFighter.Core.Math;
using PlatformFighter.Core.Sim;
using PlatformFighter.Core.Sim.Collision;

namespace PlatformFighter.Gameplay;

/// <summary>
/// Static definition of a projectile kind — MeleeLight's <c>articles</c> entry,
/// minus the rendering fields. Source: <c>src/physics/article.js</c>.
/// </summary>
public readonly struct ProjectileSpec
{
    public readonly Fx SpeedPerTick;
    public readonly Fx SpawnOffsetX;
    public readonly Fx SpawnOffsetY;
    public readonly Fx Radius;
    public readonly Fx Damage;
    public readonly Fx KnockbackBase;
    public readonly Fx KnockbackGrowth;
    public readonly Fx DirX;
    public readonly Fx DirY;
    public readonly int MaxLifetimeFrames;
    public readonly bool DestroyOnHit;

    public ProjectileSpec(
        Fx speedPerTick, Fx spawnOffsetX, Fx spawnOffsetY, Fx radius,
        int damage, int knockbackBase, int knockbackGrowth, int angleDegrees,
        int maxLifetimeFrames, bool destroyOnHit)
    {
        SpeedPerTick = speedPerTick;
        SpawnOffsetX = spawnOffsetX;
        SpawnOffsetY = spawnOffsetY;
        Radius = radius;
        Damage = Fx.FromInt(damage);
        KnockbackBase = Fx.FromInt(knockbackBase);
        KnockbackGrowth = Fx.FromInt(knockbackGrowth);
        (DirX, DirY) = Characters.AngleTable.Get(angleDegrees);
        MaxLifetimeFrames = maxLifetimeFrames;
        DestroyOnHit = destroyOnHit;
    }

    public Hitbox ToHitbox() =>
        new(Damage, KnockbackBase, KnockbackGrowth, DirX, DirY,
            Fx.Zero, Fx.Zero, Radius);

    /// <summary>
    /// Fox's Blaster shot, transcribed from <c>articles.LASER</c> in
    /// <c>src/physics/article.js</c> plus its spawn call in
    /// <c>characters/fox/moves/NEUTRALSPECIALGROUND.js</c>.
    ///
    /// <c>new createHitbox(new Vec2D(0,0), 1.172, 3, 361, isFox ? 0 : ..., 0,
    /// isFox ? 0 : ..., 0, 0, 1, 1)</c> — for Fox specifically the knockback
    /// growth AND the set-knockback term are both 0, and base knockback is 0.
    ///
    /// THAT IS NOT A TRANSCRIPTION ERROR AND MUST NOT BE "FIXED": Fox's laser
    /// deals damage but causes no knockback and no flinch at all. That is the
    /// defining difference between Fox's Blaster and Falco's (whose branch of
    /// the same ternary gets growth 100 / set-knockback 5, which is why Falco's
    /// laser staggers and Fox's does not). It is the reason Fox can lasercamp
    /// without interrupting his own follow-ups.
    ///
    /// Velocity: <c>(isFox ? 7 : 5) * cos(rotate) * face</c> with rotate 0, so a
    /// flat 7 units/tick horizontally, no gravity, no arc. Spawn offset
    /// <c>x: 8, y: 7</c> from the caller (Y negated here for this engine's
    /// Y-down convention). Lifetime <c>timer > 200</c>, and
    /// <c>destroyOnHit: true</c>.
    /// </summary>
    public static readonly ProjectileSpec FoxLaser = new(
        speedPerTick: Fx.FromInt(7),
        spawnOffsetX: Fx.FromInt(8),
        spawnOffsetY: -Fx.FromInt(7),
        radius: Fx.Ratio(1172, 1000),
        damage: 3,
        knockbackBase: 0,
        knockbackGrowth: 0,
        angleDegrees: 361,
        maxLifetimeFrames: 200,
        destroyOnHit: true);
}

/// <summary>
/// One live projectile. A plain value type held in a fixed-size pool rather
/// than an <c>ISimObject</c> of its own, deliberately:
///
///  - <b>Determinism.</b> MeleeLight's articles live in a JS array that is
///    pushed to and spliced from as shots spawn and die. Reproducing that with
///    dynamic <c>SimWorld.Register</c>/<c>Unregister</c> would make the sim's
///    object list order depend on spawn/despawn history, which is exactly the
///    kind of thing that desyncs a rollback netcode later. A fixed pool with
///    stable slot indices has no such ordering.
///  - <b>Snapshots.</b> Every slot serializes unconditionally, so the snapshot
///    layout is constant regardless of how many shots are in flight. That keeps
///    save/load byte-identical in shape, which the state-hash tests depend on.
///  - <b>No allocation.</b> Nothing is created per shot.
/// </summary>
public struct Projectile
{
    public bool Active;
    /// <summary>Which player fired it — a projectile never hits its owner.</summary>
    public int OwnerIndex;
    public FxVec2 Position;
    public FxVec2 PreviousPosition;
    public FxVec2 Velocity;
    public int FramesAlive;
    /// <summary>Facing at spawn, kept so the hit applies knockback in the
    /// direction of travel rather than the shooter's current facing (he may
    /// have turned around since firing).</summary>
    public bool MovingRight;

    public void Deactivate()
    {
        Active = false;
        FramesAlive = 0;
        Position = PreviousPosition = FxVec2.Zero;
        Velocity = FxVec2.Zero;
    }
}

/// <summary>
/// Fixed-capacity projectile pool: spawn, per-tick movement, lifetime and
/// stage-bounds despawn, plus serialization. Hit resolution against fighters
/// lives in <see cref="CombatSystem"/>, alongside the fighter-vs-fighter checks,
/// so all damage application goes through one place.
/// </summary>
public sealed class ProjectilePool
{
    /// <summary>Enough for sustained laser fire from both fighters at once
    /// (Fox's Blaster is 40 frames per shot and a laser lives 200), with room
    /// to spare. Fixed so the snapshot layout never changes size.</summary>
    public const int Capacity = 16;

    private readonly Projectile[] _slots = new Projectile[Capacity];

    public int Count
    {
        get
        {
            int n = 0;
            for (int i = 0; i < Capacity; i++) if (_slots[i].Active) n++;
            return n;
        }
    }

    public ref Projectile this[int i] => ref _slots[i];

    /// <summary>
    /// Spawn into the LOWEST free slot. Lowest-index-first (rather than a
    /// rotating cursor) keeps the pool's contents a pure function of the
    /// sequence of spawns and despawns, which is what makes a snapshot taken
    /// now and restored later produce an identical pool.
    /// If every slot is busy the shot is dropped — MeleeLight has no such cap,
    /// but at 16 simultaneous shots the difference is unreachable in practice.
    /// </summary>
    public bool TrySpawn(in ProjectileSpec spec, FxVec2 shooterPosition, bool facingRight, int ownerIndex)
    {
        for (int i = 0; i < Capacity; i++)
        {
            if (_slots[i].Active) continue;

            Fx offsetX = facingRight ? spec.SpawnOffsetX : -spec.SpawnOffsetX;
            var pos = new FxVec2(shooterPosition.X + offsetX, shooterPosition.Y + spec.SpawnOffsetY);
            Fx vx = facingRight ? spec.SpeedPerTick : -spec.SpeedPerTick;

            _slots[i] = new Projectile
            {
                Active = true,
                OwnerIndex = ownerIndex,
                Position = pos,
                PreviousPosition = pos,
                Velocity = new FxVec2(vx, Fx.Zero),
                FramesAlive = 0,
                MovingRight = facingRight,
            };
            return true;
        }
        return false;
    }

    /// <summary>
    /// Advance every live projectile one frame. Despawns on lifetime expiry or
    /// on leaving the stage's blast zone — the latter standing in for
    /// MeleeLight's <c>wallDetection(i)</c>, which tests the shot against real
    /// stage segments. Blast-zone despawn is a deliberate simplification and is
    /// noted as such: a laser will currently pass through Battlefield's
    /// underside geometry rather than being absorbed by it. It never survives
    /// past the blast zone, so nothing leaks.
    /// </summary>
    public void Tick(StageGeometry stage)
    {
        for (int i = 0; i < Capacity; i++)
        {
            if (!_slots[i].Active) continue;

            _slots[i].PreviousPosition = _slots[i].Position;
            _slots[i].Position += _slots[i].Velocity;
            _slots[i].FramesAlive++;

            if (_slots[i].FramesAlive > ProjectileSpec.FoxLaser.MaxLifetimeFrames
                || stage.IsPastBlastZone(_slots[i].Position))
            {
                _slots[i].Deactivate();
            }
        }
    }

    public void Clear()
    {
        for (int i = 0; i < Capacity; i++) _slots[i].Deactivate();
    }

    /// <summary>Every slot writes unconditionally — see the class doc on why a
    /// constant-size snapshot matters.</summary>
    public void SaveState(StateWriter w)
    {
        for (int i = 0; i < Capacity; i++)
        {
            ref Projectile p = ref _slots[i];
            w.WriteBool(p.Active);
            w.WriteInt(p.OwnerIndex);
            w.WriteFxVec2(p.Position);
            w.WriteFxVec2(p.PreviousPosition);
            w.WriteFxVec2(p.Velocity);
            w.WriteInt(p.FramesAlive);
            w.WriteBool(p.MovingRight);
        }
    }

    public void LoadState(StateReader r)
    {
        for (int i = 0; i < Capacity; i++)
        {
            _slots[i].Active = r.ReadBool();
            _slots[i].OwnerIndex = r.ReadInt();
            _slots[i].Position = r.ReadFxVec2();
            _slots[i].PreviousPosition = r.ReadFxVec2();
            _slots[i].Velocity = r.ReadFxVec2();
            _slots[i].FramesAlive = r.ReadInt();
            _slots[i].MovingRight = r.ReadBool();
        }
    }
}
