using Kanelson.Grains;
using Kanelson.Services;
using MudBlazor.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans;
using Orleans.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.SystemConsole.Themes;

var builder = WebApplication.CreateBuilder(args);
// remove default logging providers
builder.Logging.ClearProviders();
// Serilog configuration        
var logger = ConfigureBaseLogging()
    .CreateLogger();

// Register Serilog
builder.Logging.AddSerilog(logger);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<GameService>();
builder.Services.AddSingleton<IQuestionService, QuestionService>();
builder.Services.AddMudServices();

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering()
        .UseMongoDBClient(builder.Configuration.GetConnectionString("MongoDb"))
        .AddMongoDBGrainStorage("kanelson-storage", options =>
        {
            options.DatabaseName = "Kanelson";
            options.ConfigureJsonSerializerSettings = settings =>
            {
                settings.NullValueHandling = NullValueHandling.Include;
                settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                settings.DefaultValueHandling = DefaultValueHandling.Populate;
            };
        });

    siloBuilder.ConfigureApplicationParts(parts =>
    {
        parts.AddApplicationPart(typeof(GameGrain).Assembly).WithReferences();
    });
    
    
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

LoggerConfiguration ConfigureBaseLogging()
{
    var loggerConfiguration = new LoggerConfiguration();
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing") return loggerConfiguration.MinimumLevel.Fatal();
    loggerConfiguration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Orleans", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Infrastructure", LogEventLevel.Warning)
        .Destructure.AsScalar<JObject>()
        .Destructure.AsScalar<JArray>()
        .WriteTo.Async(a => a.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}", theme: AnsiConsoleTheme.Code))
        .Enrich.WithExceptionDetails()
        .Enrich.FromLogContext();

    return loggerConfiguration;
}