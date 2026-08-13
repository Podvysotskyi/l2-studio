using JasperFx;
using L2.Studio.Configurations;
using L2.Studio.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.AddStudioWorker("l2-studio-worker");
builder.AddStudioWorkerJobs();
builder.Services.AddStudioWorkerApplication(builder.Configuration);
return await builder.Build().RunJasperFxCommands(args);
