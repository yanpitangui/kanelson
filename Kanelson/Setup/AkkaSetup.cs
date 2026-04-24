using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Persistence.Sql.Hosting;
using Kanelson.Domain.Questions;
using Kanelson.Domain.Rooms;
using Kanelson.Domain.Rooms.Local;
using Kanelson.Domain.Templates;
using Kanelson.Domain.Users;
using Kanelson.Extractors;
using LinqToDB;
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
                    .AddHocon(@"
akka.actor {
  serializers {
    messagepack = ""Akka.Serialization.MessagePack.MsgPackSerializer, Akka.Serialization.MessagePack""
  }
  serialization-bindings {
    ""System.Object"" = messagepack
  }
}", Akka.Hosting.HoconAddMode.Append)
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
                    .WithSingleton<AllRoomsIndexActor>("all-rooms-index",
                        (_, _, _) => AllRoomsIndexActor.Props(),
                        new ClusterSingletonOptions { Role = actorSystemName })
                    .WithShardRegion<Room>(nameof(Room),
                        (_, ar, sp) =>
                            (identifier) =>
                                Room.Props(identifier,
                                    ar.Get<AllRoomsIndexActor>(),
                                    sp.GetService<IUserService>()),
                        new RoomMessageExtractor(50),
                        defaultShardOptions)
                    .WithActors((system, registry) =>
                    {
                        var roomShard = registry.Get<Room>();
                        var localRoomManager = system.ActorOf(
                            LocalRoomActorManager.Props(roomShard),
                            "local-room-manager");
                        registry.Register<LocalRoomActorManager>(localRoomManager);
                    });

                akkaBuilder.WithSqlPersistence(
                    connectionString: ctx.Configuration.GetConnectionString("KanelsonDb")!,
                    providerName: ProviderName.PostgreSQL);

            });
        });
    }
}
