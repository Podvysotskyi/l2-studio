using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces.Models;
using L2.Studio.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.EntityFrameworkCore;
using Wolverine.Runtime;

namespace L2.Studio.Worker;

public sealed class AssetStorageReconciliationPublisher(IWolverineRuntime runtime) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken) =>
        await new MessageBus(runtime).PublishAsync(new ReconcileAssetStorage());

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
