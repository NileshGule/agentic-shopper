using AgenticShopper.Agents.Receipt.Interfaces;
using AgenticShopper.Agents.Receipt.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IOcrService, TesseractOcrService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", agent = "receipt" }));

app.Run();
