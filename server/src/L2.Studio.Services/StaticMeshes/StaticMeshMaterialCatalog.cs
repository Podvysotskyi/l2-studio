using System.Text.Json;
using L2.Studio.Context;
using L2.Studio.Context.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace L2.Studio.Services;

internal sealed record StaticMeshMaterialCatalog(
    StaticMeshMaterialResolver Resolver,
    IReadOnlyList<string> GpuTextureFormats,
    int LoadedTextureCount);
