using Microsoft.AspNetCore.Mvc;
using NLog;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseNLog();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("", ([FromBody] Request request, ILogger<Program> logger) =>
{
    logger.LogInformation("Request {@Request}", request);

    if (request.Title.Equals("ex"))
    {
        logger.LogError("Request exception {@Request}", request);
        throw new ArgumentException("test");
    }
    
    logger.LogInformation("test");
    return Results.Ok(request);
});

app.Run();

public record Request(string Title);