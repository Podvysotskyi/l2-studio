using L2.Studio.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace L2.Foundation.Tests;

public sealed class WorkerTests
{
    [Fact]
    public async Task Worker_stops_cleanly_when_cancelled()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var worker = new Worker(new IdleProcessor(), NullLogger<Worker>.Instance);

        await worker.RunAsync(cancellation.Token);
    }

    private sealed class IdleProcessor : IAssetImportJobProcessor
    {
        public Task<bool> ProcessNextAsync(CancellationToken cancellationToken) => Task.FromResult(false);
    }
}
