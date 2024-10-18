using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Persistence.Sql.Hosting;
using Kanelson.Domain.Questions;
using Kanelson.Domain.Rooms;
using Kanelson.Domain.Templates;
using Kanelson.Domain.Users;
using Kanelson.Extractors;
using Kanelson.Hubs;
using LinqToDB;
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
            
            var defaultShardOptions = new ShardOptions()
            {
                Role = actorSystemName,
                PassivateIdleEntityAfter = TimeSpan.FromMinutes(1),
                ShouldPassivateIdleEntities = true,
            };

            services.AddAkka(actorSystemName, (akkaBuilder) =>
            {
                var userMessageExtractor = new UserMessageExtractor(50);
                akkaBuilder
                    .ConfigureLoggers(setup =>
                    {
                        setup.ClearLoggers();
                        setup.AddLoggerFactory();
                    })
                    .BootstrapNetwork(ctx.Configuration, actorSystemName, logger)
                    .WithShardRegion<User>(nameof(User),
                        User.Props,
                        userMessageExtractor,
                        defaultShardOptions
                    )
                    .WithShardRegion<UserQuestions>(nameof(UserQuestions),
                        UserQuestions.Props,
                        userMessageExtractor,
                        defaultShardOptions)
                    .WithShardRegion<RoomTemplateIndex>(nameof(RoomTemplateIndex),
                        RoomTemplateIndex.Props,
                        userMessageExtractor,
                        defaultShardOptions)
                    .WithShardRegion<Room>(nameof(Room),
                        (_, _, sp) =>
                            (identifier) => 
                                Room.Props(identifier, (IHubContext)sp.GetService<IHubContext<RoomHub>>(),
                                    sp.GetService<IUserService>()),
                        new RoomMessageExtractor(50),
                        defaultShardOptions)
                    .WithSingleton<RoomIndex>("room-index", (_,ar,sp) =>
                        RoomIndex.Props("room-index",
                            ar.Get<Room>(),
                            (IHubContext)sp.GetService<IHubContext<RoomLobbyHub>>(),
                            sp.GetService<IUserService>()), new ClusterSingletonOptions
                    {
                        Role = actorSystemName,
                    });

                akkaBuilder.WithSqlPersistence(
                    connectionString: ctx.Configuration.GetConnectionString("KanelsonDb")!,
                    providerName: ProviderName.PostgreSQL);

            });
        });
    }
}