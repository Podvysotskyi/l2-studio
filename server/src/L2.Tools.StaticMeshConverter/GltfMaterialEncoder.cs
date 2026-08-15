namespace L2.Tools.StaticMeshConverter;

internal sealed class GltfMaterialEncoder
{
    private readonly List<object> images = [];
    private readonly List<object> textures = [];
    private readonly List<object> materials = [];
    private readonly Dictionary<string, int> textureIndices = new(StringComparer.Ordinal);

    public IReadOnlyList<object> Images => images;
    public IReadOnlyList<object> Textures => textures;
    public IReadOnlyList<object> Materials => materials;
    public IReadOnlyList<object> Samplers { get; } =
    [
        new { magFilter = 9729, minFilter = 9987, wrapS = 10497, wrapT = 10497 },
        new { magFilter = 9729, minFilter = 9987, wrapS = 33071, wrapT = 10497 },
        new { magFilter = 9729, minFilter = 9987, wrapS = 10497, wrapT = 33071 },
        new { magFilter = 9729, minFilter = 9987, wrapS = 33071, wrapT = 33071 }
    ];

    public int Add(StaticMeshMaterialBinding binding)
    {
        var pbr = new Dictionary<string, object?>
        {
            ["baseColorFactor"] = binding.BlendMode == StaticMeshBlendMode.Invisible
                ? new[] { binding.Tint?.R ?? 1f, binding.Tint?.G ?? 1f, binding.Tint?.B ?? 1f, 0f }
                : new[] { binding.Tint?.R ?? 1f, binding.Tint?.G ?? 1f, binding.Tint?.B ?? 1f, binding.Tint?.A ?? 1f },
            ["metallicFactor"] = 0f,
            ["roughnessFactor"] = 1f
        };
        if (binding.DiffuseUrl is not null)
        {
            pbr["baseColorTexture"] = new
            {
                index = TextureIndex(binding.DiffuseUrl, binding.ClampU, binding.ClampV)
            };
        }
        var material = new Dictionary<string, object?>
        {
            ["name"] = binding.Name,
            ["pbrMetallicRoughness"] = pbr,
            ["doubleSided"] = binding.DoubleSided,
            ["extras"] = new
            {
                l2 = new
                {
                    diffuseUrl = binding.DiffuseUrl,
                    emissiveUrl = binding.EmissiveUrl,
                    blendMode = binding.BlendMode.ToString().ToLowerInvariant(),
                    opacityUrl = binding.OpacityUrl,
                    opacitySource = binding.OpacitySource.ToString().ToLowerInvariant(),
                    opacityChannel = binding.OpacityChannel.ToString().ToLowerInvariant(),
                    unlit = binding.Unlit,
                    depthWrite = binding.DepthWrite,
                    depthTest = binding.DepthTest,
                    panRate = binding.PanRate,
                    panRateV = binding.PanRateV,
                    rotationRate = binding.RotationRate,
                    detailUrl = binding.DetailUrl,
                    detailScale = binding.DetailScale,
                    diffuseAnimation = Animation(binding.DiffuseAnimation),
                    opacityAnimation = Animation(binding.OpacityAnimation),
                    emissiveAnimation = Animation(binding.EmissiveAnimation),
                    uvOscillation = binding.UvOscillation,
                    fade = Fade(binding.Fade),
                    composite = Composite(binding.Composite),
                    selfIlluminationMaskUrl = binding.SelfIlluminationMaskUrl,
                    specularUrl = binding.SpecularUrl,
                    specularityMaskUrl = binding.SpecularityMaskUrl,
                    performLightingOnSpecularPass = binding.PerformLightingOnSpecularPass,
                    clampU = binding.ClampU,
                    clampV = binding.ClampV,
                    windMode = binding.WindMode == StaticMeshWindMode.None
                        ? null
                        : binding.WindMode.ToString().ToLowerInvariant()
                }
            }
        };
        if (binding.EmissiveUrl is not null)
        {
            material["emissiveTexture"] = new { index = TextureIndex(binding.EmissiveUrl) };
            material["emissiveFactor"] = new[] { 1f, 1f, 1f };
        }
        if (binding.BlendMode == StaticMeshBlendMode.Masked)
        {
            material["alphaMode"] = "MASK";
            material["alphaCutoff"] = binding.AlphaCutoff;
        }
        else if (binding.BlendMode != StaticMeshBlendMode.Opaque)
        {
            material["alphaMode"] = "BLEND";
        }
        materials.Add(material);
        return materials.Count - 1;
    }

    private int TextureIndex(string url, bool clampU = false, bool clampV = false)
    {
        var sampler = (clampU ? 1 : 0) + (clampV ? 2 : 0);
        var key = $"{sampler}\n{url}";
        if (textureIndices.TryGetValue(key, out var existing)) return existing;
        var index = textures.Count;
        images.Add(new { uri = url });
        textures.Add(new { sampler, source = index });
        textureIndices[key] = index;
        return index;
    }

    private static object? Animation(StaticMeshTextureAnimation? animation) => animation is null
        ? null
        : new { frameUrls = animation.FrameUrls, frameRate = animation.FrameRate };

    private static object? Tint(StaticMeshMaterialTint? tint) => tint is null
        ? null
        : new { r = tint.R, g = tint.G, b = tint.B, a = tint.A };

    private static object? Fade(StaticMeshMaterialFade? fade) => fade is null
        ? null
        : new
        {
            color1 = Tint(fade.Color1),
            color2 = Tint(fade.Color2),
            type = fade.Type,
            period = fade.Period,
            phase = fade.Phase
        };

    private static object? Composite(StaticMeshMaterialComposite? composite) => composite is null
        ? null
        : new
        {
            secondaryUrl = composite.SecondaryUrl,
            secondaryTint = Tint(composite.SecondaryTint),
            secondaryFade = Fade(composite.SecondaryFade),
            maskUrl = composite.MaskUrl,
            colorOperation = composite.ColorOperation,
            alphaOperation = composite.AlphaOperation,
            invertMask = composite.InvertMask,
            modulateScale = composite.ModulateScale
        };
}
