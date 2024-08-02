using System.Diagnostics;
using NLog.Extensions.Logging;
using NLog.Web;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using web1.Observability.Metrics;

var logger = NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();

var activitySource = new ActivitySource("web1");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(activitySource);

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddNLog();
});

var resourceBuilder = ResourceBuilder
    .CreateDefault()
    .AddService(".Net Log Service");

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;

    logging.SetResourceBuilder(resourceBuilder)
        .AddOtlpExporter();
    
    // .AddOtlpExporter(otlOptions => {
    //     otlOptions.Endpoint = new Uri("http://localhost:59100");
    //     otlOptions.Protocol = OtlpExportProtocol.Grpc;
    // });
});

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: DiagnosticConfig.ServiceName))
    .WithTracing(opt =>
    {
        opt
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter();

        // .AddOtlpExporter(otlOptions =>
        // {
        //     otlOptions.Endpoint = new Uri("http://localhost:59100");
        //     otlOptions.Protocol = OtlpExportProtocol.Grpc;
        // });
    })
    .WithMetrics(opt =>
    {
        opt
            .AddMeter(DiagnosticConfig.Meter.Name)
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter();
        // .AddOtlpExporter(otlOptions =>
        // {
        //     otlOptions.Endpoint = new Uri("http://localhost:59100");
        //     otlOptions.Protocol = OtlpExportProtocol.Grpc;
        // });
    });

// var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
// if (useOtlpExporter)
// {
//     builder.Services.AddOpenTelemetry().UseOtlpExporter();
// }

builder.Services.AddControllers();

// builder.Services.AddOpenApiDocument();
// Register the Swagger services
builder.Services.AddSwaggerDocument(config =>
{
    config.DocumentName = "v1";
    config.Title = "My API";
    config.Version = "v1";
});

var app = builder.Build();

app.MapControllers();

app.MapGet("/", (ILogger<Program> logger) =>
{
    DiagnosticConfig.GetRequestCounter.Add(
        1,
        new KeyValuePair<string, object?>("get.path", "/"),
        new KeyValuePair<string, object?>("request", "get"));
    using var activity = activitySource.StartActivity("new activity /");
    logger.LogInformation("Default {Path} Get method", "/");
    return "Hello World!";
});

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    // Add OpenAPI 3.0 document serving middleware
    // Available at: http://localhost:<port>/swagger/v1/swagger.json
    app.UseOpenApi();

    // Add web UIs to interact with the document
    // Available at: http://localhost:<port>/swagger
    app.UseSwaggerUi(); // UseSwaggerUI Protected by if (env.IsDevelopment())
}

app.Run();