using System.Buffers.Binary;
using System.Text;
using L2.Tools.PackageReader;
using Xunit;

namespace L2.Tools.PackageReader.Tests;

public sealed class UnrealLevelSummaryTests
{
    [Fact]
    public void ReadsTopLevelLevelSummaryProperties()
    {
        var level = new UnrealPackageReader(Package(LevelSummary())).ReadLevel();

        var summary = Assert.IsType<UnrealLevelSummary>(level.Summary);
        Assert.Equal("Talking Island", summary.Title);
        Assert.Equal("L2 Studio", summary.Author);
        Assert.Equal("A starting area.", summary.Description);
        Assert.Equal("Welcome.", summary.LevelEnterText);
        Assert.Equal("Extra", summary.ExtraInfo);
        Assert.Equal("Deco", summary.DecoTextName);
        Assert.True(summary.HideFromMenus);
        Assert.Equal(2, summary.IdealPlayerCountMin);
        Assert.Equal(8, summary.IdealPlayerCountMax);
        Assert.Equal(1, summary.SinglePlayerTeamSize);
        Assert.Equal("ScreenshotMaterial", summary.Screenshot?.Path);
        Assert.Null(level.SummaryWarning);
        Assert.DoesNotContain("LevelSummary", level.UnrepresentedObjectClasses.Keys);
    }

    [Fact]
    public void ReportsAMissingLevelSummaryWithoutFailingTheLevel()
    {
        var level = new UnrealPackageReader(Package()).ReadLevel();

        Assert.Null(level.Summary);
        Assert.Equal("No top-level LevelSummary export was found.", level.SummaryWarning);
    }

    [Fact]
    public void ReportsDuplicateLevelSummariesWithoutSelectingOne()
    {
        var level = new UnrealPackageReader(Package(LevelSummary(), LevelSummary())).ReadLevel();

        Assert.Null(level.Summary);
        Assert.Equal("Expected one top-level LevelSummary export but found 2.", level.SummaryWarning);
    }

    [Fact]
    public void ReportsAnUnreadableLevelSummaryWithoutFailingTheLevel()
    {
        var level = new UnrealPackageReader(Package(MalformedLevelSummary())).ReadLevel();

        Assert.Null(level.Summary);
        Assert.Contains("could not be decoded", level.SummaryWarning, StringComparison.Ordinal);
    }

    private static byte[] LevelSummary()
    {
        var properties = new List<byte>();
        StringProperty(properties, "Title", "Talking Island");
        StringProperty(properties, "Author", "L2 Studio");
        StringProperty(properties, "Description", "A starting area.");
        StringProperty(properties, "LevelEnterText", "Welcome.");
        StringProperty(properties, "ExtraInfo", "Extra");
        StringProperty(properties, "DecoTextName", "Deco");
        BoolProperty(properties, "HideFromMenus", true);
        IntProperty(properties, "IdealPlayerCountMin", 2);
        IntProperty(properties, "IdealPlayerCountMax", 8);
        IntProperty(properties, "SinglePlayerTeamSize", 1);
        ObjectProperty(properties, "Screenshot", 2);
        Name(properties, "None");
        return [.. properties];
    }

    private static byte[] MalformedLevelSummary()
    {
        var properties = new List<byte>();
        Name(properties, "Title");
        properties.AddRange([0x5d, 4, (byte)'x']);
        return [.. properties];
    }

