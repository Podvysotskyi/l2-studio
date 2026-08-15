# Studio server extension guide

Use this guide for HTTP APIs, contracts, persistence, repositories,
configuration, migrations, and server-owned tests.

## Contents

- [Respect project ownership](#respect-project-ownership)
- [Build an endpoint](#build-an-endpoint)
- [Model and query persistence](#model-and-query-persistence)
- [Create a migration](#create-a-migration)
- [Register dependencies and configuration](#register-dependencies-and-configuration)
- [Test server changes](#test-server-changes)

## Respect project ownership

| Project | Responsibility |
| --- | --- |
| `L2.Studio.Api` | Controllers, request filters, and HTTP composition |
| `L2.Studio.Contracts` | Browser-facing models, requests, and responses |
| `L2.Studio.Context` | EF Core entities and model configuration |
| `L2.Studio.Migrations` | Migrations, design-time context creation, and seed data |
| `L2.Studio.Repositories.Interfaces` | Persistence abstractions and shared persistence/import values |
| `L2.Studio.Repositories` | Database implementations, projections, and path validation |
| `L2.Studio.Services.Interfaces` | Cross-host service abstractions and options |
| `L2.Studio.Services` | Orchestration, conversion, manifests, previews, and asset processing |
| `L2.Studio.Configurations` | DI, host composition, messaging, persistence, health, CORS, identity, and observability |
| `L2.Studio.Worker` | Wolverine handlers and bounded Worker lifecycle services |
| `L2.Tools.*` | Reusable client parsing and conversion libraries with stable public names |

Put each top-level type in a same-named file and use the root project namespace.
Use intentional partial files only for one large cohesive implementation; name
them `TypeName.Concern.cs` and keep the declared type partial.

## Build an endpoint

Use version-scoped routes for content, jobs, catalogs, artifacts, and releases:

```csharp
[ApiController]
[Route("api/game-versions/{gameVersion}/things")]
public sealed class ThingsController(IThingRepository repository) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ThingSummary>> Get(
        string gameVersion,
        int id,
        CancellationToken cancellationToken)
    {
        var thing = await repository.GetAsync(gameVersion, id, cancellationToken);
        return thing is null ? NotFound() : Ok(thing);
    }
}
```

Follow this sequence:

1. Define one immutable record per request, response, or model file in
   `L2.Studio.Contracts`. Reuse `DirectoryPage<T>` for standard paging.
2. Extend the owning repository interface with an asynchronous operation that
   accepts the game version and `CancellationToken`.
3. Implement a projection to the public contract in the repository. Do not
   expose tracked entities or persistence navigation properties.
4. Add a thin controller action. Normalize strings once, validate all inputs,
   delegate work, and map the result to an explicit HTTP status.
5. Mirror the contract and endpoint in the web service when it is browser
   facing.

Use route constraints such as `{id:int}` and `{id:guid}`. Encode composite
resource identity explicitly in the route. Do not infer the selected game
version from global process state on the server.

Return consistent results:

- `200 OK` for successful reads and mutations that return a representation.
- `202 Accepted` with a status location for queued durable work.
- `204 No Content` for successful deletes with no body.
- `400 ValidationProblemDetails` keyed by the invalid request property.
- `404 Not Found` when the version-scoped resource or supported route target is
  absent.
- `409 Conflict` for active-job conflicts or dependency-protected deletes.

Catch only expected domain/persistence conflicts at the HTTP boundary. Do not
convert cancellation or unexpected failures into validation responses.

## Model and query persistence

Entities use data annotations for scalar schema metadata:

```csharp
[Table("things")]
[PrimaryKey(nameof(GameVersion), nameof(Id))]
public sealed class Thing
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }

    [Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [Column("name"), MaxLength(100)]
    public required string Name { get; set; }
}
```

Put table names, column names, maximum lengths, primary keys, and generated
value behavior on the entity. Use `GameContentDbContext.OnModelCreating` for:

- relationships and delete behavior;
- alternate keys and indexes;
- database defaults and filtered indexes;
- provider types such as `jsonb`;
- check constraints and inheritance discriminators.

Add a `DbSet<T>` for aggregate roots and entities queried directly. Every
version-owned entity must have a game-version foreign key with restrictive
delete behavior unless it is a dependent that intentionally cascades with its
owner. Composite relationships must include `GameVersion` so data cannot cross
version boundaries.

Repository implementations should:

- create contexts through `IDbContextFactory<GameContentDbContext>`;
- use `AsNoTracking` for reads;
- filter by `GameVersion` before identifiers, search, or paging;
- project in SQL directly to contract records;
- order deterministically before `Skip` and `Take`;
- return `DirectoryPage<T>` with total count, page, and page size;
- escape `%`, `_`, and `\` for literal `ILIKE` search patterns;
- save once per atomic mutation and honor cancellation throughout;
- translate provider constraint errors only when the caller needs a stable
  domain conflict.

## Create a migration

After the consolidated initial migration is published, create additive
migrations. Do not rewrite or delete an applied migration to accommodate a new
entity.

Generate migrations through the SDK container from the Studio repository root.
Replace `AddThings` with a descriptive PascalCase name:

```sh
docker run --rm \
  --user "$(id -u):$(id -g)" \
  --env HOME=/tmp/l2-studio-home \
  --volume "$PWD:/workspace" \
  --workdir /workspace \
  mcr.microsoft.com/dotnet/sdk:10.0.102 \
  sh -lc '
    mkdir -p "$HOME" /tmp/dotnet-tools
    dotnet tool install dotnet-ef --tool-path /tmp/dotnet-tools --version 10.0.4
    /tmp/dotnet-tools/dotnet-ef migrations add AddThings \
      --project server/src/L2.Studio.Migrations/L2.Studio.Migrations.csproj \
      --startup-project server/src/L2.Studio.Migrations/L2.Studio.Migrations.csproj \
      --context GameContentDbContext
  '
```

Review the generated `Up`, `Down`, designer, and model snapshot. Confirm schema,
table/column names, keys, foreign keys, delete behavior, indexes, defaults, and
provider types. Add or update context model tests for the same invariants. Do
not hand-author designer or snapshot output.

## Register dependencies and configuration

- Register API application repositories, services, initializers, and recovery
  services in `AddStudioApiApplication`.
- Register Worker-only processors and stores in `AddStudioWorkerApplication`.
- Put transport setup and message routing in
  `StudioMessagingConfigurationExtensions`.
- Put Worker lifecycle services in `WorkerJobConfigurationExtensions`.
- Bind options from a named section, validate safety and required values, and
  call `ValidateOnStart` for host-critical configuration.
- Add health checks for dependencies or migration state that determine
  readiness. Keep liveness independent from downstream availability.
- Add environment-specific non-secret defaults to API appsettings and Compose;
  keep deployable secrets in environment variables.

Whenever registration or options change, extend
`L2.Studio.Configurations.Tests` to assert the service lifetime, implementation,
validation, hosted service, or health registration.

## Test server changes

Place tests with the production project they verify:

- `L2.Studio.Api.Tests`: normalization, validation, delegation, statuses, and
  problem details using stubs.
- `L2.Studio.Context.Tests`: relational model metadata without connecting to a
  database.
- `L2.Studio.Migrations.Tests`: seed and migration-owned behavior.
- `L2.Studio.Repositories.Tests`: projections, filters, paging, mutations,
  conflicts, and path validation using database-free fixtures.
- `L2.Studio.Services.Tests`: orchestration, conversion, manifests, path safety,
  and serialization.
- `L2.Studio.Worker.Tests`: message dispatch, reconciliation, lifecycle, and
  catalog integrity.
- `L2.Studio.Configurations.Tests`: DI, options, host composition, health, and
  messaging configuration.
- `L2.Tools.*.Tests`: parsing and conversion behavior owned by each tool.

Keep repository tests database-free. Run server unit tests plus both production
host builds after any server change, and run Compose validation for host,
storage, or cross-service changes.
