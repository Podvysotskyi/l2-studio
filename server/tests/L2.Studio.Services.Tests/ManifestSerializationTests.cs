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
}
