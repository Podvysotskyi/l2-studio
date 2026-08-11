using L2.Tools.PackageReader;
using L2.Tools.StaticMeshConverter;

namespace L2.Studio.Services;

internal sealed record StaticMeshMaterialResolution(
    IReadOnlyList<StaticMeshMaterialBinding?> SectionMaterials,
    int MaterialCount,
    int ResolvedMaterialCount,
    string Status,
    string? Error);

internal sealed class StaticMeshMaterialResolver
{
    private readonly IReadOnlyDictionary<string, TextureManifestEntry> textures;
    private readonly IReadOnlyDictionary<string, TextureMaterialManifestEntry> materials;

    public StaticMeshMaterialResolver(IEnumerable<TextureManifest> manifests)
        : this(
            manifests.SelectMany(manifest => manifest.Textures),
            manifests.SelectMany(manifest => manifest.Materials ?? []))
    {
    }

    public StaticMeshMaterialResolver(
        IEnumerable<TextureManifestEntry> textureEntries,
        IEnumerable<TextureMaterialManifestEntry> materialEntries)
    {
        textures = textureEntries
            .GroupBy(texture => Key(texture.PackageName, texture.ObjectName), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        materials = materialEntries
            .GroupBy(material => Key(material.PackageName, material.ObjectName), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public StaticMeshMaterialResolution Resolve(UnrealStaticMesh mesh, string currentPackage)
    {
        var references = mesh.Sections
            .Select(section => Normalize(section.Material, currentPackage))
            .ToArray();
        var distinctReferences = references
            .Where(reference => reference is not null)
            .Select(reference => Key(reference!.PackageName, reference.ObjectName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (distinctReferences.Length == 0)
        {
            return new StaticMeshMaterialResolution(
                Enumerable.Repeat<StaticMeshMaterialBinding?>(null, mesh.Sections.Count).ToArray(),
                0,
                0,
                "none",
                null);
        }

        var resolved = new Dictionary<string, StaticMeshMaterialBinding>(StringComparer.OrdinalIgnoreCase);
        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sectionMaterials = references.Select(reference =>
        {
            if (reference is null)
            {
                return null;
            }
            var key = Key(reference.PackageName, reference.ObjectName);
            if (resolved.TryGetValue(key, out var cached))
            {
                return cached;
            }
            try
            {
                var binding = Resolve(reference, currentPackage, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
                resolved[key] = binding;
                return binding;
            }
            catch (InvalidDataException exception)
            {
                errors[key] = exception.Message;
                return null;
            }
        }).Select(binding => binding is null
            ? null
            : binding with { WindMode = WindMode(mesh.Name, binding) }).ToArray();

        var status = resolved.Count == distinctReferences.Length
            ? "resolved"
            : resolved.Count == 0 ? "unresolved" : "partial";
        return new StaticMeshMaterialResolution(
            sectionMaterials,
            distinctReferences.Length,
            resolved.Count,
            status,
            errors.Count == 0 ? null : string.Join(" ", errors.Values.Distinct().Take(4)));
    }

    private StaticMeshMaterialBinding Resolve(
        TextureMaterialReference reference,
        string currentPackage,
        HashSet<string> visited,
        int depth)
    {
        if (depth >= 16)
        {
            throw new InvalidDataException($"Material '{reference.PackageName}.{reference.ObjectName}' exceeds 16 graph nodes.");
        }
        var normalized = Normalize(reference, currentPackage)!;
        var key = Key(normalized.PackageName, normalized.ObjectName);
        if (!visited.Add(key))
        {
            throw new InvalidDataException($"Material '{normalized.PackageName}.{normalized.ObjectName}' contains a cycle.");
        }
        try
        {
            if (textures.TryGetValue(key, out var texture))
            {
                if (texture.Url is null)
                {
                    throw new InvalidDataException($"Texture '{normalized.PackageName}.{normalized.ObjectName}' is not published.");
                }
                return new StaticMeshMaterialBinding(
                    normalized.ObjectName,
                    texture.Url,
                    null,
                    null,
                    StaticMeshBlendMode.Opaque,
                    false,
                    0.5f,
                    true,
                    true,
                    DiffuseAnimation: Animation(texture));
            }
            if (!materials.TryGetValue(key, out var material))
            {
                throw new InvalidDataException($"Material '{normalized.PackageName}.{normalized.ObjectName}' is not published.");
            }

            var innerReference = material.ClassName switch
            {
                "FinalBlend" or "Panner" or "Rotator" or "TexPanner" or "TexRotator" or "Combiner" or "TexOscillator" or "TexOscillatorTriggered" or "ColorModifier" => material.Material,
                "FadeColor" => null,
                _ => material.Diffuse
            };
            var inner = innerReference is null
                ? new StaticMeshMaterialBinding(
                    material.ObjectName,
                    null,
                    null,
                    null,
                    StaticMeshBlendMode.Opaque,
                    false,
                    0.5f,
                    true,
                    true)
                : Resolve(innerReference, material.PackageName, visited, depth + 1);
            var opacity = ResolveTextureChannel(
                material.Opacity,
                material.PackageName,
                visited,
                depth + 1);
            if (opacity.Url is null)
            {
                opacity = ResolveTextureChannel(
                    material.Mask,
                    material.PackageName,
                    visited,
                    depth + 1);
            }
            var emissive = ResolveTextureChannel(
                material.SelfIllumination,
                material.PackageName,
                visited,
                depth + 1);
            var detail = ResolveTextureChannel(
                material.Detail,
                material.PackageName,
                visited,
                depth + 1);
            var selfIlluminationMask = ResolveTextureChannel(
                material.SelfIlluminationMask,
                material.PackageName,
                visited,
                depth + 1);
            var specular = ResolveTextureChannel(
                material.Specular,
                material.PackageName,
                visited,
                depth + 1);
            var specularityMask = ResolveTextureChannel(
                material.SpecularityMask,
                material.PackageName,
                visited,
                depth + 1);
            var opacityUrl = opacity.Url;
            var emissiveUrl = emissive.Url;
            var emissiveAnimation = emissive.Animation;
            var detailUrl = detail.Url;
            StaticMeshMaterialBinding? secondary = null;
            if (material.ClassName == "Combiner" && material.Material2 is not null)
            {
                secondary = Resolve(
                    material.Material2,
                    material.PackageName,
                    visited,
                    depth + 1);
            }
            var blendMode = material.ClassName == "FinalBlend"
                ? FrameBufferBlendMode(material.FrameBufferBlending)
                : material.OutputBlending != 0
                    ? OutputBlendMode(material.OutputBlending)
                    : inner.BlendMode;
            if (material.AlphaTest)
            {
                blendMode = StaticMeshBlendMode.Masked;
            }
            else if (opacityUrl is not null && blendMode == StaticMeshBlendMode.Opaque)
            {
                blendMode = StaticMeshBlendMode.AlphaBlend;
            }
            return inner with
            {
                Name = material.ObjectName,
                OpacityUrl = opacityUrl ?? inner.OpacityUrl,
                EmissiveUrl = emissiveUrl ?? inner.EmissiveUrl,
                BlendMode = blendMode,
                DoubleSided = material.TwoSided || material.TreatAsTwoSided || inner.DoubleSided,
                AlphaCutoff = material.AlphaRef / 255f,
                DepthWrite = material.ZWrite,
                DepthTest = material.ZTest,
                OpacitySource = opacityUrl is null
                    ? inner.OpacitySource
                    : StaticMeshOpacitySource.Texture,
                OpacityChannel = opacityUrl is null
                    ? inner.OpacityChannel
                    : StaticMeshOpacityChannel.Alpha,
                OpacityAnimation = opacityUrl is null
                    ? inner.OpacityAnimation
                    : opacity.Animation,
                EmissiveAnimation = emissiveUrl is null
                    ? inner.EmissiveAnimation
                    : emissiveAnimation,
                PanRate = material.ClassName is "Panner" or "TexPanner" ? material.PanRate : inner.PanRate,
                RotationRate = material.ClassName is "Rotator" or "TexRotator" ? material.RotationRate : inner.RotationRate,
                DetailUrl = detailUrl ?? inner.DetailUrl,
                DetailScale = detailUrl is null ? inner.DetailScale : material.DetailScale,
                Tint = material.ClassName == "ColorModifier" && material.ModifierColor is { } tint
                    ? new StaticMeshMaterialTint(
                        tint.Red / 255f,
                        tint.Green / 255f,
                        tint.Blue / 255f,
                        tint.Alpha / 255f)
                    : inner.Tint,
                UvOscillation = material.ClassName is "TexOscillator" or "TexOscillatorTriggered"
                    ? new StaticMeshUvOscillation(
                        material.UOscillationType,
                        material.VOscillationType,
                        material.UOscillationRate,
                        material.VOscillationRate,
                        material.UOscillationAmplitude,
                        material.VOscillationAmplitude,
                        material.UOscillationPhase,
                        material.VOscillationPhase)
                    : inner.UvOscillation,
                Fade = material.ClassName == "FadeColor" &&
                    material.FadeColor1 is { } fadeColor1 &&
                    material.FadeColor2 is { } fadeColor2
                    ? new StaticMeshMaterialFade(
                        Tint(fadeColor1),
                        Tint(fadeColor2),
                        material.ColorFadeType,
                        material.FadePeriod,
                        material.FadePhase)
                    : inner.Fade,
                Composite = material.ClassName == "Combiner" && secondary is not null
                    ? new StaticMeshMaterialComposite(
                        secondary.DiffuseUrl,
                        secondary.Tint,
                        secondary.Fade,
                        ResolveTextureChannel(material.Mask, material.PackageName, visited, depth + 1).Url,
                        material.CombineOperation,
                        material.AlphaOperation,
                        material.InvertMask,
                        material.Modulate4X ? 4 : material.Modulate2X ? 2 : 1)
                    : inner.Composite,
                SelfIlluminationMaskUrl = selfIlluminationMask.Url ?? inner.SelfIlluminationMaskUrl,
                SpecularUrl = specular.Url ?? inner.SpecularUrl,
                SpecularityMaskUrl = specularityMask.Url ?? inner.SpecularityMaskUrl,
                PerformLightingOnSpecularPass = material.PerformLightingOnSpecularPass || inner.PerformLightingOnSpecularPass
            };
        }
        finally
        {
            visited.Remove(key);
        }
    }

    private ResolvedTextureChannel ResolveTextureChannel(
        TextureMaterialReference? reference,
        string currentPackage,
        HashSet<string> visited,
        int depth)
    {
        var normalized = Normalize(reference, currentPackage);
        if (normalized is null) return new ResolvedTextureChannel(null, null);
        try
        {
            var resolved = Resolve(normalized, normalized.PackageName, visited, depth);
            return new ResolvedTextureChannel(resolved.DiffuseUrl, resolved.DiffuseAnimation);
        }
        catch (InvalidDataException)
        {
            // Optional channels must not make an otherwise usable diffuse graph fail.
            return new ResolvedTextureChannel(null, null);
        }
    }

    private sealed record ResolvedTextureChannel(
        string? Url,
        StaticMeshTextureAnimation? Animation);

    private static StaticMeshMaterialTint Tint(TextureMaterialColor color) => new(
        color.Red / 255f,
        color.Green / 255f,
        color.Blue / 255f,
        color.Alpha / 255f);

    private static StaticMeshTextureAnimation? Animation(TextureManifestEntry? texture)
    {
        if (texture?.Animation is not { FrameUrls.Count: > 1 } animation) return null;
        var frameRate = animation.MaxFrameRate > 0
            ? animation.MaxFrameRate
            : animation.MinFrameRate;
        return frameRate > 0
            ? new StaticMeshTextureAnimation(animation.FrameUrls, frameRate)
            : null;
    }

    internal static StaticMeshWindMode WindMode(
        string meshName,
        StaticMeshMaterialBinding material)
    {
        var mesh = meshName.ToLowerInvariant();
        var name = material.Name.ToLowerInvariant();
        if (mesh.Contains("grass", StringComparison.Ordinal) ||
            mesh.Contains("reed", StringComparison.Ordinal))
            return StaticMeshWindMode.Grass;

        var foliageName = name.Contains("leaf", StringComparison.Ordinal) ||
            name.Contains("foliage", StringComparison.Ordinal) ||
            name.Contains("flower", StringComparison.Ordinal) ||
            name.Contains("plant", StringComparison.Ordinal);
        var translucent = material.BlendMode is StaticMeshBlendMode.Masked or StaticMeshBlendMode.AlphaBlend;
        if (foliageName && translucent) return StaticMeshWindMode.Foliage;
        if (mesh.Contains("tree", StringComparison.Ordinal) &&
            name.Contains("leaf", StringComparison.Ordinal) && translucent)
            return StaticMeshWindMode.Foliage;
        return StaticMeshWindMode.None;
    }

    private static StaticMeshBlendMode OutputBlendMode(byte value) => value switch
    {
        1 => StaticMeshBlendMode.Masked,
        2 => StaticMeshBlendMode.Modulate,
        3 => StaticMeshBlendMode.AlphaBlend,
        4 => StaticMeshBlendMode.Invisible,
        5 => StaticMeshBlendMode.Additive,
        6 => StaticMeshBlendMode.Modulate,
        _ => StaticMeshBlendMode.Opaque
    };

    private static StaticMeshBlendMode FrameBufferBlendMode(byte value) => value switch
    {
        1 => StaticMeshBlendMode.Modulate,
        2 or 3 or 4 => StaticMeshBlendMode.AlphaBlend,
        5 => StaticMeshBlendMode.Modulate,
        6 => StaticMeshBlendMode.Additive,
        7 => StaticMeshBlendMode.Invisible,
        _ => StaticMeshBlendMode.Opaque
    };

    private static TextureMaterialReference? Normalize(
        UnrealObjectReference? reference,
        string currentPackage) => reference is null
        ? null
        : new TextureMaterialReference(
            string.IsNullOrEmpty(reference.PackageName) ? currentPackage : reference.PackageName,
            reference.ObjectName,
            reference.ClassName);

    private static TextureMaterialReference? Normalize(
        TextureMaterialReference? reference,
        string currentPackage) => reference is null
        ? null
        : reference with
        {
            PackageName = string.IsNullOrEmpty(reference.PackageName) ? currentPackage : reference.PackageName
        };

    private static string Key(string packageName, string objectName) => $"{packageName}\n{objectName}";
}
