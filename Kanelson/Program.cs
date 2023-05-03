using System.Diagnostics;
using System.Security.Claims;
using Kanelson.Hubs;
using Kanelson.Services;
using Kanelson.Tracing;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.ResponseCompression;
using MudBlazor.Services;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;


Activity.DefaultIdFormat = ActivityIdFormat.W3C;

var builder = WebApplication.CreateBuilder(args);
// remove default logging providers
builder.Logging.ClearProviders();
// Serilog configuration        
var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

// Register Serilog
builder.Logging.AddSerilog(logger);

builder.Services.AddHealthChecks()
    .AddMongoDb(builder.Configuration.GetConnectionString("MongoDb")!);


builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.Secure = CookieSecurePolicy.Always;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});

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
            var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
            await userService.Upsert(user!.FindFirstValue(ClaimTypes.NameIdentifier)!, 
                user!.FindFirstValue(ClaimTypes.Name)!);
        };
    });

builder.Services.AddOptions();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddLocalization();
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});


var dbName = "Kanelson";
builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder
        .AddActivityPropagation()
        .UseMongoDBClient(builder.Configuration.GetConnectionString("MongoDb"))
        .UseLocalhostClustering()
        .UseDashboard(x =>
        {
            x.HostSelf = true;
            x.CounterUpdateIntervalMs = 10000;
        })
        .UseMongoDBReminders(opt =>
        {
            opt.DatabaseName = dbName;
        })
        .AddMongoDBGrainStorage("kanelson-storage", options =>
        {
            options.DatabaseName = dbName;
        });
});

var tracingOptions = builder.Configuration.GetSection("Tracing")
    .Get<TracingOptions>()!;


builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(rb => rb.AddService(serviceName: OpenTelemetryExtensions.ServiceName))
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("Microsoft.Orleans");
    }).WithTracing(telemetry =>
    {
        telemetry
            .AddSource(OpenTelemetryExtensions.ServiceName)
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: OpenTelemetryExtensions.ServiceName,
                        serviceVersion: OpenTelemetryExtensions.ServiceVersion))
            .AddAspNetCoreInstrumentation()
            .AddSource("Microsoft.Orleans.Application")
            .AddSource("Microsoft.Orleans.Runtime");

        if (tracingOptions.Enabled)
        {
            telemetry.AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(tracingOptions.Uri);
            });
        }
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

app.UseCookiePolicy();
app.UseRouting();

app.Map("/dashboard", x => x.UseOrleansDashboard());

app.MapControllers();
app.MapHub<RoomHub>("/roomHub");
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();
app.MapHealthChecks("/healthz");

var supportedCultures = new[] { "en-US", "pt-BR" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[1])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

await app.RunAsync();