using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Discovery.KubernetesApi;
using Akka.Hosting;
using Akka.Management;
using Akka.Management.Cluster.Bootstrap;
using Akka.Remote.Hosting;
using Kanelson.Grains;
using Kanelson.Grains.Questions;

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
                    }).WithActors((system, registry) =>
                    {
                        var userIndexActor = system.ActorOf(Props.Create(() => new UserIndexActor("user-index")), 
                            "user-index");
                        registry.Register<UserIndexActor>(userIndexActor);


                        var userQuestionIndex =
                            system.ActorOf(Props.Create(() => new QuestionIndexActor("user-question-index")));
                        
                        registry.Register<QuestionIndexActor>(userQuestionIndex);
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