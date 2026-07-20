using PlatformFighter.Core.Math;

namespace PlatformFighter.Core.Combat;

/// <summary>
/// Phase 7: one active hitbox. Deliberately minimal — no active-frame
/// windows, no shield-stun, no multi-hit, no hitlag. Those are Phase
/// 9/10 (Character framework / first fighter) concerns once there's an
/// actual move to attach them to; this struct is just "what happens if
/// this box connects with a hurtbox this tick."
///
/// DirX/DirY are a pre-normalized launch direction, NOT a Melee-style
/// angle-in-degrees. Fx has no sin/cos (see Core/Math/Fx.cs — sim math
/// is fixed-point only, and a correctly-rounded fixed-point trig table
/// is its own chunk of work), so instead of an angle this stores the
/// direction directly, e.g. (1,0) horizontal, (0,-1) straight up,
/// (Fx.Ratio(7,10), -Fx.Ratio(7,10)) diagonal up-and-away. DirX is
/// mirrored by the attacker's facing at apply-time; DirY is not
/// (negative is always up, per FxVec2's +Y-down convention).
/// </summary>
public readonly struct Hitbox
{
    public readonly Fx Damage;
    public readonly Fx KnockbackBase;
    public readonly Fx KnockbackGrowth;
    public readonly Fx DirX;
    public readonly Fx DirY;

    public Hitbox(Fx damage, Fx knockbackBase, Fx knockbackGrowth, Fx dirX, Fx dirY)
    {
        Damage = damage;
        KnockbackBase = knockbackBase;
        KnockbackGrowth = knockbackGrowth;
        DirX = dirX;
        DirY = dirY;
    }
}
