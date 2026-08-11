using L2.Studio.Configurations;

var builder = WebApplication.CreateBuilder(args)
    .AddStudioApi("l2-studio-api");
builder.Services.AddStudioApiApplication(builder.Configuration);
var app = builder.Build();
app.MapStudioApi();
app.Run();
