using L2.Studio.Configurations;
using L2.Studio.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.AddStudioWorker("l2-studio-worker");
builder.Services.AddStudioWorkerApplication(builder.Configuration);
builder.Services.AddHostedService<Worker>();
await builder.Build().RunAsync();
