using System.Text;
using System.Text.Json;
using Xunit;

namespace L2.Studio.Services.Tests;

public sealed class ManifestSerializationTests
{
    [Fact]
    public void SerializesPublishedManifestsAsCompactJsonWithATrailingNewline()
    {
        var contents = AssetImportJobProcessor.SerializeManifest(new
        {
            SchemaVersion = 1,
            Entries = new[]
            {
                new { Name = "First", Value = 1 },
                new { Name = "Second", Value = 2 }
            }
        });

        var json = Encoding.UTF8.GetString(contents);
        Assert.EndsWith("\n", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\n ", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\r", json, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(contents.AsMemory(0, contents.Length - 1));
        Assert.Equal(1, document.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(2, document.RootElement.GetProperty("entries").GetArrayLength());
    }

    [Fact]
    public void SerializesMapLevelSummaryMetadataWhenPresent()
    {
        var contents = AssetImportJobProcessor.SerializeManifest(new MapManifest(
            15,
            "17_25",
            "17_25.unr",
            "source-hash",
            111,
            new MapLevelSummaryManifestEntry(
                "Talking Island",
                "L2 Studio",
                "A starting area.",
                "Welcome.",
                null,
                null,
                false,
                2,
                8,
                null,
                "MyLevel.Screenshot"),
            new MapEnvironmentManifestEntry(new MapColor(0, 0, 0), 0, null),
            [],
            [],
            [],
            [],
            [],
            [],
            new Dictionary<string, int>(),
            []));

        using var document = JsonDocument.Parse(contents.AsMemory(0, contents.Length - 1));
        var summary = document.RootElement.GetProperty("summary");
        Assert.Equal("Talking Island", summary.GetProperty("title").GetString());
        Assert.False(summary.GetProperty("hideFromMenus").GetBoolean());
        Assert.Equal("MyLevel.Screenshot", summary.GetProperty("screenshot").GetString());
    }

    [Fact]
    public void SerializesANullMapLevelSummaryWhenItIsUnavailable()
    {
        var contents = AssetImportJobProcessor.SerializeManifest(new { Summary = (object?)null });

        using var document = JsonDocument.Parse(contents.AsMemory(0, contents.Length - 1));
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("summary").ValueKind);
    }
}
