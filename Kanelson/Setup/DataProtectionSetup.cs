using Azure.Identity;
using Microsoft.AspNetCore.DataProtection;

namespace Kanelson.Setup;

public static class DataProtectionSetup
{
    public static IHostBuilder AddDataProtectionSetup(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices((ctx, services) =>
        {
            var dataProtectionBuilder = services.AddDataProtection()
                .SetApplicationName("Kanelson")
                .SetDefaultKeyLifetime(TimeSpan.FromDays(30));
            if (ctx.HostingEnvironment.EnvironmentName is not ("Testing" or "Development"))
            {
                // dataProtectionBuilder.PersistKeysToAzureBlobStorage(new Uri(ctx.Configuration.GetConnectionString("BlobStorage")!), new DefaultAzureCredential())
                //     .ProtectKeysWithAzureKeyVault(new Uri(ctx.Configuration.GetConnectionString("KeyVault")!), new DefaultAzureCredential());
            }
        });
    }     
}