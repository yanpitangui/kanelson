using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace Kanelson.Setup;

/// <summary>
/// Conjunto de extensões para padronizar acesso a configurações no keyvault
/// </summary>
public static class KeyVaultSetup
{
    /// <summary>
    /// Adiciona configurações advindas do keyvault
    /// </summary>
    /// <param name="builder">Objeto que centraliza informações de host</param>
    /// <returns></returns>
    public static IHostBuilder AddKeyVaultConfigurationSetup(this IHostBuilder builder)
    {
        return builder.ConfigureAppConfiguration((context, configurationBuilder) =>
        {
            var intermediateConfig = configurationBuilder.Build();
            if (context.HostingEnvironment.EnvironmentName is not ("Testing" or "Development"))
            {
                configurationBuilder.AddAzureAppConfiguration(options =>
                {
                    options.Connect(new Uri(intermediateConfig.GetConnectionString("AppConfig")!), new DefaultAzureCredential())
                        .ConfigureKeyVault(kv =>
                        {
                            kv.SetCredential(new DefaultAzureCredential());
                        })
                        .Select(KeyFilter.Any, context.HostingEnvironment.EnvironmentName);
                });
            }
        });
    }
}