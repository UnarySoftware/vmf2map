using System.Text;

namespace Vmf2Map;

internal static class VmfParser
{
    public static List<VmfNode> Parse(string text)
    {
        var position = 0;
        var roots = new List<VmfNode>();

        while (true)
        {
            SkipWhitespaceAndComments(text, ref position);
            if (position >= text.Length)
            {
                break;
            }

            var name = ReadName(text, ref position);
            roots.Add(ReadBlock(text, ref position, name));
        }

        return roots;
    }

    private static VmfNode ReadBlock(string text, ref int position, string name)
    {
        SkipWhitespaceAndComments(text, ref position);
        Expect(text, ref position, '{');

        var node = new VmfNode(name);

        while (true)
        {
            SkipWhitespaceAndComments(text, ref position);
            if (position >= text.Length)
            {
                throw new FormatException($"Unexpected end of file inside block '{name}'.");
            }

            var c = text[position];
            if (c == '}')
            {
                position++;
                return node;
            }

            if (c == '"')
            {
                var key = ReadQuoted(text, ref position);
                SkipWhitespaceAndComments(text, ref position);

                if (position >= text.Length || text[position] != '"')
                {
                    throw new FormatException($"Expected a value after key '{key}' in block '{name}'.");
                }

                var value = ReadQuoted(text, ref position);
                node.Properties.Add(new KeyValuePair<string, string>(key, value));
                continue;
            }

            var childName = ReadName(text, ref position);
            node.Children.Add(ReadBlock(text, ref position, childName));
        }
    }

    private static void SkipWhitespaceAndComments(string text, ref int position)
    {
        while (position < text.Length)
        {
            var c = text[position];

            if (char.IsWhiteSpace(c))
            {
                position++;
                continue;
            }

            if (c == '/' && position + 1 < text.Length && text[position + 1] == '/')
            {
                while (position < text.Length && text[position] != '\n')
                {
                    position++;
                }

                continue;
            }

            return;
        }
    }

    private static string ReadQuoted(string text, ref int position)
    {
        position++;

        var builder = new StringBuilder();
        while (position < text.Length)
        {
            var c = text[position++];

            if (c == '\\' && position < text.Length && text[position] == '"')
            {
                builder.Append('"');
                position++;
                continue;
            }

            if (c == '"')
            {
                return builder.ToString();
            }

            builder.Append(c);
        }

        throw new FormatException("Unterminated quoted string.");
    }

    private static string ReadName(string text, ref int position)
    {
        var start = position;
        while (position < text.Length && !char.IsWhiteSpace(text[position]) && text[position] != '{' && text[position] != '}')
        {
            position++;
        }

        if (position == start)
        {
            throw new FormatException($"Expected a block name at offset {position}.");
        }

        return text[start..position];
    }

    private static void Expect(string text, ref int position, char expected)
    {
        if (position >= text.Length || text[position] != expected)
        {
            throw new FormatException($"Expected '{expected}' at offset {position}.");
        }

        position++;
    }
}
