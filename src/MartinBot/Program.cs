var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "MartinBot is running");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
