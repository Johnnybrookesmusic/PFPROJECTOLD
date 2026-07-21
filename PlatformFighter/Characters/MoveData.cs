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
///
/// OffsetX/OffsetY/Radius (all optional, default zero): real per-move
/// spatial hitbox placement, ported from MeleeLight's attributes.js
/// `offsets[...]` tables (the specific frame used is that hitbox's FIRST
/// active-frame position — same "one representative hit" simplification
/// as everything else in this struct, just applied to position too) and
/// each hitbox's own `size` field as Radius. Left at zero (the "no data
/// ported yet" sentinel — see Hitbox.cs) for moves that don't have this
/// wired in yet; CombatSystem falls back to the old flat-reach box for
/// those. See Characters/Fox/FoxMoves.cs for which moves have real data
/// and Docs/COMBAT.md for the port notes.
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
	public readonly Fx OffsetX;
	public readonly Fx OffsetY;
	public readonly Fx Radius;

	/// <summary>
	/// The move's REAL hitboxes — every simultaneous box, every sequential hit,
	/// each with its own window, offset, radius and knockback parameters. See
	/// HitboxSpec.cs for the full rationale and for how repeating (multi-hit)
	/// boxes are expressed.
	///
	/// Null/empty means "no per-hitbox data ported for this move", in which case
	/// the flat single-hit fields above are used instead — the pre-existing
	/// behaviour, kept working so a partially-ported move never silently loses
	/// its hit. Every Fox move now supplies this array; the flat fields remain
	/// as the move's REPRESENTATIVE hit (its strongest/first box) because the
	/// debug HUD and several tests read them directly.
	/// </summary>
	public readonly HitboxSpec[]? Hitboxes;

	/// <summary>True when real per-hitbox data exists for this move.</summary>
	public bool HasHitboxes => Hitboxes is { Length: > 0 };

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
        int landingLagFrames = 0, Fx launchSpeed = default, Fx launchDecayPerTick = default,
        Fx offsetX = default, Fx offsetY = default, Fx radius = default,
        int setKnockback = 0, HitboxSpec[]? hitboxes = null)
    {
        Hitboxes = hitboxes;
        SetKnockback = Fx.FromInt(setKnockback);
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
        OffsetX = offsetX;
        OffsetY = offsetY;
        Radius = radius;
    }

    public bool IsActiveOnFrame(int actionFrame)
    {
        if (HasHitboxes)
        {
            foreach (var hb in Hitboxes!)
                if (hb.IsActiveOnFrame(actionFrame)) return true;
            return false;
        }
        return actionFrame >= FirstActiveFrame && actionFrame <= LastActiveFrame;
    }

    public readonly Fx SetKnockback;

    public Hitbox ToHitbox() => new(
        Damage, KnockbackBase, KnockbackGrowth, DirX, DirY,
        OffsetX, OffsetY, Radius, SetKnockback);

    /// <summary>
	/// Last action frame on which ANY of this move's hitboxes is live. Used by
	/// the tests and the HUD to answer "is this move still threatening" without
	/// each caller re-scanning the array. Falls back to LastActiveFrame for a
	/// move with no per-hitbox data.
	/// </summary>
	public int LastHitboxFrame
	{
		get
		{
			if (!HasHitboxes) return LastActiveFrame;
			int last = 0;
			foreach (var hb in Hitboxes!)
				if (hb.LastActiveFrame > last) last = hb.LastActiveFrame;
			return last;
		}
	}
}
