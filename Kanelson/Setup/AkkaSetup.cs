using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Akka.Persistence.Azure.Hosting;
using Azure.Identity;
using Kanelson.Actors;
using Kanelson.Actors.Questions;
using Kanelson.Actors.Rooms;
using Kanelson.Actors.Templates;
using Kanelson.Hubs;
using Kanelson.Services;
using Microsoft.AspNetCore.SignalR;
using Serilog.Core;


namespace Kanelson.Setup;

public static class AkkaSetup
{
    public static void AddAkkaSetup(this IHostBuilder hostBuilder, Logger logger)
    {
        const string actorSystemName = "Kanelson";

        hostBuilder.ConfigureServices((ctx, services) =>
        {
            
            var extractor = new MessageExtractor(100);

            var defaultShardOptions = new ShardOptions()
            {
                Role = actorSystemName,
                PassivateIdleEntityAfter = TimeSpan.FromMinutes(1),
                ShouldPassivateIdleEntities = true,
                RememberEntities = false,
            };

            services.AddAkka(actorSystemName, (akkaBuilder) =>
            {
                akkaBuilder
                    .ConfigureLoggers(setup =>
                    {
                        setup.ClearLoggers();
                        setup.AddLoggerFactory();
                    })
                    .BootstrapNetwork(ctx.Configuration, actorSystemName, logger)
                    .WithShardRegion<User>(nameof(User),
                        User.Props,
                        extractor,
                        defaultShardOptions
                    )
                    .WithShardRegion<UserQuestions>(nameof(UserQuestions),
                        UserQuestions.Props,
                        extractor,
                        defaultShardOptions)
                    .WithShardRegion<TemplateIndex>(nameof(TemplateIndex),
                        TemplateIndex.Props,
                        extractor,
                        defaultShardOptions)
                    .WithSingleton<RoomIndex>("room-index", (_,_,sp) =>
                         Props.Create<RoomIndex>("room-index",
                            sp.GetService<IHubContext<RoomHub>>(),
                            sp.GetService<IHubContext<RoomLobbyHub>>(),
                            sp.GetService<IUserService>()), new ClusterSingletonOptions
                    {
                        Role = actorSystemName,
                    });

                if (ctx.HostingEnvironment.IsDevelopment())
                {
                    akkaBuilder
                        .WithAzureTableJournal(ctx.Configuration.GetConnectionString("TableStorage")!)
                        .WithAzureBlobsSnapshotStore(ctx.Configuration.GetConnectionString("BlobStorage")!);
                    

                }
                else
                {
                    var credentials = new DefaultAzureCredential();

                    akkaBuilder
                        .WithAzureTableJournal(new Uri(ctx.Configuration.GetConnectionString("TableStorage")!),
                            defaultAzureCredential: credentials)
                        .WithAzureBlobsSnapshotStore(new Uri(ctx.Configuration.GetConnectionString("BlobStorage")!),
                            defaultAzureCredential: credentials);
                }

            });
        });
    }
}