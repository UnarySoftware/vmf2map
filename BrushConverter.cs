using System.Globalization;

namespace Vmf2Map;

internal sealed class ConversionStats
{
    public int Brushes { get; set; }
    public int WorldBrushes { get; set; }
    public Dictionary<string, int> EntityBrushes { get; } = [];
    public int FlattenedDisplacements { get; set; }
    public int InvalidSolids { get; set; }
}

internal static class BrushConverter
{
    private const int MinimumFacesPerBrush = 4;
    private const double DegeneratePlaneEpsilon = 1e-9;

    public static List<Brush> Convert(
        List<VmfNode> roots,
        SurfaceTextures surfaces,
        double scale,
        ConversionStats stats)
    {
        var brushes = new List<Brush>();

        foreach (var root in roots)
        {
            if (root.Name is not ("world" or "entity"))
            {
                continue;
            }

            var solids = CollectSolids(root);
            if (solids.Count == 0)
            {
                continue;
            }

            var converted = 0;
            foreach (var solid in solids)
            {
                var brush = ConvertSolid(solid, surfaces, scale, stats);
                if (brush is not null)
                {
                    brushes.Add(brush);
                    converted++;
                }
            }

            if (root.Name == "world")
            {
                stats.WorldBrushes += converted;
            }
            else
            {
                var classname = root.Get("classname") ?? "(no classname)";
                stats.EntityBrushes[classname] = stats.EntityBrushes.GetValueOrDefault(classname) + converted;
            }
        }

        stats.Brushes = brushes.Count;
        return brushes;
    }

    private static List<VmfNode> CollectSolids(VmfNode node)
    {
        var solids = new List<VmfNode>();
        Walk(node, solids);
        return solids;

        static void Walk(VmfNode node, List<VmfNode> solids)
        {
            foreach (var child in node.Children)
            {
                if (child.Name == "solid")
                {
                    solids.Add(child);
                }
                else
                {
                    Walk(child, solids);
                }
            }
        }
    }

    private static Brush? ConvertSolid(VmfNode solid, SurfaceTextures surfaces, double scale, ConversionStats stats)
    {
        var sides = solid.ChildrenNamed("side").ToList();

        if (sides.Any(side => side.HasChild("dispinfo")))
        {
            stats.FlattenedDisplacements++;
        }

        if (sides.Count < MinimumFacesPerBrush)
        {
            stats.InvalidSolids++;
            return null;
        }

        var faces = new List<Face>(sides.Count);

        foreach (var side in sides)
        {
            var face = ConvertSide(side, scale);
            if (face is null)
            {
                stats.InvalidSolids++;
                return null;
            }

            faces.Add(face);
        }

        var brush = new Brush();
        foreach (var face in faces)
        {
            brush.Faces.Add(face with { Material = surfaces.Resolve(face.Normal) });
        }

        return brush;
    }

    private static Face? ConvertSide(VmfNode side, double scale)
    {
        var plane = side.Get("plane");
        var uaxis = side.Get("uaxis");
        var vaxis = side.Get("vaxis");

        if (plane is null || uaxis is null || vaxis is null)
        {
            return null;
        }

        if (!TryParsePlane(plane, out var p0, out var p1, out var p2))
        {
            return null;
        }

        if ((p0 - p1).Cross(p2 - p1).Length < DegeneratePlaneEpsilon)
        {
            return null;
        }

        if (!TryParseTextureAxis(uaxis, out var u) || !TryParseTextureAxis(vaxis, out var v))
        {
            return null;
        }

        _ = TryParseDouble(side.Get("rotation"), out var rotation);

        return new Face
        {
            P0 = p0 * scale,
            P1 = p1 * scale,
            P2 = p2 * scale,
            Material = string.Empty,
            U = u with { Scale = u.Scale * scale },
            V = v with { Scale = v.Scale * scale },
            Rotation = rotation,
        };
    }

    private static bool TryParsePlane(string text, out Vec3 p0, out Vec3 p1, out Vec3 p2)
    {
        p0 = p1 = p2 = default;

        var points = new Vec3[3];
        var count = 0;
        var position = 0;

        while (count < 3)
        {
            var open = text.IndexOf('(', position);
            if (open < 0)
            {
                return false;
            }

            var close = text.IndexOf(')', open);
            if (close < 0)
            {
                return false;
            }

            if (!TryParseVec3(text.AsSpan(open + 1, close - open - 1), out points[count]))
            {
                return false;
            }

            count++;
            position = close + 1;
        }

        p0 = points[0];
        p1 = points[1];
        p2 = points[2];
        return true;
    }

    private static bool TryParseTextureAxis(string text, out TextureAxis axis)
    {
        axis = default;

        var open = text.IndexOf('[');
        var close = text.IndexOf(']');
        if (open < 0 || close < open)
        {
            return false;
        }

        var parts = Split(text.AsSpan(open + 1, close - open - 1), 4);
        if (parts is null)
        {
            return false;
        }

        if (!TryParseDouble(parts[0], out var x) ||
            !TryParseDouble(parts[1], out var y) ||
            !TryParseDouble(parts[2], out var z) ||
            !TryParseDouble(parts[3], out var offset))
        {
            return false;
        }

        _ = TryParseDouble(text[(close + 1)..].Trim(), out var scale);
        if (scale == 0)
        {
            scale = 1;
        }

        axis = new TextureAxis(new Vec3(x, y, z), offset, scale);
        return true;
    }

    private static bool TryParseVec3(ReadOnlySpan<char> text, out Vec3 value)
    {
        value = default;

        var parts = Split(text, 3);
        if (parts is null)
        {
            return false;
        }

        if (!TryParseDouble(parts[0], out var x) ||
            !TryParseDouble(parts[1], out var y) ||
            !TryParseDouble(parts[2], out var z))
        {
            return false;
        }

        value = new Vec3(x, y, z);
        return true;
    }

    private static string[]? Split(ReadOnlySpan<char> text, int expected)
    {
        var parts = text.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == expected ? parts : null;
    }

    private static bool TryParseDouble(string? text, out double value)
    {
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
