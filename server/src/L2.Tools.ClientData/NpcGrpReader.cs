using System.Globalization;
using System.Text;

namespace L2.Tools.ClientData;

public static class NpcGrpReader
{
    public static IReadOnlyList<NpcAppearanceSourceRecord> ReadProtocol211(ReadOnlySpan<byte> source) =>
        ReadDecoded(LineageClientDataDecoder.DecodeProtocol211(source));

    public static IReadOnlyList<NpcAppearanceSourceRecord> ReadDecoded(ReadOnlySpan<byte> payload)
    {
        var text = Encoding.Unicode.GetString(payload).TrimStart('\uFEFF');
        var records = new List<NpcAppearanceSourceRecord>();
        var lineNumber = 0;
        foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            lineNumber++;
            var trimmed = line.Trim().TrimEnd('\0');
            if (trimmed.Length == 0) continue;
            try
            {
                records.Add(ParseLine(trimmed));
            }
            catch (Exception exception) when (exception is FormatException or OverflowException or KeyNotFoundException)
            {
                throw new InvalidDataException($"The protocol 211 npcgrp record on line {lineNumber} is malformed.", exception);
            }
        }
        if (records.Count == 0) throw new InvalidDataException("The protocol 211 npcgrp file contains no NPC records.");
        return records;
    }

    private static NpcAppearanceSourceRecord ParseLine(string line)
    {
        var tokens = Tokens(line);
        if (tokens.Count < 3 || tokens[0] != "npc_begin" || tokens[^1] != "npc_end")
            throw new FormatException("An NPC row must start with npc_begin and end with npc_end.");
        var fields = tokens.Skip(1).SkipLast(1).Select(token => token.Split('=', 2))
            .ToDictionary(parts => parts[0], parts => parts.Length == 2 ? parts[1] : string.Empty, StringComparer.Ordinal);
        return new NpcAppearanceSourceRecord(
            UInt(fields, "npc_id"),
            Scalar(fields, "npc_name"),
            Float(fields, "npc_speed"),
            Scalar(fields, "class_name"),
            Scalar(fields, "mesh_name"),
            Array(fields, "texture_name"),
            Float(fields, "collision_radius"),
            Float(fields, "collision_height"),
            Array(fields, "attack_sound1"),
            Array(fields, "defense_sound1"),
            Array(fields, "damage_sound"),
            Float(fields, "sound_vol"),
            Float(fields, "sound_radius"),
            Float(fields, "sound_random"),
            Scalar(fields, "attack_effect"));
    }

    private static IReadOnlyList<string> Tokens(string line)
    {
        var result = new List<string>();
        var start = -1;
        var squareDepth = 0;
        var braceDepth = 0;
        for (var index = 0; index <= line.Length; index++)
        {
            var current = index == line.Length ? ' ' : line[index];
            if (current == '[') squareDepth++;
            else if (current == ']') squareDepth--;
            else if (current == '{') braceDepth++;
            else if (current == '}') braceDepth--;
            if (char.IsWhiteSpace(current) && squareDepth == 0 && braceDepth == 0)
            {
                if (start >= 0)
                {
                    result.Add(line[start..index]);
                    start = -1;
                }
            }
            else if (start < 0)
            {
                start = index;
            }
        }
        if (squareDepth != 0 || braceDepth != 0) throw new FormatException("An NPC row contains unbalanced delimiters.");
        return result;
    }

    private static string Scalar(IReadOnlyDictionary<string, string> fields, string name)
    {
        var value = fields[name].Trim();
        return value.Length >= 2 && value[0] == '[' && value[^1] == ']'
            ? value[1..^1]
            : value;
    }

    private static IReadOnlyList<string> Array(IReadOnlyDictionary<string, string> fields, string name)
    {
        var value = fields[name].Trim();
        if (value.Length >= 2 && value[0] == '{' && value[^1] == '}') value = value[1..^1];
        return value.Split([';', ',', ':'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.Trim().Trim('[', ']'))
            .Where(item => item.Length > 0 && !string.Equals(item, "none", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static uint UInt(IReadOnlyDictionary<string, string> fields, string name) =>
        uint.Parse(fields[name], NumberStyles.Integer, CultureInfo.InvariantCulture);

    private static float Float(IReadOnlyDictionary<string, string> fields, string name) =>
        float.Parse(fields[name], NumberStyles.Float, CultureInfo.InvariantCulture);
}
