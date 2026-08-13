using System.Buffers.Binary;
using L2.Tools.PackageReader;
using Xunit;

namespace L2.Tools.PackageReader.Tests;

public sealed class UnrealPackageVersionTests
{
    [Theory]
    [InlineData((ushort)0)]
    [InlineData((ushort)1)]
    [InlineData((ushort)3)]
    [InlineData((ushort)6)]
    [InlineData((ushort)11)]
    public void ReadsRevision118SupportedLicenseePackages(ushort licenseeVersion)
    {
        var level = new UnrealPackageReader(Package(118, licenseeVersion)).ReadLevel();

        Assert.Empty(level.Actors);
    }

    [Theory]
    [InlineData((ushort)2)]
    [InlineData((ushort)4)]
    public void RejectsUnlistedRevision118LicenseePackages(ushort licenseeVersion)
    {
        var exception = Assert.Throws<InvalidDataException>(
            () => new UnrealPackageReader(Package(118, licenseeVersion)).ReadLevel());

        Assert.Equal($"Unsupported Unreal package version 118/{licenseeVersion}.", exception.Message);
    }

    private static byte[] Package(ushort packageVersion, ushort licenseeVersion)
    {
        var package = new byte[36];
        BinaryPrimitives.WriteUInt32LittleEndian(package, UnrealPackageReader.PackageTag);
        BinaryPrimitives.WriteUInt32LittleEndian(
            package.AsSpan(sizeof(uint)),
            ((uint)licenseeVersion << 16) | packageVersion);
        BinaryPrimitives.WriteInt32LittleEndian(package.AsSpan(16), 36);
        BinaryPrimitives.WriteInt32LittleEndian(package.AsSpan(24), 36);
        BinaryPrimitives.WriteInt32LittleEndian(package.AsSpan(32), 36);
        return package;
    }
}
