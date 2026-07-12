namespace Vmf2Map;

internal sealed class SurfaceTextures(string wall, string floorCeiling, double angleDegrees)
{
    public const double DefaultAngleDegrees = 45.0;
    private readonly double _minVerticalness = Math.Cos(double.DegreesToRadians(angleDegrees));
    public int WallFaces { get; private set; }
    public int FloorCeilingFaces { get; private set; }

    public string Resolve(Vec3 unitNormal)
    {
        var verticalness = Math.Abs(unitNormal.Z);

        if (verticalness >= _minVerticalness)
        {
            FloorCeilingFaces++;
            return floorCeiling;
        }

        WallFaces++;
        return wall;
    }
}
