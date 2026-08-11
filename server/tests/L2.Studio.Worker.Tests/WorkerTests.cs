using L2.Studio.Worker;
using L2.Studio.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace L2.Studio.Worker.Tests;

public sealed class WorkerTests
{
    [Fact]
    public async Task Worker_stops_cleanly_when_cancelled()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var worker = new L2.Studio.Worker.Worker(
            new IdleProcessor(),
            NullLogger<L2.Studio.Worker.Worker>.Instance);

        await worker.RunAsync(cancellation.Token);
    }

    private sealed class IdleProcessor : IAssetImportJobProcessor
    {
        public Task<bool> ProcessNextAsync(CancellationToken cancellationToken) => Task.FromResult(false);
    }
}
