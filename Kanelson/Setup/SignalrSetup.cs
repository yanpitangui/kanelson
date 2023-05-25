namespace Kanelson.Setup;

public static class SignalrSetup
{
    public static IHostBuilder AddSignalRSetup(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices((ctx, services) =>
        {
            var redisConn = ctx.Configuration.GetConnectionString("Redis");

            if (!string.IsNullOrWhiteSpace(redisConn))
            {
                services.AddSignalR().AddStackExchangeRedis(redisConn);
            }
        });
    }     
}