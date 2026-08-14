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

[WolverineHandler]
public sealed class AssetImportFileHandlers(IAssetImportWorkItemProcessor processor)
{
    public Task Handle(ImportTextureFile message, CancellationToken token) => processor.ProcessAsync(message.WorkItemId, token);
    public Task Handle(ImportStaticMeshFile message, CancellationToken token) => processor.ProcessAsync(message.WorkItemId, token);
    public Task Handle(ImportAnimationFile message, CancellationToken token) => processor.ProcessAsync(message.WorkItemId, token);
    public Task Handle(ImportSoundFile message, CancellationToken token) => processor.ProcessAsync(message.WorkItemId, token);
    public Task Handle(ImportMusicFile message, CancellationToken token) => processor.ProcessAsync(message.WorkItemId, token);
    public Task Handle(ImportMapFile message, CancellationToken token) => processor.ProcessAsync(message.WorkItemId, token);
    public Task Handle(ImportSceneFile message, CancellationToken token) => processor.ProcessAsync(message.WorkItemId, token);
    public Task Handle(GenerateMapPreview message, CancellationToken token) => processor.ProcessAsync(message.WorkItemId, token);
}
