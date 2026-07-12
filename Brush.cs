namespace Vmf2Map;

internal readonly record struct Vec3(double X, double Y, double Z)
{
    public static Vec3 operator -(Vec3 a, Vec3 b)
    {
        return new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    public static Vec3 operator *(Vec3 a, double scalar)
    {
        return new(a.X * scalar, a.Y * scalar, a.Z * scalar);
    }

    public Vec3 Cross(Vec3 other)
    {
        return new(
        (Y * other.Z) - (Z * other.Y),
        (Z * other.X) - (X * other.Z),
        (X * other.Y) - (Y * other.X));
    }

    public double Length => Math.Sqrt((X * X) + (Y * Y) + (Z * Z));

    public Vec3 Normalized()
    {
        var length = Length;
        return length == 0 ? this : new Vec3(X / length, Y / length, Z / length);
    }
}

internal readonly record struct TextureAxis(Vec3 Axis, double Offset, double Scale);

internal sealed record Face
{
    public required Vec3 P0 { get; init; }
    public required Vec3 P1 { get; init; }
    public required Vec3 P2 { get; init; }
    public required string Material { get; init; }
    public required TextureAxis U { get; init; }
    public required TextureAxis V { get; init; }
    public required double Rotation { get; init; }
    public Vec3 Normal => (P0 - P1).Cross(P2 - P1).Normalized();
}

internal sealed class Brush
{
    public List<Face> Faces { get; } = [];
}
