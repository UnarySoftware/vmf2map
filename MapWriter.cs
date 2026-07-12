using System.Globalization;
using System.Text;

namespace Vmf2Map;

internal static class MapWriter
{
    public static void Write(TextWriter writer, IReadOnlyList<Brush> brushes)
    {
        writer.WriteLine("// Game: Recusant");
        writer.WriteLine("// Format: Valve");
        writer.WriteLine("// entity 0");
        writer.WriteLine("{");
        writer.WriteLine("\"mapversion\" \"220\"");
        writer.WriteLine("\"wad\" \"\"");
        writer.WriteLine("\"classname\" \"worldspawn\"");

        for (var i = 0; i < brushes.Count; i++)
        {
            writer.WriteLine($"// brush {i}");
            writer.WriteLine("{");

            foreach (var face in brushes[i].Faces)
            {
                writer.WriteLine(FormatFace(face));
            }

            writer.WriteLine("}");
        }

        writer.WriteLine("}");
    }

    private static string FormatFace(Face face)
    {
        var builder = new StringBuilder();

        AppendPoint(builder, face.P0);
        builder.Append(' ');
        AppendPoint(builder, face.P1);
        builder.Append(' ');
        AppendPoint(builder, face.P2);

        builder.Append(" \"").Append(face.Material).Append('"');

        AppendAxis(builder, face.U);
        AppendAxis(builder, face.V);

        builder.Append(' ').Append(Number(face.Rotation));
        builder.Append(' ').Append(Number(face.U.Scale));
        builder.Append(' ').Append(Number(face.V.Scale));

        return builder.ToString();
    }

    private static void AppendPoint(StringBuilder builder, Vec3 p) =>
        builder.Append("( ")
            .Append(Number(p.X)).Append(' ')
            .Append(Number(p.Y)).Append(' ')
            .Append(Number(p.Z))
            .Append(" )");

    private static void AppendAxis(StringBuilder builder, TextureAxis axis) =>
        builder.Append(" [ ")
            .Append(Number(axis.Axis.X)).Append(' ')
            .Append(Number(axis.Axis.Y)).Append(' ')
            .Append(Number(axis.Axis.Z)).Append(' ')
            .Append(Number(axis.Offset))
            .Append(" ]");

    private static string Number(double value)
    {
        if (value == 0)
        {
            return "0";
        }

        var text = value.ToString("R", CultureInfo.InvariantCulture);
        return text.Contains('E') ? text.Replace("E", "e") : text;
    }
}
