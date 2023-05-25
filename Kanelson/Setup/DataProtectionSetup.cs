using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;

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
            
            var redisConn = ctx.Configuration.GetConnectionString("Redis");

            if (!string.IsNullOrWhiteSpace(redisConn))
            {
                dataProtectionBuilder.PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect(
                    redisConn
                ), $"DataProtection-Keys-{ctx.HostingEnvironment.EnvironmentName}");

            }
        });
    }     
}