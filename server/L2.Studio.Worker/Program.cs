using L2.Studio.Content;
using L2.Studio.Foundation;
using L2.Studio.Contracts;
using L2.Studio.Worker;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);
builder.AddL2WorkerFoundation("l2-studio-worker");
builder.Services.Configure<AssetImportOptions>(builder.Configuration.GetSection(AssetImportOptions.SectionName));
builder.Services.AddGameContentPersistence(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IAssetImportJobProcessor, AssetImportJobProcessor>();
builder.Services.AddHostedService<Worker>();
await builder.Build().RunAsync();
