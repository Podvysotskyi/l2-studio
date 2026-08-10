var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok());
app.Run();

namespace L2.Studio.Api { public sealed class StudioApiMarker; }
