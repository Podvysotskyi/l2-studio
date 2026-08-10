using L2.Studio.Content;
using L2.Studio.Foundation;
using L2.Studio.Api.Content;
using L2.Studio.Api.Data;
using L2.Studio.Api.Assets;
using L2.Studio.Contracts;

var builder = WebApplication.CreateBuilder(args)
    .AddL2Foundation("l2-studio-api");
builder.Services.Configure<GameContentOptions>(builder.Configuration.GetSection(GameContentOptions.SectionName));
builder.Services.Configure<AssetImportOptions>(builder.Configuration.GetSection(AssetImportOptions.SectionName));
builder.Services.AddGameContentPersistence(builder.Configuration);
builder.Services.AddHealthChecks().AddGameContentMigrationHealthCheck();
builder.Services.AddScoped<ContentDirectoryRepository>();
builder.Services.AddScoped<AssetImportRepository>();
builder.Services.AddScoped<AssetCatalogRepository>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<GameContentMigrator>();
builder.Services.AddHostedService<GameContentLookupSeedService>();
builder.Services.AddHostedService<GameContentPlayerLookupSeedService>();
builder.Services.AddHostedService<GameContentPlayerClassSeedService>();
builder.Services.AddHostedService<GameContentPlayerAppearanceSeedService>();
builder.Services.AddHostedService<GameContentNpcSeedService>();
builder.Services.AddHostedService<GameContentSkillSeedService>();
var app = builder.Build();
app.MapL2Foundation();
app.MapContentDirectory();
app.MapAssetImports();
app.MapAssetCatalogs();
app.Run();

namespace L2.Studio.Api { public sealed class StudioApiMarker; }
