using JasperFx;
using L2.Studio.Configurations;

var builder = Host.CreateApplicationBuilder(args);
builder.AddStudioWorker("l2-studio-worker");
builder.AddStudioWorkerMessaging();
builder.Services.AddStudioWorkerApplication(builder.Configuration);
return await builder.Build().RunJasperFxCommands(args);
