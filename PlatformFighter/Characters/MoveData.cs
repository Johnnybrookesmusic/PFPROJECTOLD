using PlatformFighter.Core.Combat;
using PlatformFighter.Core.Math;

namespace PlatformFighter.Characters;

public enum MoveCategory
{
	GroundedNormal,
	Aerial,
	Special,
}

/// <summary>
/// Phase 9: one move's usable data — frame timing plus a single
/// representative hit. Real Melee moves with multiple active hitboxes
/// (Falco's 5-hit fair, Fox's 2-hit dash attack, etc.) are collapsed to
/// ONE hit here, same simplification FoxMoveData.json/FalcoMoveData.json
/// already make at the transcription layer — see either file's _readme.
/// This is a deliberate scope cut for the first playable demo, not an
/// oversight: modeling true multi-hit sequencing (each sub-hit its own
/// active window, its own hitbox id turning others off) is real work for
/// whoever picks up hit-detection accuracy after this milestone.
///
/// DirX/DirY are baked in at construction time via AngleTable, from the
/// Melee-style angle this move's data actually specifies — callers never
/// touch AngleTable directly.
/// </summary>
public readonly struct MoveDef
{
    public readonly string Name;
    public readonly MoveCategory Category;

    /// <summary>1-indexed action frames, inclusive — matches the source
	/// data's own convention (see FoxMoveData.json/FalcoMoveData.json).</summary>
	public readonly int FirstActiveFrame;
	public readonly int LastActiveFrame;
	public readonly int TotalFrames;

	/// <summary>Recorded but not yet consumed — see PlayerMover.TickAttacking's
	/// doc comment on why there's no early-cancel via IASA yet.</summary>
	public readonly int IasaFrame;

	/// <summary>Aerial-only. 0 for GroundedNormal/Special — those lock in place
	/// for TotalFrames instead (see PlayerMover.TickAttacking).</summary>
	public readonly int LandingLagFrames;

	public readonly Fx Damage;
	public readonly Fx KnockbackBase;
	public readonly Fx KnockbackGrowth;
	public readonly Fx DirX;
	public readonly Fx DirY;

	/// <summary>Phase 11: nonzero ONLY for Up-B (Fire Fox) and Side-B (Illusion) —
	/// the two Fox specials that physically propel the character rather than just
	/// throwing out a hit. Real values pulled from MeleeLight's own physics code
    /// (src/characters/fox/moves/UPSPECIALLAUNCH.js and SIDESPECIALGROUND.js),
    /// NOT from fightcore.gg (which only lists hitbox frame data, not the
	/// self-movement speed) — see FoxMoves.cs's per-move notes for the exact
	/// source lines. PlayerMover.TryStartAttack/TickAttacking are what actually
	/// consume this; MoveDef just carries the number.</summary>
	public readonly Fx LaunchSpeed;
	/// <summary>Per-tick speed reduction while the launch is decaying. 0 for every
	/// move that doesn't self-launch.</summary>
    public readonly Fx LaunchDecayPerTick;

    public MoveDef(
        string name, MoveCategory category,
        int firstActiveFrame, int lastActiveFrame, int totalFrames, int iasaFrame,
        int damage, int angleDegrees, int knockbackBase, int knockbackGrowth,
        int landingLagFrames = 0, Fx launchSpeed = default, Fx launchDecayPerTick = default)
    {
        Name = name;
        Category = category;
        FirstActiveFrame = firstActiveFrame;
        LastActiveFrame = lastActiveFrame;
        TotalFrames = totalFrames;
        IasaFrame = iasaFrame;
        LandingLagFrames = landingLagFrames;
        Damage = Fx.FromInt(damage);
        KnockbackBase = Fx.FromInt(knockbackBase);
        KnockbackGrowth = Fx.FromInt(knockbackGrowth);
        (DirX, DirY) = AngleTable.Get(angleDegrees);
        LaunchSpeed = launchSpeed;
        LaunchDecayPerTick = launchDecayPerTick;
    }

    public bool IsActiveOnFrame(int actionFrame) =>
        actionFrame >= FirstActiveFrame && actionFrame <= LastActiveFrame;

    public Hitbox ToHitbox() => new(Damage, KnockbackBase, KnockbackGrowth, DirX, DirY);
}
