using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace L2.Tools.PackageReader;

public sealed partial class UnrealPackageReader
{
    public const uint PackageTag = 0x9e2a83c1;

    private const int TextureFormatDxt1 = 3;
    private const int TextureFormatRgba8 = 5;
    private const int TextureFormatDxt3 = 7;
    private const int TextureFormatDxt5 = 8;
    private const int TextureFormatG16 = 10;
    private const uint ObjectFlagHasStack = 0x02000000;
    private readonly byte[] data;
    private readonly List<string> names = [];
    private readonly List<ImportEntry> imports = [];
    private ushort packageVersion;
    private ushort licenseeVersion;

    public UnrealPackageReader(byte[] data)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public IReadOnlyList<UnrealTexture> ReadTextures() =>
        ReadTextureExports()
            .Where(export => export.Texture is not null)
            .Select(export => export.Texture!)
            .ToArray();

    public IReadOnlyList<UnrealTextureExport> ReadTextureExports()
    {
        var header = ReadHeader();
        ReadNames(header);
        ReadImports(header);
        var exports = ReadExports(header);
        var palettes = exports
            .Select((export, index) => (Export: export, Index: index + 1))
            .Where(item => string.Equals(
                ResolveClassName(item.Export.ClassIndex, exports),
                "Palette",
                StringComparison.OrdinalIgnoreCase))
            .ToDictionary(item => item.Index, item => ReadPalette(item.Export));
        var textures = new List<UnrealTextureExport>();

        foreach (var export in exports)
        {
            if (!string.Equals(ResolveClassName(export.ClassIndex, exports), "Texture", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var texture = ReadTexture(export, ResolveObjectPath(export, exports), palettes, exports);
            if (texture is not null)
            {
                textures.Add(texture);
            }
        }

        return textures;
    }

    public IReadOnlyList<UnrealStaticMesh> ReadStaticMeshes()
    {
        var header = ReadHeader();
        ReadNames(header);
        ReadImports(header);
        var exports = ReadExports(header);
        return exports
            .Where(export => string.Equals(
                ResolveClassName(export.ClassIndex, exports),
                "StaticMesh",
                StringComparison.OrdinalIgnoreCase))
            .Select(export => ReadStaticMesh(export, ResolveObjectPath(export, exports), exports))
            .ToArray();
    }

    public IReadOnlyList<UnrealMaterialExport> ReadMaterialExports()
    {
        var header = ReadHeader();
        ReadNames(header);
        ReadImports(header);
        var exports = ReadExports(header);
        return exports
            .Select(export => (Export: export, ClassName: ResolveClassName(export.ClassIndex, exports)))
            .Where(item => item.ClassName is "Shader" or "FinalBlend" or "Panner" or "Rotator" or "TexPanner" or "TexRotator" or "Combiner" or "TexOscillator" or "TexOscillatorTriggered" or "ColorModifier" or "FadeColor")
            .Select(item => TryReadMaterial(item.Export, item.ClassName, exports))
            .Where(material => material is not null)
            .Select(material => material!)
            .ToArray();
    }

    public IReadOnlyList<UnrealSoundExport> ReadSoundExports()
    {
        var header = ReadHeader();
        ReadNames(header);
        ReadImports(header);
        var exports = ReadExports(header);
        return exports
            .Where(export => string.Equals(
                ResolveClassName(export.ClassIndex, exports),
                "Sound",
                StringComparison.OrdinalIgnoreCase))
            .Select(export => ReadSound(export, ResolveObjectPath(export, exports)))
            .ToArray();
    }

    private UnrealSoundExport ReadSound(ExportEntry export, string name)
    {
        var start = export.SerialOffset;
        var end = checked(start + export.SerialSize);
        if (start < 0 || export.SerialSize <= 0 || end > data.Length)
            throw new InvalidDataException($"Sound '{name}' has an invalid serialized range.");
        var riff = -1;
        for (var position = start; position <= end - 12; position++)
        {
            if (data[position] == (byte)'R' && data[position + 1] == (byte)'I' &&
                data[position + 2] == (byte)'F' && data[position + 3] == (byte)'F' &&
                data[position + 8] == (byte)'W' && data[position + 9] == (byte)'A' &&
                data[position + 10] == (byte)'V' && data[position + 11] == (byte)'E')
            {
                riff = position;
                break;
            }
        }
        if (riff < 0) throw new InvalidDataException($"Sound '{name}' has no RIFF/WAVE payload.");
        var length = checked((int)BitConverter.ToUInt32(data, riff + 4) + 8);
        if (length < 44 || riff + length > end)
            throw new InvalidDataException($"Sound '{name}' has an invalid RIFF/WAVE length.");
        var format = FindWaveChunk(riff, length, "fmt "u8);
        var sampleData = FindWaveChunk(riff, length, "data"u8);
        if (format < 0 || sampleData < 0 || format + 16 > riff + length)
            throw new InvalidDataException($"Sound '{name}' has incomplete WAVE chunks.");
        var channels = BitConverter.ToUInt16(data, format + 8);
        var sampleRate = checked((int)BitConverter.ToUInt32(data, format + 12));
        var byteRate = BitConverter.ToUInt32(data, format + 16);
        var sampleBytes = BitConverter.ToUInt32(data, sampleData + 4);
        if (channels == 0 || sampleRate <= 0 || byteRate == 0)
            throw new InvalidDataException($"Sound '{name}' has invalid WAVE metadata.");
        return new UnrealSoundExport(
            name,
            data.AsSpan(riff, length).ToArray(),
            sampleRate,
            channels,
            sampleBytes / (double)byteRate);
    }

    private int FindWaveChunk(int riff, int length, ReadOnlySpan<byte> id)
    {
        var position = riff + 12;
        var end = riff + length;
        while (position <= end - 8)
        {
            var size = checked((int)BitConverter.ToUInt32(data, position + 4));
            if (data.AsSpan(position, 4).SequenceEqual(id)) return position;
            position = checked(position + 8 + size + (size & 1));
        }
        return -1;
    }

    private UnrealMaterialExport? TryReadMaterial(
        ExportEntry export,
        string className,
        IReadOnlyList<ExportEntry> exports)
    {
        try
        {
            return ReadMaterial(export, className, exports);
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    public UnrealLevel ReadLevel() => ReadSceneCore(includeSceneObjects: false).Level;

    public UnrealScene ReadScene() => ReadSceneCore(includeSceneObjects: true);

    private UnrealScene ReadSceneCore(bool includeSceneObjects)
    {
        var header = ReadHeader();
        ReadNames(header);
        ReadImports(header);
        var exports = ReadExports(header);
        var actors = new List<UnrealLevelActor>();
        var playerStarts = new List<UnrealPlayerStart>();
        var terrains = new List<UnrealTerrainInfo>();
        var lights = new List<UnrealLevelLight>();
        var waterVolumes = new List<UnrealWaterVolume>();
        var cameras = new List<UnrealSceneObject>();
        var interpolationPoints = new List<UnrealSceneObject>();
        var sceneManagers = new List<UnrealSceneObject>();
        var actions = new List<UnrealSceneObject>();
        var ambientSounds = new List<UnrealSceneObject>();
        var effects = new List<UnrealSceneObject>();
        var skyZones = new List<UnrealSkyZoneInfo>();
        var environmentCandidates = new List<(UnrealLevelEnvironment Environment, bool TerrainZone)>();
        var summaryCandidates = exports.Where(export =>
            export.PackageIndex == 0 &&
            string.Equals(
                ResolveClassName(export.ClassIndex, exports),
                "LevelSummary",
                StringComparison.OrdinalIgnoreCase)).ToArray();
        var unrepresented = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var exportIndex = 0; exportIndex < exports.Count; exportIndex++)
        {
            var export = exports[exportIndex];
            var className = ResolveClassName(export.ClassIndex, exports);
            if (string.Equals(className, "LevelSummary", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            var sceneCollection = SceneCollection(
                className,
                cameras,
                interpolationPoints,
                sceneManagers,
                actions,
                ambientSounds,
                effects);
            if (includeSceneObjects && sceneCollection is not null)
            {
                try
                {
                    var sceneProperties = ReadObjectProperties(export, exports, requireComplete: false);
                    sceneCollection.Add(SceneObject(exportIndex, export, className, exports, sceneProperties));
                }
                catch (InvalidDataException)
                {
                    unrepresented[className] = unrepresented.GetValueOrDefault(className) + 1;
                }

                continue;
            }

            if (!IsWorldObjectExport(export, exports))
            {
                continue;
            }

            var name = ResolveObjectPath(export, exports);
            if (string.Equals(className, "PlayerStart", StringComparison.OrdinalIgnoreCase))
            {
                ParsedObject playerStartProperties;
                try
                {
                    playerStartProperties = ReadObjectProperties(export, exports, requireComplete: false);
                }
                catch (InvalidDataException)
                {
                    unrepresented[className] = unrepresented.GetValueOrDefault(className) + 1;
                    continue;
                }
                if (Bool(playerStartProperties.Values, "bDeleteMe") ||
                    Bool(playerStartProperties.Values, "bPendingDelete"))
                {
                    continue;
                }
                playerStarts.Add(new UnrealPlayerStart(
                    name,
                    Vector(playerStartProperties.Values, "Location", Vector3.Zero),
                    Rotator(playerStartProperties.Values, "Rotation")));
                continue;
            }
            var supported = className is
                "TerrainInfo" or
                "LevelInfo" or
                "ZoneInfo" or
                "SkyZoneInfo" or
                "StaticMeshActor" or
                "MovableStaticMeshActor" or
                "WaterVolume" or
                "Light" or
                "NMovableSunLight" or
                "Sunlight";
            if (!supported)
            {
                if (!IsStructuralWorldClass(className))
                {
                    unrepresented[className] = unrepresented.GetValueOrDefault(className) + 1;
                }
                continue;
            }

            ParsedObject properties;
            try
            {
                properties = ReadObjectProperties(
                    export,
                    exports,
                    requireComplete: !string.Equals(className, "TerrainInfo", StringComparison.OrdinalIgnoreCase));
            }
            catch (InvalidDataException exception)
            {
                throw new InvalidDataException(
                    $"Level object '{name}' ({className}, flags 0x{export.ObjectFlags:x8}) " +
                    $"could not be decoded: {exception.Message}",
                    exception);
            }
            if (Bool(properties.Values, "bDeleteMe") || Bool(properties.Values, "bPendingDelete"))
            {
                continue;
            }
            if (string.Equals(className, "TerrainInfo", StringComparison.OrdinalIgnoreCase))
            {
                var terrainMap = Object(properties.Values, "TerrainMap");
                (UnrealCoordinateFrame toWorld, UnrealCoordinateFrame toHeightMap, bool derived) frames;
                try
                {
                    frames = ReadTerrainCoordinateFrames(properties, exports);
                }
                catch (InvalidDataException exception)
                {
                    throw new InvalidDataException(
                        $"TerrainInfo '{name}' map '{terrainMap?.Path ?? "none"}' failed native decoding " +
                        $"at {properties.NativeOffset} with {properties.NativeLength} bytes; " +
                        $"location {Vector(properties.Values, "Location", Vector3.Zero)}, " +
                        $"scale {Vector(properties.Values, "TerrainScale", Vector3.One)}: {exception.Message}",
                        exception);
                }
                terrains.Add(new UnrealTerrainInfo(
                    name,
                    Vector(properties.Values, "Location", Vector3.Zero),
                    Rotator(properties.Values, "Rotation"),
                    Vector(properties.Values, "TerrainScale", Vector3.One),
                    frames.toWorld,
                    frames.toHeightMap,
                    terrainMap,
                    TerrainLayers(properties.Values),
                    frames.derived));
                continue;
            }

            if (string.Equals(className, "SkyZoneInfo", StringComparison.OrdinalIgnoreCase))
            {
                var lensFlares = new List<UnrealSkyZoneLensFlare>();
                for (var index = 0; index < 12; index++)
                {
                    var suffix = index == 0 ? string.Empty : $"[{index}]";
                    var texture = Object(properties.Values, $"LensFlare{suffix}");
                    if (texture is null) continue;
                    lensFlares.Add(new UnrealSkyZoneLensFlare(
                        index,
                        texture,
                        Float(properties.Values, $"LensFlareOffset{suffix}", 0),
                        Float(properties.Values, $"LensFlareScale{suffix}", 1)));
                }
                skyZones.Add(new UnrealSkyZoneInfo(
                    exportIndex,
                    name,
                    Vector(properties.Values, "Location", Vector3.Zero),
                    Float(properties.Values, "DrawScale", 1),
                    Float(properties.Values, "TexUPanSpeed", 1),
                    Float(properties.Values, "TexVPanSpeed", 1),
                    lensFlares));
                continue;
            }

            if (className is "LevelInfo" or "ZoneInfo")
            {
                var hue = Byte(properties.Values, "AmbientHue", 0);
                var saturation = Byte(properties.Values, "AmbientSaturation", 255);
                var ambient = HsvColor(hue, saturation);
                UnrealDistanceFog? fog = null;
                if (Bool(properties.Values, "bDistanceFog") &&
                    properties.Values.GetValueOrDefault("DistanceFogColor") is UnrealColor fogColor)
                {
                    fog = new UnrealDistanceFog(
                        fogColor,
                        Float(properties.Values, "DistanceFogStart", 0),
                        Float(properties.Values, "DistanceFogEnd", 0));
                }
                environmentCandidates.Add((
                    new UnrealLevelEnvironment(
                        name,
                        className,
                        ambient,
                        Byte(properties.Values, "AmbientBrightness", 0) / 255f,
                        fog),
                    Bool(properties.Values, "bTerrainZone")));
                continue;
            }

            if (className is "StaticMeshActor" or "MovableStaticMeshActor")
            {
                var staticMeshInstance = Object(properties.Values, "StaticMeshInstance");
                var (vertexLighting, vertexLightingError) = ReadInstanceVertexLighting(
                    staticMeshInstance,
                    exports);
                actors.Add(new UnrealLevelActor(
                    name,
                    className,
                    Vector(properties.Values, "Location", Vector3.Zero),
                    Rotator(properties.Values, "Rotation"),
                    Vector(properties.Values, "PrePivot", Vector3.Zero),
                    Float(properties.Values, "DrawScale", 1),
                    Vector(properties.Values, "DrawScale3D", Vector3.One),
                    Object(properties.Values, "StaticMesh"),
                    staticMeshInstance,
                    vertexLighting,
                    vertexLightingError));
                continue;
            }

            if (string.Equals(className, "WaterVolume", StringComparison.OrdinalIgnoreCase))
            {
                var brush = Object(properties.Values, "Brush");
                UnrealBrushGeometry? geometry = null;
                string? error = null;
                try
                {
                    geometry = ReadBrushGeometry(brush, exports);
                }
                catch (Exception exception) when (exception is InvalidDataException or OverflowException)
                {
                    error = exception.Message;
                }
                waterVolumes.Add(new UnrealWaterVolume(
                    name,
                    className,
                    Vector(properties.Values, "Location", Vector3.Zero),
                    Rotator(properties.Values, "Rotation"),
                    Vector(properties.Values, "PrePivot", Vector3.Zero),
                    Float(properties.Values, "DrawScale", 1),
                    Vector(properties.Values, "DrawScale3D", Vector3.One),
                    brush,
                    geometry,
                    error));
                continue;
            }

            if (className is "Light" or "NMovableSunLight" or "Sunlight")
            {
                lights.Add(new UnrealLevelLight(
                    name,
                    className,
                    Vector(properties.Values, "Location", Vector3.Zero),
                    Rotator(properties.Values, "Rotation"),
                    Float(properties.Values, "LightBrightness", 64),
                    Byte(properties.Values, "LightHue", 0),
                    Byte(properties.Values, "LightSaturation", 255),
                    Float(properties.Values, "LightRadius", 64),
                    FormattedProperties(properties.Values)));
            }

        }

        var terrainZones = environmentCandidates.Where(candidate => candidate.TerrainZone).ToArray();
        var levelInfos = environmentCandidates
            .Where(candidate => candidate.Environment.SourceClass == "LevelInfo")
            .ToArray();
        var selectedEnvironment = terrainZones.Length == 1
            ? terrainZones[0].Environment
            : levelInfos.Length == 1 ? levelInfos[0].Environment : null;
        var environmentWarning = terrainZones.Length == 1 || (terrainZones.Length == 0 && levelInfos.Length == 1)
            ? null
            : $"Expected one active terrain zone but found {terrainZones.Length}.";
        var (summary, summaryWarning) = ReadLevelSummary(summaryCandidates, exports);

        return new UnrealScene(
            new UnrealLevel(
                actors,
                terrains,
                lights,
                waterVolumes,
                unrepresented,
                selectedEnvironment,
                environmentWarning,
                summary,
                summaryWarning,
                ReadBspModels(exports, skyZones, waterVolumes),
                skyZones,
                playerStarts),
            skyZones,
            ReadSkyBackdrops(exports),
            cameras,
            interpolationPoints,
            sceneManagers,
            actions,
            ambientSounds,
            ResolveEmitterOwners(effects));
    }

    private static IReadOnlyList<UnrealSceneObject> ResolveEmitterOwners(
        IReadOnlyList<UnrealSceneObject> effects)
    {
        var claims = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ambiguous = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var owner in effects.Where(effect => effect.ClassName == "Emitter"))
        {
            if (!owner.Properties.TryGetValue("Emitters", out var children)) continue;
            foreach (var reference in children.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var child = ObjectLeafName(reference);
                if (claims.TryGetValue(child, out var existing) &&
                    !string.Equals(existing, owner.Name, StringComparison.OrdinalIgnoreCase))
                {
                    ambiguous.Add(child);
                    continue;
                }
                claims[child] = owner.Name;
            }
        }

        return effects.Select(effect =>
        {
            if (effect.ClassName is not ("SpriteEmitter" or "BeamEmitter")) return effect;
            var child = ObjectLeafName(effect.Name);
            return effect with
            {
                Owner = !ambiguous.Contains(child) && claims.TryGetValue(child, out var owner)
                    ? owner
                    : null
            };
        }).ToArray();
    }

    private static string ObjectLeafName(string path)
    {
        var separator = path.LastIndexOf('.');
        return separator >= 0 ? path[(separator + 1)..] : path;
    }

    private static List<UnrealSceneObject>? SceneCollection(
        string className,
        List<UnrealSceneObject> cameras,
        List<UnrealSceneObject> interpolationPoints,
        List<UnrealSceneObject> sceneManagers,
        List<UnrealSceneObject> actions,
        List<UnrealSceneObject> ambientSounds,
        List<UnrealSceneObject> effects) => className switch
        {
            "Camera" => cameras,
            "InterpolationPoint" => interpolationPoints,
            "SceneManager" => sceneManagers,
            "ActionMoveCamera" or "ActionWarp" => actions,
            "AmbientSoundObject" => ambientSounds,
            "Emitter" or "SpriteEmitter" or "BeamEmitter" or "Projector" or "NSun" or "NMoon" => effects,
            _ => null
        };

    private UnrealSceneObject SceneObject(
        int order,
        ExportEntry export,
        string className,
        IReadOnlyList<ExportEntry> exports,
        ParsedObject parsed)
    {
        var target = Object(parsed.Values, "Target") ??
            Object(parsed.Values, "Camera") ??
            Object(parsed.Values, "Path") ??
            Object(parsed.Values, "Destination") ??
            Object(parsed.Values, "IntPoint");
        var duration = Float(parsed.Values, "Duration",
            Float(parsed.Values, "Time",
                Float(parsed.Values, "SceneTime", 0)));
        var owner = export.PackageIndex > 0
            ? ResolveObjectPath(exports[export.PackageIndex - 1], exports)
            : null;
        var properties = FormattedProperties(parsed.Values);
        return new UnrealSceneObject(
            order,
            ResolveObjectPath(export, exports),
            className,
            Vector(parsed.Values, "Location", Vector3.Zero),
            Rotator(parsed.Values, "Rotation"),
            duration,
            target,
            properties,
            owner);
    }

    private static IReadOnlyDictionary<string, string> FormattedProperties(
        IReadOnlyDictionary<string, object?> values) => values
            .Where(item => item.Value is not null)
            .ToDictionary(
                item => item.Key,
                item => FormatProperty(item.Value!),
                StringComparer.OrdinalIgnoreCase);

    private static string FormatProperty(object value) => value switch
    {
        Vector3 vector => $"{vector.X:R},{vector.Y:R},{vector.Z:R}",
        UnrealRotator rotation => $"{rotation.Pitch},{rotation.Yaw},{rotation.Roll}",
        UnrealRange range => $"{range.Min:R},{range.Max:R}",
        UnrealVectorRange range =>
            $"{range.Min.X:R},{range.Min.Y:R},{range.Min.Z:R};{range.Max.X:R},{range.Max.Y:R},{range.Max.Z:R}",
        IReadOnlyList<UnrealParticleColorScale> curve => string.Join(
            ";",
            curve.Select(key => $"{key.RelativeTime:R},{key.Color.Red},{key.Color.Green},{key.Color.Blue},{key.Color.Alpha}")),
        IReadOnlyList<UnrealParticleSizeScale> curve => string.Join(
            ";",
            curve.Select(key => $"{key.RelativeTime:R},{key.RelativeSize:R}")),
        IReadOnlyList<UnrealParticleBeamEndPoint> endpoints => string.Join(
            "|",
            endpoints.Select(endpoint =>
                $"{endpoint.Offset.Min.X:R},{endpoint.Offset.Min.Y:R},{endpoint.Offset.Min.Z:R};" +
                $"{endpoint.Offset.Max.X:R},{endpoint.Offset.Max.Y:R},{endpoint.Offset.Max.Z:R};" +
                $"{endpoint.Weight:R}")),
        UnrealObjectReference reference => reference.Path,
        IReadOnlyList<UnrealObjectReference?> references => string.Join(
            ",",
            references.Where(reference => reference is not null).Select(reference => reference!.Path)),
        float number => number.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    private bool IsWorldObjectExport(ExportEntry export, IReadOnlyList<ExportEntry> exports)
    {
        // UE2 map actors and their structural companions are top-level exports.
        // Some packages additionally nest them below the Level export.
        if (export.PackageIndex == 0) return true;
        var outer = export.PackageIndex;
        var remaining = exports.Count;
        while (outer > 0 && remaining-- > 0)
        {
            var parent = exports[outer - 1];
            if (string.Equals(ResolveClassName(parent.ClassIndex, exports), "Level", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            outer = parent.PackageIndex;
        }
        return false;
    }

    private static bool IsStructuralWorldClass(string className) => className is
        "Level" or "LevelSummary" or "Model" or "Polys" or "Brush" or "TerrainSector" or
        "StaticMeshInstance" or "ReachSpec";

    private (UnrealLevelSummary? Summary, string? Warning) ReadLevelSummary(
        IReadOnlyList<ExportEntry> candidates,
        IReadOnlyList<ExportEntry> exports)
    {
        if (candidates.Count == 0)
        {
            return (null, "No top-level LevelSummary export was found.");
        }

        if (candidates.Count != 1)
        {
            return (null, $"Expected one top-level LevelSummary export but found {candidates.Count}.");
        }

        var summary = candidates[0];
        try
        {
            var properties = ReadObjectProperties(summary, exports, requireComplete: true).Values;
            return (new UnrealLevelSummary(
                String(properties, "Title"),
                String(properties, "Author"),
                String(properties, "Description"),
                String(properties, "LevelEnterText"),
                String(properties, "ExtraInfo"),
                String(properties, "DecoTextName"),
                BoolValue(properties, "HideFromMenus"),
                Int(properties, "IdealPlayerCountMin"),
                Int(properties, "IdealPlayerCountMax"),
                Int(properties, "SinglePlayerTeamSize"),
                Object(properties, "Screenshot")), null);
        }
        catch (Exception exception) when (exception is InvalidDataException or OverflowException)
        {
            return (null, $"LevelSummary '{ResolveObjectPath(summary, exports)}' could not be decoded: {exception.Message}");
        }
    }

    private IReadOnlyList<UnrealSkyBackdrop> ReadSkyBackdrops(IReadOnlyList<ExportEntry> exports)
    {
        var result = new List<UnrealSkyBackdrop>();
        foreach (var decoded in ReadModels(exports))
        {
            if (decoded.Model is null)
            {
                result.Add(new UnrealSkyBackdrop(decoded.Name, null, decoded.Error));
                continue;
            }
            var backdrop = UnrealBspMeshBuilder.Build(
                decoded.Model,
                UnrealModelSurfaceSelection.FakeBackdrop);
            foreach (var chunk in backdrop.Chunks)
            {
                result.Add(new UnrealSkyBackdrop(chunk.Name, chunk.Mesh, null));
            }
        }
        return result;
    }

    private IReadOnlyList<UnrealBspModel> ReadBspModels(
        IReadOnlyList<ExportEntry> exports,
        IReadOnlyList<UnrealSkyZoneInfo> skyZones,
        IReadOnlyList<UnrealWaterVolume> waterVolumes) =>
        ReadModels(exports).Select(decoded => decoded.Model is null
            ? new UnrealBspModel(
                decoded.Name,
                [],
                new UnrealBspDiagnostics(0, 0, 0, 0, 0, 0),
                decoded.Error)
            : UnrealBspMeshBuilder.Build(
                decoded.Model,
                UnrealModelSurfaceSelection.World,
                skyZones,
                waterVolumes))
        .ToArray();

    private IReadOnlyList<DecodedModel> ReadModels(IReadOnlyList<ExportEntry> exports)
    {
        var result = new List<DecodedModel>();
        // ULevel's world model is the largest top-level UModel in the UE2 map.
        // Smaller top-level models back brushes, movers, and volumes and must not
        // be emitted as static world geometry.
        foreach (var model in exports.Where(export =>
                     export.PackageIndex == 0 &&
                     string.Equals(ResolveClassName(export.ClassIndex, exports), "Model", StringComparison.OrdinalIgnoreCase))
                 .OrderByDescending(export => export.SerialSize)
                 .ThenBy(export => export.SerialOffset)
                 .Take(1))
        {
            var name = ResolveObjectPath(model, exports);
            try
            {
                result.Add(new DecodedModel(name, ReadModel(model, exports, name), null));
            }
            catch (Exception exception) when (exception is InvalidDataException or OverflowException)
            {
                result.Add(new DecodedModel(name, null, exception.Message));
            }
        }
        return result;
    }

    private UnrealModelData ReadModel(
        ExportEntry model,
        IReadOnlyList<ExportEntry> exports,
        string name)
    {
        if (model.SerialSize <= 1 || model.SerialOffset < 0 ||
            model.SerialOffset + model.SerialSize > data.Length)
            throw new InvalidDataException($"BSP model '{name}' has an invalid serialized range.");

        // Lineage II packages exist with and without the four-byte per-surface
        // extension after UE2's light-map scale. Decode both layouts and select
        // the candidate whose node, surface, vertex, and point references are
        // structurally coherent.
        return UnrealModelSurfaceLayoutDecoder.DecodeBest(
            lineageSurfaceBytes =>
            {
                var nativeOffset = checked(model.SerialOffset + 1);
                var reader = new PackageCursor(data, nativeOffset, model.SerialSize - 1);
                reader.Skip(25 + 16);
                var vectors = ReadVectorArray(reader, "model vectors");
                var points = ReadVectorArray(reader, "model points");
                var nodes = ReadBrushNodes(reader);
                var surfaces = ReadBrushSurfaces(reader, exports, lineageSurfaceBytes);
                var vertices = ReadBrushVertices(reader);
                return new UnrealModelData(name, vectors, points, nodes, surfaces, vertices);
            },
            CompareModelLayouts);
    }

    private static int CompareModelLayouts(UnrealModelData left, UnrealModelData right)
    {
        var leftScore = ModelLayoutScore(left);
        var rightScore = ModelLayoutScore(right);
        var invalidComparison = rightScore.InvalidNodeCount.CompareTo(leftScore.InvalidNodeCount);
        return invalidComparison != 0
            ? invalidComparison
            : leftScore.ValidNodeCount.CompareTo(rightScore.ValidNodeCount);
    }

    private static (int InvalidNodeCount, int ValidNodeCount) ModelLayoutScore(UnrealModelData model)
    {
        var invalid = 0;
        var valid = 0;
        foreach (var node in model.Nodes.Where(node => node.VertexCount > 0))
        {
            if (node.Surface < 0 || node.Surface >= model.Surfaces.Count ||
                node.VertexCount < 3 ||
                node.VertexPool < 0 ||
                node.VertexPool > model.Vertices.Count - node.VertexCount ||
                !IsFinite(node.Normal) ||
                node.Normal.LengthSquared() <= 1e-8f)
            {
                invalid++;
                continue;
            }

            var surface = model.Surfaces[node.Surface];
            if (surface.BasePoint < 0 || surface.BasePoint >= model.Points.Count ||
                surface.TextureU < 0 || surface.TextureU >= model.Vectors.Count ||
                surface.TextureV < 0 || surface.TextureV >= model.Vectors.Count ||
                !IsFinite(model.Points[surface.BasePoint]) ||
                !IsFinite(model.Vectors[surface.TextureU]) ||
                !IsFinite(model.Vectors[surface.TextureV]) ||
                model.Vertices
                    .Skip(node.VertexPool)
                    .Take(node.VertexCount)
                    .Any(index => index < 0 || index >= model.Points.Count || !IsFinite(model.Points[index])))
            {
                invalid++;
                continue;
            }

            valid++;
        }

        return (invalid, valid);
    }

    private UnrealBrushGeometry ReadBrushGeometry(
        UnrealObjectReference? brush,
        IReadOnlyList<ExportEntry> exports)
    {
        if (brush is null)
        {
            throw new InvalidDataException("Water volume has no Brush model reference.");
        }
        var model = exports.FirstOrDefault(export =>
            string.Equals(ResolveObjectPath(export, exports), brush.ObjectName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(ResolveClassName(export.ClassIndex, exports), "Model", StringComparison.OrdinalIgnoreCase));
        if (model is null)
        {
            throw new InvalidDataException($"Brush model '{brush.Path}' was not found in the package.");
        }
        if (model.SerialSize <= 0 || model.SerialOffset < 0 || model.SerialOffset + model.SerialSize > data.Length)
        {
            throw new InvalidDataException($"Brush model '{brush.Path}' has an invalid serialized range.");
        }

        return UnrealModelSurfaceLayoutDecoder.Decode(lineageSurfaceBytes =>
        {
            // Model exports in these UE2 maps have an empty tagged-property block
            // (the one-byte `None` name) followed immediately by native UModel data.
            var nativeOffset = checked(model.SerialOffset + 1);
            var reader = new PackageCursor(data, nativeOffset, model.SerialSize - 1);
            try
            {
                reader.Skip(25 + 16); // UPrimitive bounding box and sphere.
                SkipVectorArray(reader, "brush vectors");
                var points = ReadVectorArray(reader, "brush points");
                var nodes = ReadBrushNodes(reader);
                SkipBrushSurfaces(reader, lineageSurfaceBytes);
                var vertices = ReadBrushVertices(reader);
                return BuildBrushGeometry(brush.Path, points, nodes, vertices);
            }
            catch (Exception exception) when (exception is InvalidDataException or OverflowException)
            {
                throw new InvalidDataException(
                    $"Brush model '{brush.Path}' failed near native offset {reader.Position - nativeOffset}: {exception.Message}",
                    exception);
            }
        });
    }

    private static void SkipVectorArray(PackageCursor reader, string description)
    {
        var count = ReadArrayCount(reader, 1_000_000, description);
        reader.Skip(checked(count * 12));
    }

    private static Vector3[] ReadVectorArray(PackageCursor reader, string description)
    {
        var count = ReadArrayCount(reader, 1_000_000, description);
        var result = new Vector3[count];
        for (var index = 0; index < count; index++)
        {
            result[index] = reader.ReadVector3();
            if (!IsFinite(result[index]))
            {
                throw new InvalidDataException($"{description} contains a non-finite coordinate.");
            }
        }
        return result;
    }

    private UnrealModelNode[] ReadBrushNodes(PackageCursor reader)
    {
        var count = ReadArrayCount(reader, 1_000_000, "brush BSP nodes");
        var result = new UnrealModelNode[count];
        for (var index = 0; index < count; index++)
        {
            var normal = reader.ReadVector3();
            var planeW = reader.ReadSingle();
            if (!IsFinite(normal)) throw new InvalidDataException("Brush BSP node has a non-finite plane.");
            _ = reader.ReadUInt64();
            _ = reader.ReadByte();
            var vertexPool = reader.ReadCompactIndex();
            var surface = reader.ReadCompactIndex();
            var back = reader.ReadCompactIndex();
            var front = reader.ReadCompactIndex();
            _ = reader.ReadCompactIndex(); // coplanar
            _ = reader.ReadCompactIndex(); // collision bound
            _ = reader.ReadCompactIndex(); // render bound
            reader.Skip(32); // exclusive and inclusive sphere bounds
            var backZone = reader.ReadByte();
            var frontZone = reader.ReadByte();
            var vertexCount = reader.ReadByte();
            reader.Skip(8); // leaves
            reader.Skip(12); // section, first vertex, lightmap
            result[index] = new UnrealModelNode(
                vertexPool,
                surface,
                vertexCount,
                normal,
                planeW,
                back,
                front,
                backZone,
                frontZone);
        }
        return result;
    }

    private static void SkipBrushSurfaces(PackageCursor reader, int lineageSurfaceBytes)
    {
        var count = ReadArrayCount(reader, 1_000_000, "brush BSP surfaces");
        for (var index = 0; index < count; index++)
        {
            _ = reader.ReadCompactIndex(); // material
            reader.Skip(4); // flags
            for (var field = 0; field < 6; field++) _ = reader.ReadCompactIndex();
            reader.Skip(16 + 4 + lineageSurfaceBytes); // plane, light-map scale, optional Lineage II field
        }
    }

    private UnrealModelSurface[] ReadBrushSurfaces(
        PackageCursor reader,
        IReadOnlyList<ExportEntry> exports,
        int lineageSurfaceBytes = 4)
    {
        var count = ReadArrayCount(reader, 1_000_000, "model BSP surfaces");
        var result = new UnrealModelSurface[count];
        for (var index = 0; index < count; index++)
        {
            var rawMaterialReference = reader.ReadCompactIndex();
            UnrealObjectReference? material = null;
            var materialReferenceInvalid = false;
            try
            {
                material = ResolveObjectReference(rawMaterialReference, exports);
            }
            catch (InvalidDataException) when (rawMaterialReference != 0)
            {
                materialReferenceInvalid = true;
            }
            var flags = reader.ReadUInt32();
            var basePoint = reader.ReadCompactIndex();
            var normalVector = reader.ReadCompactIndex();
            var textureU = reader.ReadCompactIndex();
            var textureV = reader.ReadCompactIndex();
            _ = reader.ReadCompactIndex(); // light map
            _ = reader.ReadCompactIndex(); // brush poly
            reader.Skip(16 + 4 + lineageSurfaceBytes);
            result[index] = new UnrealModelSurface(
                material,
                rawMaterialReference,
                materialReferenceInvalid,
                (UnrealPolyFlags)flags,
                basePoint,
                normalVector,
                textureU,
                textureV);
        }
        return result;
    }

    private static int[] ReadBrushVertices(PackageCursor reader)
    {
        var count = ReadArrayCount(reader, 1_000_000, "brush vertices");
        var result = new int[count];
        for (var index = 0; index < count; index++)
        {
            result[index] = reader.ReadCompactIndex();
            _ = reader.ReadCompactIndex();
        }
        return result;
    }

    private static UnrealBrushGeometry BuildBrushGeometry(
        string name,
        IReadOnlyList<Vector3> points,
        IReadOnlyList<UnrealModelNode> nodes,
        IReadOnlyList<int> vertices)
    {
        var faces = new List<UnrealBrushFace>();
        foreach (var node in nodes.Where(node => node.VertexCount > 0))
        {
            if (node.VertexCount < 3 || node.VertexPool < 0 || node.VertexPool + node.VertexCount > vertices.Count)
                throw new InvalidDataException($"Brush model '{name}' contains an invalid or degenerate face.");
            var pointIndices = new int[node.VertexCount];
            for (var index = 0; index < node.VertexCount; index++)
            {
                pointIndices[index] = vertices[node.VertexPool + index];
            }
            faces.Add(new UnrealBrushFace(pointIndices, node.Normal));
        }
        return UnrealBrushGeometryBuilder.Build(name, points, faces);
    }

    private sealed record DecodedModel(string Name, UnrealModelData? Model, string? Error);

    private ParsedObject ReadObjectProperties(
        ExportEntry export,
        IReadOnlyList<ExportEntry> exports,
        bool requireComplete,
        int? maximumBlocks = null)
    {
        if (export.SerialSize <= 0 || export.SerialOffset < 0 || export.SerialOffset + export.SerialSize > data.Length)
        {
            throw new InvalidDataException($"Object '{export.ObjectName}' has an invalid serialized range.");
        }

        var reader = new PackageCursor(data, export.SerialOffset, export.SerialSize);
        if ((export.ObjectFlags & ObjectFlagHasStack) != 0)
        {
            var node = reader.ReadCompactIndex();
            _ = reader.ReadCompactIndex(); // state node
            reader.Skip(8); // probe mask
            _ = reader.ReadInt32(); // latent action
            if (node != 0)
            {
                _ = reader.ReadCompactIndex(); // script code offset
            }
        }
        var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var blockCount = 0;
        while (reader.Remaining > 0)
        {
            if (!requireComplete && blockCount > 0 && IsTerrainNativeData(reader.Position, exports))
            {
                break;
            }

            var blockStart = reader.Position;
            Dictionary<string, object?> block;
            try
            {
                block = ReadPropertyBlock(reader, export, exports);
            }
            catch (InvalidDataException) when (!requireComplete && blockCount > 0)
            {
                reader.Seek(blockStart);
                break;
            }

            foreach (var property in block)
            {
                properties[property.Key] = property.Value;
            }
            blockCount++;
            if (maximumBlocks is not null && blockCount >= maximumBlocks) break;
        }

        if (blockCount == 0)
        {
            throw new InvalidDataException($"Object '{export.ObjectName}' has no property blocks.");
        }
        if (requireComplete && reader.Remaining != 0)
        {
            throw new InvalidDataException(
                $"Object '{export.ObjectName}' has {reader.Remaining} unread serialized bytes.");
        }

        return new ParsedObject(properties, reader.Position, reader.Remaining);
    }

    private bool IsTerrainNativeData(int offset, IReadOnlyList<ExportEntry> exports)
    {
        try
        {
            var reader = new PackageCursor(data, offset);
            var sectorCount = reader.ReadCompactIndex();
            if (sectorCount is < 1 or > 4096)
            {
                return false;
            }

            for (var index = 0; index < sectorCount; index++)
            {
                var reference = reader.ReadCompactIndex();
                if (reference <= 0 || reference > exports.Count ||
                    !string.Equals(
                        ResolveClassName(exports[reference - 1].ClassIndex, exports),
                        "TerrainSector",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            var sectorsX = reader.ReadInt32();
            var sectorsY = reader.ReadInt32();
            if (sectorsX <= 0 || sectorsY <= 0 || (long)sectorsX * sectorsY != sectorCount)
            {
                return false;
            }

            return IsFinite(reader.ReadVector3()) &&
                IsFinite(reader.ReadVector3()) &&
                IsFinite(reader.ReadVector3()) &&
                IsFinite(reader.ReadVector3());
        }
        catch (Exception exception) when (exception is InvalidDataException or OverflowException)
        {
            return false;
        }
    }

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    private Dictionary<string, object?> ReadPropertyBlock(
        PackageCursor reader,
        ExportEntry export,
        IReadOnlyList<ExportEntry> exports)
    {
        var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        while (true)
        {
            var propertyName = ReadName(reader);
            if (string.Equals(propertyName, "None", StringComparison.OrdinalIgnoreCase))
            {
                return properties;
            }

            var info = reader.ReadByte();
            var type = info & 0x0f;
            string? structName = null;
            if (type == 10)
            {
                structName = ReadName(reader);
            }

            var size = ReadPropertySize(reader, (info >> 4) & 7);
            if (type == 3)
            {
                properties[propertyName] = (info & 0x80) != 0;
                continue;
            }

            int? arrayIndex = null;
            if ((info & 0x80) != 0)
            {
                arrayIndex = reader.ReadArrayIndex();
            }

            var start = reader.Position;
            object? value = type switch
            {
                1 when size == 1 => reader.ReadByte(),
                2 when size == 4 => reader.ReadInt32(),
                4 when size == 4 => reader.ReadSingle(),
                5 => ResolveObjectReference(reader.ReadCompactIndex(), exports),
                6 => ReadName(reader),
                9 when string.Equals(propertyName, "Actions", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(propertyName, "Emitters", StringComparison.OrdinalIgnoreCase) =>
                    ReadObjectReferenceArray(reader, size, exports),
                9 when string.Equals(propertyName, "ColorScale", StringComparison.OrdinalIgnoreCase) =>
                    ReadParticleColorScale(reader, size, export, exports),
                9 when string.Equals(propertyName, "SizeScale", StringComparison.OrdinalIgnoreCase) =>
                    ReadParticleSizeScale(reader, size, export, exports),
                9 when string.Equals(propertyName, "BeamEndPoints", StringComparison.OrdinalIgnoreCase) =>
                    ReadParticleBeamEndPoints(reader, size, export, exports),
                10 when size == 12 && string.Equals(structName, "Vector", StringComparison.OrdinalIgnoreCase) =>
                    reader.ReadVector3(),
                10 when size == 12 && string.Equals(structName, "Rotator", StringComparison.OrdinalIgnoreCase) =>
                    new UnrealRotator(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()),
                10 when size == 4 && string.Equals(structName, "Color", StringComparison.OrdinalIgnoreCase) =>
                    ReadColor(reader),
                10 when string.Equals(structName, "Range", StringComparison.OrdinalIgnoreCase) =>
                    ReadRange(reader, size, export, exports),
                10 when string.Equals(structName, "RangeVector", StringComparison.OrdinalIgnoreCase) =>
                    ReadVectorRange(reader, size, export, exports),
                10 when string.Equals(structName, "TerrainLayer", StringComparison.OrdinalIgnoreCase) =>
                    ReadTerrainLayer(reader, size, arrayIndex ?? 0, export, exports),
                11 when size == 12 => reader.ReadVector3(),
                12 when size == 12 => new UnrealRotator(
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadInt32()),
                13 => reader.ReadUnrealString(),
                _ => null
            };
            var consumed = reader.Position - start;
            if (consumed > size)
            {
                throw new InvalidDataException(
                    $"Object '{export.ObjectName}' property '{propertyName}' exceeded its encoded size.");
            }
            reader.Skip(size - consumed);
            properties[arrayIndex is null ? propertyName : $"{propertyName}[{arrayIndex.Value}]"] = value;
        }
    }

    private UnrealTerrainLayer ReadTerrainLayer(
        PackageCursor reader,
        int size,
        int index,
        ExportEntry export,
        IReadOnlyList<ExportEntry> exports)
    {
        var start = reader.Position;
        var layerReader = new PackageCursor(data, start, size);
        var values = ReadPropertyBlock(layerReader, export, exports);
        while (layerReader.Remaining > 0 && layerReader.PeekByte() == 0)
        {
            _ = layerReader.ReadByte();
        }
        if (layerReader.Remaining != 0)
        {
            throw new InvalidDataException(
                $"Terrain layer {index} has {layerReader.Remaining} unread serialized bytes.");
        }
        reader.Seek(checked(start + size));
        return new UnrealTerrainLayer(
            index,
            Object(values, "Texture"),
            Object(values, "AlphaMap"),
            Float(values, "UScale", 1),
            Float(values, "VScale", 1),
            Float(values, "UPan", 0),
            Float(values, "VPan", 0),
            Byte(values, "TextureMapAxis", 0),
            Float(values, "TextureRotation", 0),
            Rotator(values, "LayerRotation"));
    }

    private IReadOnlyList<UnrealObjectReference?> ReadObjectReferenceArray(
        PackageCursor reader,
        int size,
        IReadOnlyList<ExportEntry> exports)
    {
        var start = reader.Position;
        var count = reader.ReadCompactIndex();
        if (count is < 0 or > 65_536)
        {
            throw new InvalidDataException($"Object-reference array count {count} is invalid.");
        }

        var values = new UnrealObjectReference?[count];
        for (var index = 0; index < count; index++)
        {
            values[index] = ResolveObjectReference(reader.ReadCompactIndex(), exports);
        }

        if (reader.Position - start > size)
        {
            throw new InvalidDataException("Object-reference array exceeded its encoded size.");
        }

        return values;
    }

    private IReadOnlyList<UnrealParticleColorScale> ReadParticleColorScale(
        PackageCursor reader,
        int size,
        ExportEntry export,
        IReadOnlyList<ExportEntry> exports)
    {
        var start = reader.Position;
        try
        {
            var curveReader = new PackageCursor(data, start, size);
            var count = curveReader.ReadCompactIndex();
            if (count is < 0 or > 1024)
                throw new InvalidDataException($"Particle color-curve count {count} is invalid.");
            var result = new UnrealParticleColorScale[count];
            for (var index = 0; index < count; index++)
            {
                var values = ReadPropertyBlock(curveReader, export, exports);
                var time = Float(values, "RelativeTime", 0);
                var color = values.GetValueOrDefault("Color") is UnrealColor value
                    ? value
                    : new UnrealColor(255, 255, 255, 255);
                result[index] = new UnrealParticleColorScale(time, color);
            }
            reader.Seek(checked(start + size));
            return result;
        }
        catch (Exception exception) when (exception is InvalidDataException or OverflowException)
        {
            reader.Seek(checked(start + size));
            return [];
        }
    }

    private IReadOnlyList<UnrealParticleSizeScale> ReadParticleSizeScale(
        PackageCursor reader,
        int size,
        ExportEntry export,
        IReadOnlyList<ExportEntry> exports)
    {
        var start = reader.Position;
        try
        {
            var curveReader = new PackageCursor(data, start, size);
            var count = curveReader.ReadCompactIndex();
            if (count is < 0 or > 1024)
                throw new InvalidDataException($"Particle size-curve count {count} is invalid.");
            var result = new UnrealParticleSizeScale[count];
            for (var index = 0; index < count; index++)
            {
                var values = ReadPropertyBlock(curveReader, export, exports);
                result[index] = new UnrealParticleSizeScale(
                    Float(values, "RelativeTime", 0),
                    Float(values, "RelativeSize", 1));
            }
            reader.Seek(checked(start + size));
            return result;
        }
        catch (Exception exception) when (exception is InvalidDataException or OverflowException)
        {
            reader.Seek(checked(start + size));
            return [];
        }
    }

    private IReadOnlyList<UnrealParticleBeamEndPoint> ReadParticleBeamEndPoints(
        PackageCursor reader,
        int size,
        ExportEntry export,
        IReadOnlyList<ExportEntry> exports)
    {
        var start = reader.Position;
        try
        {
            var endpointReader = new PackageCursor(data, start, size);
            var count = endpointReader.ReadCompactIndex();
            if (count is < 0 or > 1024)
                throw new InvalidDataException($"Particle beam-endpoint count {count} is invalid.");
            var result = new UnrealParticleBeamEndPoint[count];
            for (var index = 0; index < count; index++)
            {
                var values = ReadPropertyBlock(endpointReader, export, exports);
                var actorTag = values.GetValueOrDefault("ActorTag")?.ToString() ?? string.Empty;
                var offset = values.GetValueOrDefault("Offset") is UnrealVectorRange value
                    ? value
                    : new UnrealVectorRange(Vector3.Zero, Vector3.Zero);
                result[index] = new UnrealParticleBeamEndPoint(
                    actorTag,
                    offset,
                    Math.Max(Float(values, "Weight", 1), 0));
            }
            reader.Seek(checked(start + size));
            return result;
        }
        catch (Exception exception) when (exception is InvalidDataException or OverflowException)
        {
            reader.Seek(checked(start + size));
            return [];
        }
    }

    private (UnrealCoordinateFrame ToWorld, UnrealCoordinateFrame ToHeightMap, bool Derived) ReadTerrainCoordinateFrames(
        ParsedObject parsed,
        IReadOnlyList<ExportEntry> exports)
    {
        if (parsed.NativeLength <= 0)
        {
            throw new InvalidDataException("TerrainInfo has no native coordinate-frame data.");
        }

        var nativeOffset = parsed.NativeOffset;
        if (!IsTerrainNativeData(nativeOffset, exports))
        {
            var end = checked(parsed.NativeOffset + parsed.NativeLength);
            nativeOffset = Enumerable.Range(parsed.NativeOffset + 1, Math.Max(end - parsed.NativeOffset - 1, 0))
                .FirstOrDefault(offset => IsTerrainNativeData(offset, exports));
            if (nativeOffset == 0)
            {
                if (!HasTerrainSectorReferences(parsed.NativeOffset, exports))
                    throw new InvalidDataException("TerrainInfo native coordinate-frame data was not found.");
                return DerivedTerrainCoordinateFrames(parsed.Values);
            }
        }

        var reader = new PackageCursor(data, nativeOffset, checked(parsed.NativeOffset + parsed.NativeLength - nativeOffset));
        var sectorCount = reader.ReadCompactIndex();
        if (sectorCount is < 1 or > 4096)
        {
            throw new InvalidDataException($"TerrainInfo has an invalid sector count {sectorCount}.");
        }
        for (var index = 0; index < sectorCount; index++)
        {
            _ = reader.ReadCompactIndex();
        }

        var sectorsX = reader.ReadInt32();
        var sectorsY = reader.ReadInt32();
        if (sectorsX <= 0 || sectorsY <= 0 || (long)sectorsX * sectorsY != sectorCount)
        {
            throw new InvalidDataException(
                $"TerrainInfo sector grid {sectorsX}x{sectorsY} does not match {sectorCount} sectors.");
        }

        var toWorld = new UnrealCoordinateFrame(
            reader.ReadVector3(),
            reader.ReadVector3(),
            reader.ReadVector3(),
            reader.ReadVector3());
        var toHeightMap = new UnrealCoordinateFrame(
            reader.ReadVector3(),
            reader.ReadVector3(),
            reader.ReadVector3(),
            reader.ReadVector3());
        if (!IsFinite(toWorld.Origin) || !IsFinite(toWorld.XAxis) ||
            !IsFinite(toWorld.YAxis) || !IsFinite(toWorld.ZAxis) ||
            !IsFinite(toHeightMap.Origin) || !IsFinite(toHeightMap.XAxis) ||
            !IsFinite(toHeightMap.YAxis) || !IsFinite(toHeightMap.ZAxis))
        {
            throw new InvalidDataException("TerrainInfo contains a non-finite coordinate frame.");
        }
        return (toWorld, toHeightMap, false);
    }

    private bool HasTerrainSectorReferences(int offset, IReadOnlyList<ExportEntry> exports)
    {
        try
        {
            var reader = new PackageCursor(data, offset);
            var count = reader.ReadCompactIndex();
            if (count is < 1 or > 4096) return false;
            for (var index = 0; index < count; index++)
            {
                var reference = reader.ReadCompactIndex();
                if (reference <= 0 || reference > exports.Count ||
                    !string.Equals(
                        ResolveClassName(exports[reference - 1].ClassIndex, exports),
                        "TerrainSector",
                        StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }
        catch (Exception exception) when (exception is InvalidDataException or OverflowException)
        {
            return false;
        }
    }

    private static (UnrealCoordinateFrame ToWorld, UnrealCoordinateFrame ToHeightMap, bool Derived)
        DerivedTerrainCoordinateFrames(IReadOnlyDictionary<string, object?> values)
    {
        const float heightCenter = 32767;
        const float terrainHalfSize = 128;
        var location = Vector(values, "Location", Vector3.Zero);
        var scale = Vector(values, "TerrainScale", Vector3.One);
        var zScale = scale.Z / 256f;
        if (!IsFinite(location) || !IsFinite(scale) ||
            scale.X == 0 || scale.Y == 0 || zScale == 0)
            throw new InvalidDataException("TerrainInfo cannot derive finite coordinate frames from its transform.");
        var toWorld = new UnrealCoordinateFrame(
            new Vector3(
                terrainHalfSize - location.X / scale.X,
                terrainHalfSize - location.Y / scale.Y,
                heightCenter - location.Z / zScale),
            new Vector3(scale.X, 0, 0),
            new Vector3(0, scale.Y, 0),
            new Vector3(0, 0, zScale));
        var toHeightMap = new UnrealCoordinateFrame(
            toWorld.TransformPoint(Vector3.Zero),
            new Vector3(1 / scale.X, 0, 0),
            new Vector3(0, 1 / scale.Y, 0),
            new Vector3(0, 0, 1 / zScale));
        return (toWorld, toHeightMap, true);
    }

    private UnrealObjectReference? ResolveObjectReference(int index, IReadOnlyList<ExportEntry> exports)
    {
        if (index == 0)
        {
            return null;
        }

        if (index > 0)
        {
            var exportIndex = index - 1;
            if (exportIndex < 0 || exportIndex >= exports.Count)
            {
                throw new InvalidDataException($"Object reference {index} is outside the export table.");
            }
            var export = exports[exportIndex];
            return new UnrealObjectReference(
                string.Empty,
                ResolveObjectPath(export, exports),
                ResolveClassName(export.ClassIndex, exports));
        }

        var importIndex = -index - 1;
        if (importIndex < 0 || importIndex >= imports.Count)
        {
            throw new InvalidDataException($"Object reference {index} is outside the import table.");
        }

        var import = imports[importIndex];
        var segments = new List<string> { import.ObjectName };
        var outer = import.PackageIndex;
        var remaining = imports.Count;
        while (outer < 0 && remaining-- > 0)
        {
            var outerIndex = -outer - 1;
            if (outerIndex < 0 || outerIndex >= imports.Count)
            {
                throw new InvalidDataException($"Import '{import.ObjectName}' has an invalid outer reference.");
            }
            var outerImport = imports[outerIndex];
            segments.Add(outerImport.ObjectName);
            outer = outerImport.PackageIndex;
        }
        segments.Reverse();
        var packageName = segments.Count > 1 ? segments[0] : string.Empty;
        var objectName = string.Join('.', segments.Skip(packageName.Length == 0 ? 0 : 1));
        return new UnrealObjectReference(packageName, objectName, import.ClassName);
    }

    private (IReadOnlyList<UnrealColor> Colors, string? Error) ReadInstanceVertexLighting(
        UnrealObjectReference? reference,
        IReadOnlyList<ExportEntry> exports)
    {
        if (reference is null) return ([], null);
        try
        {
            var export = exports.SingleOrDefault(candidate => string.Equals(
                ResolveObjectPath(candidate, exports),
                reference.ObjectName,
                StringComparison.OrdinalIgnoreCase));
            if (export is null || !string.Equals(
                ResolveClassName(export.ClassIndex, exports),
                "StaticMeshInstance",
                StringComparison.OrdinalIgnoreCase))
            {
                return ([], $"StaticMeshInstance '{reference.Path}' was not found.");
            }
            var reader = new PackageCursor(data, export.SerialOffset, export.SerialSize);
            if ((export.ObjectFlags & ObjectFlagHasStack) != 0)
            {
                var node = reader.ReadCompactIndex();
                _ = reader.ReadCompactIndex();
                reader.Skip(12);
                if (node != 0) _ = reader.ReadCompactIndex();
            }
            _ = ReadPropertyBlock(reader, export, exports);
            var colors = ReadCompactColorArray(reader, "static-mesh instance lighting");
            return (colors, null);
        }
        catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException or OverflowException)
        {
            return ([], $"StaticMeshInstance '{reference.Path}' lighting was ignored: {exception.Message}");
        }
    }

    private static IReadOnlyList<UnrealColor> ReadCompactColorArray(PackageCursor reader, string description)
    {
        var count = ReadArrayCount(reader, 10_000_000, description);
        var colors = new UnrealColor[count];
        for (var index = 0; index < count; index++) colors[index] = ReadColor(reader);
        return colors;
    }

    private static Vector3 Vector(IReadOnlyDictionary<string, object?> properties, string name, Vector3 fallback) =>
        properties.TryGetValue(name, out var value) && value is Vector3 vector ? vector : fallback;

    private static UnrealRotator Rotator(IReadOnlyDictionary<string, object?> properties, string name) =>
        properties.TryGetValue(name, out var value) && value is UnrealRotator rotator ? rotator : default;

    private static float Float(IReadOnlyDictionary<string, object?> properties, string name, float fallback) =>
        properties.TryGetValue(name, out var value) && value is float number ? number : fallback;

    private static int? Int(IReadOnlyDictionary<string, object?> properties, string name) =>
        properties.TryGetValue(name, out var value) && value is int number ? number : null;

    private static string? String(IReadOnlyDictionary<string, object?> properties, string name) =>
        properties.TryGetValue(name, out var value) ? value as string : null;

    private static bool? BoolValue(IReadOnlyDictionary<string, object?> properties, string name) =>
        properties.TryGetValue(name, out var value) && value is bool flag ? flag : null;

    private static UnrealObjectReference? Object(IReadOnlyDictionary<string, object?> properties, string name) =>
        properties.TryGetValue(name, out var value) ? value as UnrealObjectReference : null;

    private static byte Byte(IReadOnlyDictionary<string, object?> properties, string name, byte fallback) =>
        properties.TryGetValue(name, out var value) && value is byte number ? number : fallback;

    private static UnrealColor ReadColor(PackageCursor reader)
    {
        var blue = reader.ReadByte();
        var green = reader.ReadByte();
        var red = reader.ReadByte();
        var alpha = reader.ReadByte();
        return new UnrealColor(red, green, blue, alpha);
    }

    private UnrealRange ReadRange(
        PackageCursor reader,
        int size,
        ExportEntry export,
        IReadOnlyList<ExportEntry> exports)
    {
        if (size == 8) return new UnrealRange(reader.ReadSingle(), reader.ReadSingle());
        var values = ReadTaggedStructProperties(reader, size, export, exports, "Range");
        return new UnrealRange(Float(values, "Min", 0), Float(values, "Max", 0));
    }

    private UnrealVectorRange ReadVectorRange(
        PackageCursor reader,
        int size,
        ExportEntry export,
        IReadOnlyList<ExportEntry> exports)
    {
        if (size != 24)
        {
            var values = ReadTaggedStructProperties(reader, size, export, exports, "RangeVector");
            var x = values.GetValueOrDefault("X") is UnrealRange xRange ? xRange : default;
            var y = values.GetValueOrDefault("Y") is UnrealRange yRange ? yRange : default;
            var z = values.GetValueOrDefault("Z") is UnrealRange zRange ? zRange : default;
            return new UnrealVectorRange(
                new Vector3(x.Min, y.Min, z.Min),
                new Vector3(x.Max, y.Max, z.Max));
        }
        var rawX = new UnrealRange(reader.ReadSingle(), reader.ReadSingle());
        var rawY = new UnrealRange(reader.ReadSingle(), reader.ReadSingle());
        var rawZ = new UnrealRange(reader.ReadSingle(), reader.ReadSingle());
        return new UnrealVectorRange(
            new Vector3(rawX.Min, rawY.Min, rawZ.Min),
            new Vector3(rawX.Max, rawY.Max, rawZ.Max));
    }

    private Dictionary<string, object?> ReadTaggedStructProperties(
        PackageCursor reader,
        int size,
        ExportEntry export,
        IReadOnlyList<ExportEntry> exports,
        string structName)
    {
        var start = reader.Position;
        var structReader = new PackageCursor(data, start, size);
        var values = ReadPropertyBlock(structReader, export, exports);
        while (structReader.Remaining > 0 && structReader.PeekByte() == 0)
        {
            _ = structReader.ReadByte();
        }
        if (structReader.Remaining != 0)
        {
            throw new InvalidDataException(
                $"Object '{export.ObjectName}' {structName} has {structReader.Remaining} unread serialized bytes.");
        }
        reader.Seek(checked(start + size));
        return values;
    }

    private static UnrealColor HsvColor(byte hue, byte saturation)
    {
        var h = hue / 255f * 6f;
        var s = 1f - saturation / 255f;
        var sector = (int)MathF.Floor(h) % 6;
        var fraction = h - MathF.Floor(h);
        var p = 1f - s;
        var q = 1f - fraction * s;
        var t = 1f - (1f - fraction) * s;
        var (r, g, b) = sector switch
        {
            0 => (1f, t, p),
            1 => (q, 1f, p),
            2 => (p, 1f, t),
            3 => (p, q, 1f),
            4 => (t, p, 1f),
            _ => (1f, p, q)
        };
        return new UnrealColor((byte)MathF.Round(r * 255), (byte)MathF.Round(g * 255), (byte)MathF.Round(b * 255), 255);
    }

    private static bool Bool(IReadOnlyDictionary<string, object?> properties, string name) =>
        properties.TryGetValue(name, out var value) && value is true;

    private static IReadOnlyList<UnrealTerrainLayer> TerrainLayers(
        IReadOnlyDictionary<string, object?> properties) => properties
        .Where(item => (string.Equals(item.Key, "Layers", StringComparison.OrdinalIgnoreCase) ||
                item.Key.StartsWith("Layers[", StringComparison.OrdinalIgnoreCase)) &&
            item.Value is UnrealTerrainLayer)
        .Select(item => (UnrealTerrainLayer)item.Value!)
        .Where(layer => layer.Texture is not null || layer.AlphaMap is not null)
        .OrderBy(layer => layer.Index)
        .ToArray();

    private UnrealStaticMesh ReadStaticMesh(
        ExportEntry export,
        string objectPath,
        IReadOnlyList<ExportEntry> exports)
    {
        if (export.SerialSize <= 0 || export.SerialOffset < 0 || export.SerialOffset + export.SerialSize > data.Length)
        {
            throw new InvalidDataException($"Static mesh '{objectPath}' has an invalid serialized range.");
        }

        var reader = new PackageCursor(data, export.SerialOffset, export.SerialSize);
        var materials = ReadStaticMeshMaterials(reader, export, exports);
        reader.Skip(25 + 16); // UPrimitive bounding box and sphere.

        var sectionCount = ReadArrayCount(reader, 65_536, "static mesh sections");
        var sections = new UnrealStaticMeshSection[sectionCount];
        for (var index = 0; index < sectionCount; index++)
        {
            _ = reader.ReadInt32();
            var material = index < materials.Count
                ? materials[index]
                : null;
            var firstIndex = reader.ReadUInt16();
            _ = reader.ReadUInt16();
            _ = reader.ReadUInt16();
            _ = reader.ReadUInt16();
            var faceCount = reader.ReadUInt16();
            sections[index] = new UnrealStaticMeshSection(
                firstIndex,
                checked(faceCount * 3),
                material);
        }

        reader.Skip(25); // Bounding box serialized a second time by UStaticMesh.
        var vertexCount = ReadArrayCount(reader, 10_000_000, "static mesh vertices");
        var positions = new Vector3[vertexCount];
        var normals = new Vector3[vertexCount];
        for (var index = 0; index < vertexCount; index++)
        {
            positions[index] = reader.ReadVector3();
            normals[index] = reader.ReadVector3();
        }
        _ = reader.ReadInt32(); // vertex stream revision

        var colorStream0 = ReadColorStream(reader);
        var colorStream1 = ReadColorStream(reader);

        var uvStreamCount = ReadArrayCount(reader, 64, "static mesh UV streams");
        Vector2[] textureCoordinates = [];
        for (var streamIndex = 0; streamIndex < uvStreamCount; streamIndex++)
        {
            var coordinateCount = ReadArrayCount(reader, 10_000_000, "static mesh UV coordinates");
            var coordinates = new Vector2[coordinateCount];
            for (var index = 0; index < coordinateCount; index++)
            {
                coordinates[index] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            }
            _ = reader.ReadInt32();
            _ = reader.ReadInt32();
            if (streamIndex == 0)
            {
                textureCoordinates = coordinates;
            }
        }

        var indices = ReadIndexStream(reader);
        _ = ReadIndexStream(reader);
        _ = reader.ReadCompactIndex(); // collision object reference

        if (textureCoordinates.Length != 0 && textureCoordinates.Length != positions.Length)
        {
            Array.Resize(ref textureCoordinates, positions.Length);
        }
        if (indices.Any(index => index >= positions.Length))
        {
            throw new InvalidDataException($"Static mesh '{objectPath}' contains an out-of-range vertex index.");
        }

        return new UnrealStaticMesh(
            objectPath,
            positions,
            normals,
            textureCoordinates,
            indices,
            sections,
            colorStream0.Count == positions.Length ? colorStream0 : [],
            colorStream1.Count == positions.Length ? colorStream1 : []);
    }

    private IReadOnlyList<UnrealObjectReference?> ReadStaticMeshMaterials(
        PackageCursor reader,
        ExportEntry export,
        IReadOnlyList<ExportEntry> exports)
    {
        IReadOnlyList<UnrealObjectReference?> materials = [];
        while (true)
        {
            var propertyName = ReadName(reader);
            if (string.Equals(propertyName, "None", StringComparison.OrdinalIgnoreCase))
            {
                return materials;
            }

            var info = reader.ReadByte();
            var type = info & 0x0f;
            if (type == 10)
            {
                _ = ReadName(reader);
            }

            var size = ReadPropertySize(reader, (info >> 4) & 7);
            if (type != 3 && (info & 0x80) != 0)
            {
                _ = reader.ReadArrayIndex();
            }

            var start = reader.Position;
            if (type == 9 && string.Equals(propertyName, "Materials", StringComparison.OrdinalIgnoreCase))
            {
                materials = ReadStaticMeshMaterialArray(reader, size, export, exports);
            }

            reader.Seek(checked(start + (type == 3 ? 0 : size)));
            if (reader.Position > export.SerialOffset + export.SerialSize)
            {
                throw new InvalidDataException($"Static mesh '{export.ObjectName}' properties exceed its serialized range.");
            }
        }
    }

    private IReadOnlyList<UnrealObjectReference?> ReadStaticMeshMaterialArray(
        PackageCursor reader,
        int size,
        ExportEntry export,
        IReadOnlyList<ExportEntry> exports)
    {
        var start = reader.Position;
        var count = reader.ReadCompactIndex();
        if (count is < 0 or > 65_536)
        {
            throw new InvalidDataException($"Static mesh material count {count} is invalid.");
        }
        var materials = new UnrealObjectReference?[count];
        for (var index = 0; index < count; index++)
        {
            var values = ReadPropertyBlock(reader, export, exports);
            materials[index] = Object(values, "Material") ??
                values.Values.OfType<UnrealObjectReference>().FirstOrDefault();
        }
        if (reader.Position - start > size)
        {
            throw new InvalidDataException($"Static mesh '{export.ObjectName}' materials exceed their encoded size.");
        }
        return materials;
    }

    private UnrealMaterialExport ReadMaterial(
        ExportEntry export,
        string className,
        IReadOnlyList<ExportEntry> exports)
    {
        if (export.SerialSize <= 0 || export.SerialOffset < 0 || export.SerialOffset + export.SerialSize > data.Length)
        {
            throw new InvalidDataException($"Material '{export.ObjectName}' has an invalid serialized range.");
        }

        var reader = new PackageCursor(data, export.SerialOffset, export.SerialSize);
        if ((export.ObjectFlags & ObjectFlagHasStack) != 0)
        {
            var node = reader.ReadCompactIndex();
            _ = reader.ReadCompactIndex();
            reader.Skip(8);
            _ = reader.ReadInt32();
            if (node != 0)
            {
                _ = reader.ReadCompactIndex();
            }
        }
        var values = ReadPropertyBlock(reader, export, exports);
        return new UnrealMaterialExport(
            ResolveObjectPath(export, exports),
            className,
            Object(values, "Material") ?? Object(values, "Material1"),
            Object(values, "Diffuse"),
            Object(values, "Opacity"),
            Object(values, "SelfIllumination"),
            Byte(values, "OutputBlending", 0),
            Byte(values, "FrameBufferBlending", 0),
            Bool(values, "TwoSided"),
            Bool(values, "AlphaTest"),
            Byte(values, "AlphaRef", 128),
            !values.TryGetValue("ZWrite", out var zWrite) || zWrite is true,
            !values.TryGetValue("ZTest", out var zTest) || zTest is true,
            Object(values, "Material2"),
            Object(values, "Mask"),
            Float(values, "PanRate", 0),
            Float(values, "RotationRate", Float(values, "RotationSpeed", 0)),
            Byte(values, "CombineOperation", 0),
            Byte(values, "AlphaOperation", 0),
            Object(values, "Detail") ?? Object(values, "DetailTexture"),
            Float(values, "DetailScale", 8),
            values.GetValueOrDefault("Color") is UnrealColor color ? color : null,
            Byte(values, "UOscillationType", 0),
            Byte(values, "VOscillationType", 0),
            Float(values, "UOscillationRate", 0),
            Float(values, "VOscillationRate", 0),
            Float(values, "UOscillationAmplitude", 0),
            Float(values, "VOscillationAmplitude", 0),
            Float(values, "UOscillationPhase", 0),
            Float(values, "VOscillationPhase", 0),
            Bool(values, "TreatAsTwoSided"),
            Object(values, "SelfIlluminationMask"),
            Object(values, "Specular"),
            Object(values, "SpecularityMask"),
            Bool(values, "PerformLightingOnSpecularPass"),
            values.GetValueOrDefault("Color1") is UnrealColor color1 ? color1 : null,
            values.GetValueOrDefault("Color2") is UnrealColor color2 ? color2 : null,
            Byte(values, "ColorFadeType", 0),
            Float(values, "FadePeriod", 0),
            Float(values, "FadePhase", 0),
            Bool(values, "InvertMask"),
            Bool(values, "Modulate2X"),
            Bool(values, "Modulate4X"));
    }

    private static ushort[] ReadIndexStream(PackageCursor reader)
    {
        var count = ReadArrayCount(reader, 30_000_000, "static mesh indices");
        var indices = new ushort[count];
        for (var index = 0; index < count; index++)
        {
            indices[index] = reader.ReadUInt16();
        }
        _ = reader.ReadInt32();
        return indices;
    }

    private static IReadOnlyList<UnrealColor> ReadColorStream(PackageCursor reader)
    {
        var count = ReadArrayCount(reader, 10_000_000, "static mesh colors");
        var colors = new UnrealColor[count];
        for (var index = 0; index < count; index++)
        {
            var blue = reader.ReadByte();
            var green = reader.ReadByte();
            var red = reader.ReadByte();
            var alpha = reader.ReadByte();
            colors[index] = new UnrealColor(red, green, blue, alpha);
        }
        _ = reader.ReadInt32();
        return colors;
    }

    private static int ReadArrayCount(PackageCursor reader, int maximum, string description)
    {
        var count = reader.ReadCompactIndex();
        if (count < 0 || count > maximum)
        {
            throw new InvalidDataException($"The {description} count {count} is invalid.");
        }
        return count;
    }

    private PackageHeader ReadHeader()
    {
        var reader = new PackageCursor(data);
        if (reader.ReadUInt32() != PackageTag)
        {
            throw new InvalidDataException("The decoded file is not an Unreal package.");
        }

        var packedVersion = reader.ReadUInt32();
        packageVersion = (ushort)(packedVersion & 0xffff);
        licenseeVersion = (ushort)(packedVersion >> 16);
        var lineagePackage =
            (packageVersion == 123 && licenseeVersion is >= 12 and <= 36) ||
            (packageVersion == 118 && licenseeVersion is 1 or 3 or 6 or 11);
        var standardPackage = packageVersion is 118 or 123 or 126 && licenseeVersion == 0;
        if (!lineagePackage && !standardPackage)
        {
            throw new InvalidDataException(
                $"Unsupported Unreal package version {packageVersion}/{licenseeVersion}.");
        }

        _ = reader.ReadInt32(); // package flags
        var nameCount = reader.ReadInt32();
        var nameOffset = reader.ReadInt32();
        var exportCount = reader.ReadInt32();
        var exportOffset = reader.ReadInt32();
        var importCount = reader.ReadInt32();
        var importOffset = reader.ReadInt32();
        ValidateTable(nameCount, nameOffset, "name");
        ValidateTable(exportCount, exportOffset, "export");
        ValidateTable(importCount, importOffset, "import");
        return new PackageHeader(nameCount, nameOffset, exportCount, exportOffset, importCount, importOffset);
    }

    private void ReadNames(PackageHeader header)
    {
        var reader = new PackageCursor(data, header.NameOffset);
        for (var index = 0; index < header.NameCount; index++)
        {
            names.Add(reader.ReadUnrealString());
            _ = reader.ReadUInt32(); // object flags
        }
    }

    private void ReadImports(PackageHeader header)
    {
        var reader = new PackageCursor(data, header.ImportOffset);
        for (var index = 0; index < header.ImportCount; index++)
        {
            _ = ReadName(reader); // class package
            var className = ReadName(reader);
            var packageIndex = reader.ReadInt32();
            var objectName = ReadName(reader);
            imports.Add(new ImportEntry(className, packageIndex, objectName));
        }
    }

    private IReadOnlyList<ExportEntry> ReadExports(PackageHeader header)
    {
        var reader = new PackageCursor(data, header.ExportOffset);
        var exports = new List<ExportEntry>(header.ExportCount);
        for (var index = 0; index < header.ExportCount; index++)
        {
            var classIndex = reader.ReadCompactIndex();
            _ = reader.ReadCompactIndex(); // superclass
            var packageIndex = reader.ReadInt32();
            var objectName = ReadName(reader);
            var objectFlags = reader.ReadUInt32();
            var serialSize = reader.ReadCompactIndex();
            var serialOffset = serialSize == 0 ? 0 : reader.ReadCompactIndex();
            exports.Add(new ExportEntry(classIndex, packageIndex, objectName, objectFlags, serialSize, serialOffset));
        }

        return exports;
    }

    private UnrealTextureExport ReadTexture(
        ExportEntry export,
        string objectPath,
        IReadOnlyDictionary<int, IReadOnlyList<UnrealColor>> palettes,
        IReadOnlyList<ExportEntry> exports)
    {
        if (export.SerialSize <= 0 || export.SerialOffset < 0 || export.SerialOffset + export.SerialSize > data.Length)
        {
            throw new InvalidDataException($"Texture '{objectPath}' has an invalid serialized range.");
        }

        var reader = new PackageCursor(data, export.SerialOffset, export.SerialSize);
        int? format = null;
        int? paletteIndex = null;
        UnrealObjectReference? animationNext = null;
        var minFrameRate = 0f;
        var maxFrameRate = 0f;
        var masked = false;
        var alphaTexture = false;
        var twoSided = false;
        UnrealObjectReference? detail = null;
        var detailScale = 8f;
        byte uClampMode = 0;
        byte vClampMode = 0;
        while (true)
        {
            var propertyName = ReadName(reader);
            if (string.Equals(propertyName, "None", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            var info = reader.ReadByte();
            var type = info & 0x0f;
            if (type == 10) // StructProperty
            {
                _ = ReadName(reader);
            }

            var size = ReadPropertySize(reader, (info >> 4) & 7);
            if (type != 3 && (info & 0x80) != 0) // BoolProperty stores its value in the array bit.
            {
                _ = reader.ReadArrayIndex();
            }

            var valueOffset = reader.Position;
            if (string.Equals(propertyName, "Format", StringComparison.OrdinalIgnoreCase) && size == 1)
            {
                format = reader.ReadByte();
            }
            else if (string.Equals(propertyName, "Palette", StringComparison.OrdinalIgnoreCase) && type == 5)
            {
                paletteIndex = reader.ReadCompactIndex();
            }
            else if (string.Equals(propertyName, "AnimNext", StringComparison.OrdinalIgnoreCase) && type == 5)
            {
                animationNext = ResolveObjectReference(reader.ReadCompactIndex(), exports);
            }
            else if (string.Equals(propertyName, "MinFrameRate", StringComparison.OrdinalIgnoreCase) && size == 4)
            {
                minFrameRate = reader.ReadSingle();
            }
            else if (string.Equals(propertyName, "MaxFrameRate", StringComparison.OrdinalIgnoreCase) && size == 4)
            {
                maxFrameRate = reader.ReadSingle();
            }
            else if (string.Equals(propertyName, "bMasked", StringComparison.OrdinalIgnoreCase) && type == 3)
            {
                masked = (info & 0x80) != 0;
            }
            else if (string.Equals(propertyName, "bAlphaTexture", StringComparison.OrdinalIgnoreCase) && type == 3)
            {
                alphaTexture = (info & 0x80) != 0;
            }
            else if (string.Equals(propertyName, "bTwoSided", StringComparison.OrdinalIgnoreCase) && type == 3)
            {
                twoSided = (info & 0x80) != 0;
            }
            else if (string.Equals(propertyName, "Detail", StringComparison.OrdinalIgnoreCase) && type == 5)
            {
                detail = ResolveObjectReference(reader.ReadCompactIndex(), exports);
            }
            else if (string.Equals(propertyName, "DetailScale", StringComparison.OrdinalIgnoreCase) && size == 4)
            {
                detailScale = reader.ReadSingle();
            }
            else if (string.Equals(propertyName, "UClampMode", StringComparison.OrdinalIgnoreCase) && size == 1)
            {
                uClampMode = reader.ReadByte();
            }
            else if (string.Equals(propertyName, "VClampMode", StringComparison.OrdinalIgnoreCase) && size == 1)
            {
                vClampMode = reader.ReadByte();
            }
            else
            {
                reader.Skip(size);
            }

            if (reader.Position != valueOffset + size)
            {
                throw new InvalidDataException($"Texture '{objectPath}' property '{propertyName}' has an invalid encoded size.");
            }
        }

        // The supported Interlude texture packages serialize one obsolete material
        // integer between UObject properties and the texture's native mip array.
        if (packageVersion == 123 && reader.PeekUInt32() == 0)
        {
            _ = reader.ReadInt32();
        }

        var mipCount = reader.ReadCompactIndex();
        if (mipCount == 0)
        {
            return new UnrealTextureExport(
                objectPath,
                format is null ? null : (byte)format,
                0,
                0,
                null,
                0,
                animationNext,
                minFrameRate,
                maxFrameRate,
                masked,
                alphaTexture,
                twoSided,
                detail,
                detailScale,
                uClampMode,
                vClampMode);
        }

        if (mipCount < 0 || mipCount > 32)
        {
            throw new InvalidDataException($"Texture '{objectPath}' has an invalid mip count {mipCount}.");
        }

        var mips = new List<UnrealTextureMip>(mipCount);
        for (var mipIndex = 0; mipIndex < mipCount; mipIndex++)
        {
            _ = reader.ReadInt32(); // TLazyArray end position
            var dataLength = reader.ReadCompactIndex();
            if (dataLength < 0)
            {
                throw new InvalidDataException(
                    $"Texture '{objectPath}' mip {mipIndex} has a negative data length.");
            }

            var levelData = reader.ReadBytes(dataLength);
            var mipWidth = reader.ReadInt32();
            var mipHeight = reader.ReadInt32();
            _ = reader.ReadByte(); // UBits
            _ = reader.ReadByte(); // VBits
            if (mipWidth <= 0 || mipHeight <= 0)
            {
                throw new InvalidDataException(
                    $"Texture '{objectPath}' mip {mipIndex} has invalid dimensions {mipWidth}x{mipHeight}.");
            }
            mips.Add(new UnrealTextureMip(mipWidth, mipHeight, levelData));
        }

        var baseMip = mips[0];
        var width = baseMip.Width;
        var height = baseMip.Height;
        var mipData = baseMip.Data;

        palettes.TryGetValue(paletteIndex ?? 0, out var palette);
        var effectiveFormat = format ?? (palette is null ? -1 : 0);
        var texture = effectiveFormat switch
        {
            0 => new UnrealTexture(objectPath, UnrealTextureFormat.P8, width, height, mipData, palette, mips),
            TextureFormatDxt1 => new UnrealTexture(objectPath, UnrealTextureFormat.Dxt1, width, height, mipData, Mips: mips),
            TextureFormatRgba8 => new UnrealTexture(objectPath, UnrealTextureFormat.Rgba8, width, height, mipData, Mips: mips),
            TextureFormatDxt3 => new UnrealTexture(objectPath, UnrealTextureFormat.Dxt3, width, height, mipData, Mips: mips),
            TextureFormatDxt5 => new UnrealTexture(objectPath, UnrealTextureFormat.Dxt5, width, height, mipData, Mips: mips),
            TextureFormatG16 => new UnrealTexture(objectPath, UnrealTextureFormat.G16, width, height, mipData, Mips: mips),
            _ => null
        };
        return new UnrealTextureExport(
            objectPath,
            effectiveFormat < 0 ? null : (byte)effectiveFormat,
            width,
            height,
            texture,
            mipCount,
            animationNext,
            minFrameRate,
            maxFrameRate,
            masked,
            alphaTexture,
            twoSided,
            detail,
            detailScale,
            uClampMode,
            vClampMode);
    }

    private static string ResolveObjectPath(ExportEntry export, IReadOnlyList<ExportEntry> exports)
    {
        var segments = new List<string> { export.ObjectName };
        var packageIndex = export.PackageIndex;
        var remaining = exports.Count;
        while (packageIndex > 0 && remaining-- > 0)
        {
            var outerIndex = packageIndex - 1;
            if (outerIndex < 0 || outerIndex >= exports.Count)
            {
                throw new InvalidDataException($"Object '{export.ObjectName}' has an invalid outer export index.");
            }

            var outer = exports[outerIndex];
            segments.Add(outer.ObjectName);
            packageIndex = outer.PackageIndex;
        }

        if (remaining < 0)
        {
            throw new InvalidDataException($"Object '{export.ObjectName}' has a cyclic outer chain.");
        }

        segments.Reverse();
        return string.Join('.', segments);
    }

    private IReadOnlyList<UnrealColor> ReadPalette(ExportEntry export)
    {
        if (export.SerialSize <= 0 || export.SerialOffset < 0 || export.SerialOffset + export.SerialSize > data.Length)
        {
            throw new InvalidDataException($"Palette '{export.ObjectName}' has an invalid serialized range.");
        }

        var reader = new PackageCursor(data, export.SerialOffset, export.SerialSize);
        SkipProperties(reader);
        var colorCount = reader.ReadCompactIndex();
        if (colorCount is < 1 or > 256)
        {
            throw new InvalidDataException($"Palette '{export.ObjectName}' has an invalid color count {colorCount}.");
        }

        var colors = new UnrealColor[colorCount];
        for (var index = 0; index < colors.Length; index++)
        {
            var blue = reader.ReadByte();
            var green = reader.ReadByte();
            var red = reader.ReadByte();
            var alpha = reader.ReadByte();
            colors[index] = new UnrealColor(red, green, blue, alpha);
        }

        return colors;
    }

    private void SkipProperties(PackageCursor reader)
    {
        while (true)
        {
            var propertyName = ReadName(reader);
            if (string.Equals(propertyName, "None", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var info = reader.ReadByte();
            var type = info & 0x0f;
            if (type == 10)
            {
                _ = ReadName(reader);
            }

            var size = ReadPropertySize(reader, (info >> 4) & 7);
            if (type != 3 && (info & 0x80) != 0)
            {
                _ = reader.ReadArrayIndex();
            }

            if (type != 3)
            {
                reader.Skip(size);
            }
        }
    }

    private string ResolveClassName(int classIndex, IReadOnlyList<ExportEntry> exports)
    {
        if (classIndex < 0)
        {
            var index = -classIndex - 1;
            return index >= 0 && index < imports.Count ? imports[index].ObjectName : string.Empty;
        }

        if (classIndex > 0)
        {
            var index = classIndex - 1;
            return index >= 0 && index < exports.Count ? exports[index].ObjectName : string.Empty;
        }

        return string.Empty;
    }

    private string ReadName(PackageCursor reader)
    {
        var index = reader.ReadCompactIndex();
        if (index < 0 || index >= names.Count)
        {
            throw new InvalidDataException($"Package name index {index} is outside the name table.");
        }

        return names[index];
    }

    private static int ReadPropertySize(PackageCursor reader, int code) => code switch
    {
        0 => 1,
        1 => 2,
        2 => 4,
        3 => 12,
        4 => 16,
        5 => reader.ReadByte(),
        6 => reader.ReadUInt16(),
        7 => reader.ReadInt32(),
        _ => throw new InvalidDataException("Unknown Unreal property size code.")
    };

    private void ValidateTable(int count, int offset, string table)
    {
        if (count < 0 || offset < 0 || offset > data.Length)
        {
            throw new InvalidDataException($"The Unreal {table} table has an invalid location.");
        }
    }

    private sealed record PackageHeader(int NameCount, int NameOffset, int ExportCount, int ExportOffset, int ImportCount, int ImportOffset);
    private sealed record ImportEntry(string ClassName, int PackageIndex, string ObjectName);
    private sealed record ParsedObject(
        IReadOnlyDictionary<string, object?> Values,
        int NativeOffset,
        int NativeLength);
    private sealed record ExportEntry(
        int ClassIndex,
        int PackageIndex,
        string ObjectName,
        uint ObjectFlags,
        int SerialSize,
        int SerialOffset);

    private sealed class PackageCursor
    {
        private readonly byte[] source;
        private readonly int end;

        public PackageCursor(byte[] source, int offset = 0, int? length = null)
        {
            this.source = source;
            Position = offset;
            end = length is null ? source.Length : checked(offset + length.Value);
            if (offset < 0 || end < offset || end > source.Length)
            {
                throw new InvalidDataException("A package cursor was created outside the input data.");
            }
        }

        public int Position { get; private set; }
        public int Remaining => end - Position;

        public byte ReadByte()
        {
            Require(1);
            return source[Position++];
        }

        public ushort ReadUInt16()
        {
            Require(2);
            var value = BinaryPrimitives.ReadUInt16LittleEndian(source.AsSpan(Position, 2));
            Position += 2;
            return value;
        }

        public uint ReadUInt32()
        {
            Require(4);
            var value = BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan(Position, 4));
            Position += 4;
            return value;
        }

        public int ReadInt32() => unchecked((int)ReadUInt32());

        public ulong ReadUInt64()
        {
            Require(8);
            var value = BinaryPrimitives.ReadUInt64LittleEndian(source.AsSpan(Position, 8));
            Position += 8;
            return value;
        }

        public float ReadSingle() => BitConverter.Int32BitsToSingle(ReadInt32());

        public Vector3 ReadVector3() => new(ReadSingle(), ReadSingle(), ReadSingle());

        public uint PeekUInt32()
        {
            Require(4);
            return BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan(Position, 4));
        }

        public byte PeekByte()
        {
            Require(1);
            return source[Position];
        }

        public int ReadCompactIndex()
        {
            var current = ReadByte();
            var negative = (current & 0x80) != 0;
            var value = current & 0x3f;
            var shift = 6;
            if ((current & 0x40) != 0)
            {
                do
                {
                    current = ReadByte();
                    value |= (current & 0x7f) << shift;
                    shift += 7;
                    if (shift > 34)
                    {
                        throw new InvalidDataException("An Unreal compact index is too long.");
                    }
                }
                while ((current & 0x80) != 0);
            }

            return negative ? -value : value;
        }

        public string ReadUnrealString()
        {
            var length = ReadCompactIndex();
            if (length == 0)
            {
                return string.Empty;
            }

            if (length > 0)
            {
                var bytes = ReadBytes(length);
                if (bytes[^1] != 0)
                {
                    throw new InvalidDataException("An Unreal ANSI string is not null terminated.");
                }

                return Encoding.Latin1.GetString(bytes, 0, bytes.Length - 1);
            }

            var characterCount = checked(-length);
            var bytesLength = checked(characterCount * 2);
            var unicode = ReadBytes(bytesLength);
            if (unicode[^1] != 0 || unicode[^2] != 0)
            {
                throw new InvalidDataException("An Unreal Unicode string is not null terminated.");
            }

            return Encoding.Unicode.GetString(unicode, 0, unicode.Length - 2);
        }

        public byte[] ReadBytes(int count)
        {
            Require(count);
            var result = source.AsSpan(Position, count).ToArray();
            Position += count;
            return result;
        }

        public void Skip(int count)
        {
            Require(count);
            Position += count;
        }

        public void Seek(int position)
        {
            if (position < 0 || position > end)
            {
                throw new InvalidDataException("A package cursor seeked outside its input range.");
            }
            Position = position;
        }

        public int ReadArrayIndex()
        {
            var first = ReadByte();
            if (first < 128)
            {
                return first;
            }

            if ((first & 0x40) != 0)
            {
                return ((first & 0x3f) << 24) |
                    (ReadByte() << 16) |
                    (ReadByte() << 8) |
                    ReadByte();
            }

            return ((first & 0x7f) << 8) | ReadByte();
        }

        private void Require(int count)
        {
            if (count < 0 || Position > end - count)
            {
                throw new InvalidDataException("Unexpected end of Unreal package data.");
            }
        }
    }
}
