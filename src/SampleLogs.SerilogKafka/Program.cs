using Microsoft.AspNetCore.Mvc;
using SampleLogs.SerilogKafka;
using SampleLogs.SerilogKafka.KafkaLoggerConfig;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

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

app.UseSerilogRequestLogging();

app.UseMiddleware<RequestContextLoggingMiddleware>();

app.MapPost("", ([FromBody] Request request, ILogger<Program> logger) =>
{
    logger.LogInformation("Request {@Request}", request);

    if (request.Title.Equals("ex"))
    {
        logger.LogError("Request exception {@Request}", request);
        throw new ArgumentException("test");
    }
    
    return Results.Ok(request);
});

app.Run();

public record Request(string Title);