    private static byte[] Package(params byte[][] summaries)
    {
        var names = new[]
        {
            "None", "Core", "Class", "LevelSummary", "Texture", "Summary", "ScreenshotMaterial",
            "Title", "Author", "Description", "LevelEnterText", "ExtraInfo", "DecoTextName",
            "HideFromMenus", "IdealPlayerCountMin", "IdealPlayerCountMax", "SinglePlayerTeamSize", "Screenshot"
        };
        var nameIndices = names.Select((name, index) => (name, index)).ToDictionary(item => item.name, item => item.index);
        var nameTable = new List<byte>();
        foreach (var name in names)
        {
            UnrealString(nameTable, name);
            Int32(nameTable, 0);
        }

        var importTable = new List<byte>();
        Import(importTable, nameIndices, "LevelSummary");
        Import(importTable, nameIndices, "Texture");

        var exports = new List<(int ClassIndex, string Name, byte[] Data)>();
        exports.AddRange(summaries.Select(summary => (-1, "Summary", summary)));
        if (summaries.Length > 0) exports.Add((-2, "ScreenshotMaterial", Array.Empty<byte>()));
        var exportSize = exports.Sum(export =>
            CompactIndexSize(export.ClassIndex) + 1 + sizeof(int) + 1 + sizeof(uint) +
            CompactIndexSize(export.Data.Length) + (export.Data.Length == 0 ? 0 : 2));
        var headerSize = 36;
        var nameOffset = headerSize;
        var importOffset = nameOffset + nameTable.Count;
        var exportOffset = importOffset + importTable.Count;
        var serialOffset = exportOffset + exportSize;
        var exportTable = new List<byte>();
        var nextSerialOffset = serialOffset;
        foreach (var export in exports)
        {
            CompactIndex(exportTable, export.ClassIndex);
            CompactIndex(exportTable, 0);
            Int32(exportTable, 0);
            Name(exportTable, nameIndices, export.Name);
            UInt32(exportTable, 0);
            CompactIndex(exportTable, export.Data.Length);
            if (export.Data.Length > 0)
            {
                CompactIndex(exportTable, nextSerialOffset);
                nextSerialOffset += export.Data.Length;
            }
        }

        var package = new List<byte>();
        UInt32(package, UnrealPackageReader.PackageTag);
        UInt32(package, ((uint)12 << 16) | 123);
        Int32(package, 0);
        Int32(package, names.Length);
        Int32(package, nameOffset);
        Int32(package, exports.Count);
        Int32(package, exportOffset);
        Int32(package, 2);
        Int32(package, importOffset);
        package.AddRange(nameTable);
        package.AddRange(importTable);
        package.AddRange(exportTable);
        foreach (var export in exports) package.AddRange(export.Data);
        return [.. package];
    }

    private static void Import(List<byte> target, IReadOnlyDictionary<string, int> names, string objectName)
    {
        Name(target, names, "Core");
        Name(target, names, "Class");
        Int32(target, 0);
        Name(target, names, objectName);
    }

    private static void StringProperty(List<byte> target, string name, string value)
    {
        Name(target, name);
        var contents = new List<byte>();
        UnrealString(contents, value);
        target.Add(0x5d);
        target.Add((byte)contents.Count);
        target.AddRange(contents);
    }

    private static void BoolProperty(List<byte> target, string name, bool value)
    {
        Name(target, name);
        target.Add((byte)(3 | (value ? 0x80 : 0)));
    }

    private static void IntProperty(List<byte> target, string name, int value)
    {
        Name(target, name);
        target.Add(0x22);
        Int32(target, value);
    }

    private static void ObjectProperty(List<byte> target, string name, int value)
    {
        Name(target, name);
        target.Add(0x05);
        CompactIndex(target, value);
    }

    private static void Name(List<byte> target, string name) =>
        CompactIndex(target, Array.IndexOf(new[]
        {
            "None", "Core", "Class", "LevelSummary", "Texture", "Summary", "ScreenshotMaterial",
            "Title", "Author", "Description", "LevelEnterText", "ExtraInfo", "DecoTextName",
            "HideFromMenus", "IdealPlayerCountMin", "IdealPlayerCountMax", "SinglePlayerTeamSize", "Screenshot"
        }, name));

    private static void Name(List<byte> target, IReadOnlyDictionary<string, int> names, string name) =>
        CompactIndex(target, names[name]);

    private static void UnrealString(List<byte> target, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value + '\0');
        CompactIndex(target, bytes.Length);
        target.AddRange(bytes);
    }

    private static void CompactIndex(List<byte> target, int value)
    {
        var negative = value < 0;
        var unsigned = negative ? -value : value;
        var first = unsigned & 0x3f;
        unsigned >>= 6;
        if (unsigned != 0) first |= 0x40;
        if (negative) first |= 0x80;
        target.Add((byte)first);
        while (unsigned != 0)
        {
            var current = unsigned & 0x7f;
            unsigned >>= 7;
            if (unsigned != 0) current |= 0x80;
            target.Add((byte)current);
        }
    }

    private static int CompactIndexSize(int value)
    {
        var unsigned = Math.Abs(value);
        var size = 1;
        unsigned >>= 6;
        while (unsigned != 0)
        {
            size++;
            unsigned >>= 7;
        }
        return size;
    }

    private static void Int32(List<byte> target, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        target.AddRange(bytes);
    }

    private static void UInt32(List<byte> target, uint value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        target.AddRange(bytes);
    }
}
