using PlatformFighter.Core.Combat;
using PlatformFighter.Core.Math;

namespace PlatformFighter.Characters;

/// <summary>
/// ONE hitbox belonging to a move, with its own active window and its own full
/// parameter set — a direct 1:1 counterpart of a single
/// <c>new createHitbox(offset, size, dmg, angle, kg, bk, sk, type, clank, hG, hA)</c>
/// in MeleeLight's <c>src/characters/fox/attributes.js</c>.
///
/// WHY THIS TYPE EXISTS. <see cref="MoveDef"/> previously carried exactly ONE
/// hit — damage/angle/knockback/offset/radius as flat fields — and its own doc
/// comment called that out as a deliberate scope cut ("real Melee moves with
/// multiple active hitboxes are collapsed to ONE hit here"). That cut is what
/// this type removes. Real Fox is multi-hitbox almost everywhere:
///
///   - <b>Multiple boxes, same instant.</b> Fox's up-tilt has FOUR simultaneous
///     hitboxes at four different offsets, and they do NOT share parameters:
///     id0 does 12 damage at angle 110, the other three do 9 at angles 84/80/80.
///     Collapsing that to id0 alone meant three quarters of the move's real
///     spatial coverage simply did not exist, and every hit reported the tip's
///     damage regardless of where it actually connected.
///   - <b>Multiple hits, over time.</b> Forward air is five distinct hits
///     (fair1..fair5, 7/5/6/4/3 damage), the drill is a box that re-arms every
///     three frames, jab3 is a five-position rapid jab, and dash attack, both
///     smashes, nair, bair, upair and the get-up attacks are all two-stage.
///
/// A move therefore owns an ARRAY of these, and each one answers independently:
/// "am I live on this action frame, and if so where am I and what do I do?"
///
/// MULTI-HIT RE-ARMING. <see cref="RefreshPeriod"/> is how a single window
/// expresses a box that hits repeatedly. MeleeLight's ATTACKAIRD (the drill)
/// runs `timer % 3` over frames 5..25 — activating on one phase, incrementing
/// on the next, clearing on the third. Rather than emit seven near-identical
/// specs, one spec with RefreshPeriod 3 reproduces the same cadence, and
/// <see cref="HitGroup"/> gives each repetition a distinct id so
/// PlayerMover can tell "this is a NEW hit" from "same hit, still overlapping"
/// and allow the defender to be struck again. Zero means a plain continuous
/// window (the common case).
///
/// COORDINATE CONVENTIONS, unchanged from Hitbox.cs and applied at construction:
/// OffsetX is attacker-relative and mirrors with facing; OffsetY is the source
/// MeleeLight value NEGATED (Y-up source, Y-down engine — the same adaptation
/// Stages/Battlefield.cs documents); Radius is the source's <c>size</c> field,
/// a circle radius because real Melee hitboxes are spheres.
/// </summary>
public readonly struct HitboxSpec
{
    /// <summary>1-indexed inclusive action frames, matching MoveDef's own convention.</summary>
    public readonly int FirstActiveFrame;
    public readonly int LastActiveFrame;

    /// <summary>Frames between re-arms for a repeating (multi-hit) box. 0 = a
    /// single continuous window. See the class doc on the drill.</summary>
    public readonly int RefreshPeriod;

    public readonly Fx OffsetX;
    public readonly Fx OffsetY;
    public readonly Fx Radius;

    public readonly Fx Damage;
    public readonly Fx KnockbackBase;
    public readonly Fx KnockbackGrowth;
    public readonly Fx SetKnockback;
    public readonly Fx DirX;
    public readonly Fx DirY;

    public readonly int HitType;
    public readonly bool HitsGrounded;
    public readonly bool HitsAirborne;

    public HitboxSpec(
        int firstActiveFrame, int lastActiveFrame,
        int damage, int angleDegrees, int knockbackGrowth, int knockbackBase,
        Fx offsetX, Fx offsetY, Fx radius,
        int setKnockback = 0, int hitType = 0, int refreshPeriod = 0,
        bool hitsGrounded = true, bool hitsAirborne = true)
    {
        FirstActiveFrame = firstActiveFrame;
        LastActiveFrame = lastActiveFrame;
        RefreshPeriod = refreshPeriod;
        OffsetX = offsetX;
        OffsetY = offsetY;
        Radius = radius;
        Damage = Fx.FromInt(damage);
        KnockbackBase = Fx.FromInt(knockbackBase);
        KnockbackGrowth = Fx.FromInt(knockbackGrowth);
        SetKnockback = Fx.FromInt(setKnockback);
        (DirX, DirY) = AngleTable.Get(angleDegrees);
        HitType = hitType;
        HitsGrounded = hitsGrounded;
        HitsAirborne = hitsAirborne;
    }

    public bool IsActiveOnFrame(int actionFrame) =>
        actionFrame >= FirstActiveFrame && actionFrame <= LastActiveFrame;

    /// <summary>
    /// Which repetition of a refreshing box this frame belongs to. Distinct
    /// values mean distinct hits, so a defender already struck by group 0 can
    /// still be struck by group 1. Always 0 for a non-refreshing box, which is
    /// exactly the "one hit per move" behaviour those moves already had.
    /// </summary>
    public int HitGroup(int actionFrame) =>
        RefreshPeriod <= 0 ? 0 : (actionFrame - FirstActiveFrame) / RefreshPeriod;

    public Hitbox ToHitbox() => new(
        Damage, KnockbackBase, KnockbackGrowth, DirX, DirY,
        OffsetX, OffsetY, Radius, SetKnockback, HitType, HitsGrounded, HitsAirborne);
}
