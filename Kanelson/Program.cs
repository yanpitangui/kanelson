using System.Security.Claims;
using Kanelson.Grains;
using Kanelson.Grains.Games;
using Kanelson.Hubs;
using Kanelson.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.ResponseCompression;
using MudBlazor.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans;
using Orleans.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.SystemConsole.Themes;
using Shared.Grains;

var builder = WebApplication.CreateBuilder(args);
// remove default logging providers
builder.Logging.ClearProviders();
// Serilog configuration        
var logger = ConfigureBaseLogging()
    .CreateLogger();

// Register Serilog
builder.Logging.AddSerilog(logger);

// Add services to the container.
builder.Services.AddAuthentication(o =>
    {
        o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(o =>
    {
        o.LoginPath = "/signin";
        o.LogoutPath = "/signout";
    })
    .AddGitHub(o =>
    {
        o.ClientId = builder.Configuration.GetRequiredSection("GithubAuth")["ClientId"]!;
        o.ClientSecret = builder.Configuration.GetRequiredSection("GithubAuth")["ClientSecret"]!;
        o.CallbackPath = "/signin-github";
        o.Scope.Add("read:user");

        
        // Atualiza as informações do usuário quando ele faz login com sucesso.
        o.Events.OnCreatingTicket = async (context) =>
        {
            var user = context.Principal;
            var factory = context.HttpContext.RequestServices.GetRequiredService<IGrainFactory>();
            var userGrain = factory.GetGrain<IUserManagerGrain>(0);
            await userGrain.Upsert(user.FindFirstValue(ClaimTypes.NameIdentifier), 
                user.FindFirstValue(ClaimTypes.Name));
        };
    });
builder.Services.AddOptions();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});


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

app.UseResponseCompression();

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

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapHub<GameHub>("/gameHub");
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();


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