using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Discovery.KubernetesApi;
using Akka.Hosting;
using Akka.Management;
using Akka.Management.Cluster.Bootstrap;
using Akka.Remote.Hosting;
using Kanelson.Actors;
using Kanelson.Actors.Questions;
using Kanelson.Actors.Rooms;
using Kanelson.Hubs;
using Kanelson.Services;
using Microsoft.AspNetCore.SignalR;
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

            var port = akkaSection.GetValue("ClusterPort", 7918);

            var seeds = akkaSection.GetValue("ClusterSeeds", new[] { $"akka.tcp://{actorSystemName}@localhost:7918" })!
                .ToArray();

            services.AddAkka(actorSystemName, (akkaBuilder, provider) =>
            {
                akkaBuilder.WithRemoting(hostName, port)
                    .WithClustering(new ClusterOptions()
                    {
                        Roles = new []{ actorSystemName }, SeedNodes = seeds
                    }).WithActors((system, registry, sp) =>
                    {
                        var userIndexActor = system.ActorOf(Props.Create(() => new UserIndexActor("user-index")), 
                            "user-index");
                        registry.Register<UserIndexActor>(userIndexActor);


                        var userQuestionIndex =
                            system.ActorOf(Props.Create(() => new QuestionIndexActor("user-question-index")), 
                                "user-question-index");
                        
                        registry.Register<QuestionIndexActor>(userQuestionIndex);
                        
                        var roomIndex =
                            system.ActorOf(Props.Create(() => new RoomIndexActor("room-index", 
                                    sp.GetService<IHubContext<RoomHub>>(),
                                    sp.GetService<IUserService>())), 
                                "room-index");
                        
                        registry.Register<RoomIndexActor>(roomIndex);
                    })
                    .AddPetabridgeCmd(cmd =>
                    {
                        cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
                        cmd.RegisterCommandPalette(ClusterCommands.Instance);
                    });;

                if (ctx.HostingEnvironment.IsProduction())
                {
                    akkaBuilder
                        .WithAkkaManagement()
                        .WithClusterBootstrap(serviceName: actorSystemName)
                        .WithKubernetesDiscovery(actorSystemName);
                }
                    

            });
        });


        return hostBuilder;
    }
}