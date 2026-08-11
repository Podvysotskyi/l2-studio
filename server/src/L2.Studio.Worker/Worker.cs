using L2.Studio.Services.Interfaces;

namespace L2.Studio.Worker;

public sealed class Worker(
    IAssetImportJobProcessor processor,
    ILogger<Worker> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => RunAsync(stoppingToken);

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Studio asset-import worker started");
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!await processor.ProcessNextAsync(stoppingToken))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    }
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "Studio worker could not poll asset-import jobs");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Studio asset-import worker stopping");
        }
    }
}
