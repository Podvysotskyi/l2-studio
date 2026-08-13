using System.Numerics;

namespace L2.Tools.PackageReader;

public readonly record struct UnrealParticleBeamEndPoint(
    string ActorTag,
    UnrealVectorRange Offset,
    float Weight);
