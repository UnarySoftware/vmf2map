using System.Diagnostics;
using System.Globalization;
using System.Text;
using Vmf2Map;

class Program
{
    private static string Format(double value)
    {
        return value.ToString("0.####", CultureInfo.InvariantCulture);
    }

    private static bool TryParseNumber(string text, out double value)
    {
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    public static int Main(string[] args)
    {
        const string Usage = """
        usage: vmf2map <input.vmf> [output.map] --wall <material> --floor <material>
                       [--angle <degrees>] [--scale <factor>]

        Converts a VMF's static brush geometry to a Valve 220 .map for Recusant usage. 
        Brush entities are folded into worldspawn as ordinary static brushes, and displacements 
        collapse back to the brush they were sculpted from. Point entities are dropped.

        Every face is assigned one of the two given material paths, based on which way it
        points. The VMF's own materials are discarded entirely.

          -w, --wall <material>   material for faces pointing sideways
          -f, --floor <material>  material for faces pointing up or down
          -a, --angle <degrees>   how close to vertical a face's normal must be to count as
                                  a floor/ceiling rather than a wall (default 45)
          -s, --scale <factor>    multiply the output geometry by this factor (default 1).
                                  Texture scales grow with it, so the map keeps its look and
                                  only changes size. Must be positive.

        Material paths are written into the .map exactly as given, e.g.
          --wall "unary.core . materials/dev/dev_measuregeneric01"
        """;

        string? inputArgument = null;
        string? outputArgument = null;
        string? wall = null;
        string? floor = null;
        var angle = SurfaceTextures.DefaultAngleDegrees;
        var scale = 1.0;

        for (var i = 0; i < args.Length; i++)
        {
            var argument = args[i];

            switch (argument)
            {
                case "-h" or "--help":
                    {
                        Console.Error.WriteLine(Usage);
                        return 1;
                    }
                case "-w" or "--wall":
                case "-f" or "--floor":
                case "-a" or "--angle":
                case "-s" or "--scale":
                    {
                        if (i + 1 >= args.Length)
                        {
                            Console.Error.WriteLine($"error: {argument} needs a value.");
                            return 1;
                        }

                        var value = args[++i];

                        switch (argument)
                        {
                            case "-w" or "--wall":
                                wall = value;
                                break;

                            case "-f" or "--floor":
                                floor = value;
                                break;

                            case "-a" or "--angle":
                                if (!TryParseNumber(value, out angle) || angle is < 0 or > 90)
                                {
                                    Console.Error.WriteLine($"error: --angle must be between 0 and 90, got '{value}'.");
                                    return 1;
                                }

                                break;

                            case "-s" or "--scale":
                                if (!TryParseNumber(value, out scale) || scale <= 0)
                                {
                                    Console.Error.WriteLine($"error: --scale must be a positive number, got '{value}'.");
                                    return 1;
                                }

                                break;
                        }

                        break;
                    }
                default:
                    {
                        if (argument.StartsWith('-'))
                        {
                            Console.Error.WriteLine($"error: unknown option '{argument}'.");
                            return 1;
                        }

                        if (inputArgument is null)
                        {
                            inputArgument = argument;
                        }
                        else if (outputArgument is null)
                        {
                            outputArgument = argument;
                        }
                        else
                        {
                            Console.Error.WriteLine($"error: unexpected argument '{argument}'.");
                            return 1;
                        }

                        break;
                    }
            }
        }

        if (inputArgument is null || wall is null || floor is null)
        {
            Console.Error.WriteLine(Usage);
            return 1;
        }

        var inputPath = Path.GetFullPath(inputArgument);
        var outputPath = outputArgument is not null
            ? Path.GetFullPath(outputArgument)
            : Path.ChangeExtension(inputPath, ".map");

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"error: no such file: {inputPath}");
            return 1;
        }

        if (string.Equals(inputPath, outputPath, StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("error: output would overwrite the input file.");
            return 1;
        }

        var stopwatch = Stopwatch.StartNew();

        List<VmfNode> roots;
        try
        {
            roots = VmfParser.Parse(File.ReadAllText(inputPath));
        }
        catch (FormatException e)
        {
            Console.Error.WriteLine($"error: {inputPath} is not a valid VMF: {e.Message}");
            return 1;
        }

        var surfaces = new SurfaceTextures(wall, floor, angle);
        var stats = new ConversionStats();
        var brushes = BrushConverter.Convert(roots, surfaces, scale, stats);

        if (brushes.Count == 0)
        {
            Console.Error.WriteLine("error: the VMF contains no brushes.");
            return 1;
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using (var writer = new StreamWriter(outputPath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
        {
            MapWriter.Write(writer, brushes);
        }

        stopwatch.Stop();

        Console.WriteLine($"{Path.GetFileName(inputPath)} -> {Path.GetFileName(outputPath)}  ({stopwatch.ElapsedMilliseconds} ms)");
        Console.WriteLine($"  brushes written        {stats.Brushes}");
        Console.WriteLine($"    from world           {stats.WorldBrushes}");

        foreach (var (classname, count) in stats.EntityBrushes.OrderByDescending(entry => entry.Value))
        {
            Console.WriteLine($"    from {classname,-16} {count}");
        }

        Console.WriteLine($"  displacements flattened {stats.FlattenedDisplacements} (included above)");
        Console.WriteLine($"  dropped: invalid       {stats.InvalidSolids}");
        Console.WriteLine();
        Console.WriteLine($"  faces: wall            {surfaces.WallFaces}  {wall}");
        Console.WriteLine($"  faces: floor/ceiling   {surfaces.FloorCeilingFaces}  {floor}");
        Console.WriteLine($"  (split at {Format(angle)} deg from vertical)");

        if (scale != 1.0)
        {
            Console.WriteLine();
            Console.WriteLine($"  geometry scaled        {Format(scale)}x");
        }

        return 0;
    }
}
