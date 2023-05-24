using System.Diagnostics;
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


namespace Kanelson.Setup;

public static class AkkaSetup
{
    public static IHostBuilder AddAkkaSetup(this IHostBuilder hostBuilder, string actorSystemName)
    {
        hostBuilder.ConfigureServices((ctx, services) =>
        {
            
            var akkaSection = ctx.Configuration.GetSection("Akka");

            var connString = ctx.Configuration.GetConnectionString("Postgres");

            var hostName = akkaSection.GetValue<string>("ClusterIp", "localhost");

            var port = akkaSection.GetValue("ClusterPort", 7918);


            services.AddAkka(actorSystemName, (akkaBuilder) =>
            {
                Debug.Assert(connString != null);
                akkaBuilder.WithRemoting(hostName, port)
                    // Add after fixing timeouts
                    // .WithSqlPersistence(connectionString: connString, providerName: ProviderName.PostgreSQL15, autoInitialize: true,
                    //     schemaName: "public")
                    .WithActors((system, registry, sp) =>
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
                    });

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