using System.Diagnostics;
using System.Security.Claims;
using IdGen;
using IdGen.DependencyInjection;
using Kanelson.Hubs;
using Kanelson.Services;
using Kanelson.Setup;
using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor.Services;
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

builder.Host.AddKeyVaultConfigurationSetup();

builder.Services.AddHealthChecks();


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
        o.Events.OnCreatingTicket = (context) =>
        {
            var user = context.Principal;
            var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
            userService.Upsert(user!.FindFirstValue(ClaimTypes.NameIdentifier)!, 
                user!.FindFirstValue(ClaimTypes.Name)!);
            
            return Task.CompletedTask;
        };
    });


const string dbName = "Kanelson";

builder.Services.AddOptions();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor().AddHubOptions(o =>
{
    o.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    o.HandshakeTimeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddLocalization();

builder.Services.AddIdGen(0, () =>
{
    var epoch = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    var structure = new IdStructure(41, 10, 12);
    var options = new IdGeneratorOptions(structure, new DefaultTimeSource(epoch));
    return options;
});

builder.Host.AddAkkaSetup(dbName);

builder.Host.AddOpenTelemetrySetup();

builder.Host.AddDataProtectionSetup();

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

app.UseCookiePolicy();
app.UseRouting();

app.MapControllers();
app.MapHub<RoomHub>("/roomHub");
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/healthz");

var supportedCultures = new[] { "en-US", "pt-BR" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[1])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

await app.RunAsync();