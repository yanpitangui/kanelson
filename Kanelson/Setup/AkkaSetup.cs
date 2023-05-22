using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Kanelson.Grains;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Host;

namespace Kanelson.Setup;

public static class AkkaSetup
{
    public static IHostBuilder AddAkkaSetup(this IHostBuilder hostBuilder, string actorSystemName)
    {
        hostBuilder.ConfigureServices((ctx, services) =>
        {
            
            var akkaSection = ctx.Configuration.GetSection("Akka");


            var hostName = akkaSection.GetValue<string>("ClusterIp", "localhost");

            var port = akkaSection.GetValue("ClusterPort", 0);

            var seeds = akkaSection.GetValue("ClusterSeeds", new[] { $"akka.tcp://{actorSystemName}@localhost:7918" })!
                .ToArray();

            services.AddAkka(actorSystemName, (config, provider) =>
            {
                config.WithRemoting(hostName, port)
                    .WithClustering()
                    .AddPetabridgeCmd(cmd =>
                    {
                        cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
                        cmd.RegisterCommandPalette(ClusterCommands.Instance);
                    }).WithActors((system, registry) =>
                    {
                        var userIndexActor = system.ActorOf(Props.Create(() => new UserIndexActor("user-index")), 
                            "user-index");
                        registry.Register<UserIndexActor>(userIndexActor);
                    });
            });
        });


        return hostBuilder;
    }
}