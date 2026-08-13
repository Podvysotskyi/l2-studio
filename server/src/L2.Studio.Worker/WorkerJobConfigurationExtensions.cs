using L2.Studio.Configurations;

namespace L2.Studio.Worker;

public static class WorkerJobConfigurationExtensions
{
    public static IHostApplicationBuilder AddStudioWorkerJobs(this IHostApplicationBuilder builder)
    {
        builder.AddStudioWorkerMessaging(typeof(WorkerJobConfigurationExtensions).Assembly);
        builder.Services.AddHostedService<AssetStorageReconciliationPublisher>();
        return builder;
    }
}
