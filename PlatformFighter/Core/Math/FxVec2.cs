namespace PlatformFighter.Core.Math;

/// <summary>
/// 2D vector in fixed-point space. Used for all simulation positions,
/// velocities, knockback vectors, hitbox offsets, etc.
/// Convention: +X right, +Y DOWN (matches Godot's 2D screen space so
/// render conversion is a straight cast, minimizing sign-flip bugs).
/// </summary>
public readonly struct FxVec2
{
    public readonly Fx X;
    public readonly Fx Y;

    public FxVec2(Fx x, Fx y) { X = x; Y = y; }

    public static readonly FxVec2 Zero = new(Fx.Zero, Fx.Zero);

    public static FxVec2 operator +(FxVec2 a, FxVec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static FxVec2 operator -(FxVec2 a, FxVec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static FxVec2 operator *(FxVec2 v, Fx s)     => new(v.X * s, v.Y * s);

    /// <summary>RENDER-ONLY conversion for placing Node2Ds.</summary>
    public Godot.Vector2 ToGodot() => new(X.ToFloat(), Y.ToFloat());

    public override string ToString() => $"({X}, {Y})";
}
