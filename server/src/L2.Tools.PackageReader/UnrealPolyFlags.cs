using System.Numerics;

namespace L2.Tools.PackageReader;

[Flags]
public enum UnrealPolyFlags : uint
{
    None = 0,
    Invisible = 0x00000001,
    Masked = 0x00000002,
    Translucent = 0x00000004,
    Modulated = 0x00000040,
    FakeBackdrop = 0x00000080,
    TwoSided = 0x00000100,
    Unlit = 0x00400000,
    Portal = 0x04000000
}
