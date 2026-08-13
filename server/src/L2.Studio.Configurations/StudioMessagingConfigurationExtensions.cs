using JasperFx.Resources;
using L2.Studio.Messages;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;

namespace L2.Studio.Configurations;

public static class StudioMessagingConfigurationExtensions
{
    public const string TransportSchema = "l2_messaging";
    public const string ApiEnvelopeSchema = "l2_messaging_api";
    public const string WorkerEnvelopeSchema = "l2_messaging_worker";
    public const string ControlQueue = "l2_asset_import_control";
    public const string FileQueue = "l2_asset_import_files";

    public static IHostApplicationBuilder AddStudioApiMessaging(this IHostApplicationBuilder builder)
    {
        Configure(builder, ApiEnvelopeSchema, listen: false);
        return builder;
    }

    public static IHostApplicationBuilder AddStudioWorkerMessaging(
        this IHostApplicationBuilder builder,
        Assembly handlerAssembly)
    {
        Configure(builder, WorkerEnvelopeSchema, listen: true, handlerAssembly);
        return builder;
    }

    private static void Configure(
        IHostApplicationBuilder builder,
        string envelopeSchema,
        bool listen,
        Assembly? handlerAssembly = null)
    {
        var connectionString = builder.Configuration.GetConnectionString("PostgreSql")
            ?? throw new InvalidOperationException("ConnectionStrings:PostgreSql is required.");
        builder.UseWolverine(options =>
        {
            var persistence = options.UsePostgresqlPersistenceAndTransport(
                connectionString,
                envelopeSchema,
                TransportSchema);
            if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
            {
                persistence.AutoProvision();
                options.Services.AddResourceSetupOnStartup();
            }

            options.UseEntityFrameworkCoreTransactions();
            RouteControlMessages(options);
            RouteFileMessages(options);
            if (listen)
            {
                options.Discovery.IncludeAssembly(handlerAssembly
                    ?? throw new InvalidOperationException("A Worker handler assembly is required."));
                options.ListenToPostgresqlQueue(ControlQueue).Sequential().MaximumMessagesToReceive(1);
                options.ListenToPostgresqlQueue(FileQueue).Sequential().MaximumMessagesToReceive(1);
            }
        });
    }

    private static void RouteControlMessages(WolverineOptions options)
    {
        options.PublishMessage<DiscoverTextures>().ToPostgresqlQueue(ControlQueue);
        options.PublishMessage<DiscoverStaticMeshes>().ToPostgresqlQueue(ControlQueue);
        options.PublishMessage<DiscoverSounds>().ToPostgresqlQueue(ControlQueue);
        options.PublishMessage<DiscoverMusic>().ToPostgresqlQueue(ControlQueue);
        options.PublishMessage<DiscoverMaps>().ToPostgresqlQueue(ControlQueue);
        options.PublishMessage<DiscoverScenes>().ToPostgresqlQueue(ControlQueue);
        options.PublishMessage<DiscoverMapPreviews>().ToPostgresqlQueue(ControlQueue);
        options.PublishMessage<AssetImportWorkItemCompleted>().ToPostgresqlQueue(ControlQueue);
        options.PublishMessage<FinalizeAssetImportRun>().ToPostgresqlQueue(ControlQueue);
        options.PublishMessage<DeleteAssetVersion>().ToPostgresqlQueue(ControlQueue);
        options.PublishMessage<ValidateAssetRelease>().ToPostgresqlQueue(ControlQueue);
        options.PublishMessage<ActivateAssetRelease>().ToPostgresqlQueue(ControlQueue);
        options.PublishMessage<ImportC1NpcTypes>().ToPostgresqlQueue(ControlQueue);
        options.PublishMessage<ImportC4NpcTypes>().ToPostgresqlQueue(ControlQueue);
        options.PublishMessage<ImportInterludeNpcTypes>().ToPostgresqlQueue(ControlQueue);
        options.PublishMessage<ImportC1NpcRaces>().ToPostgresqlQueue(ControlQueue);
        options.PublishMessage<ImportC4NpcRaces>().ToPostgresqlQueue(ControlQueue);
        options.PublishMessage<ImportInterludeNpcRaces>().ToPostgresqlQueue(ControlQueue);
    }

    private static void RouteFileMessages(WolverineOptions options)
    {
        options.PublishMessage<ImportTextureFile>().ToPostgresqlQueue(FileQueue);
        options.PublishMessage<ImportStaticMeshFile>().ToPostgresqlQueue(FileQueue);
        options.PublishMessage<ImportSoundFile>().ToPostgresqlQueue(FileQueue);
        options.PublishMessage<ImportMusicFile>().ToPostgresqlQueue(FileQueue);
        options.PublishMessage<ImportMapFile>().ToPostgresqlQueue(FileQueue);
        options.PublishMessage<ImportSceneFile>().ToPostgresqlQueue(FileQueue);
        options.PublishMessage<GenerateMapPreview>().ToPostgresqlQueue(FileQueue);
        options.PublishMessage<ReconcileAssetStorage>().ToPostgresqlQueue(FileQueue);
    }
}
