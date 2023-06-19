using Akka.Actor;
using Akka.Hosting;
using Akka.Persistence.Azure.Hosting;
using Akka.Remote.Hosting;
using Azure.Identity;
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
            
            services.AddAkka(actorSystemName, (akkaBuilder) =>
            {
                akkaBuilder
                    .WithActors((system, registry, sp) =>
                    {
                        var userIndexActor = system.ActorOf(Props.Create<UserIndexActor>("user-index"),
                            "user-index");
                        registry.Register<UserIndexActor>(userIndexActor);


                        var userQuestionIndex =
                            system.ActorOf(Props.Create<QuestionIndexActor>("user-question-index"),
                                "user-question-index");

                        registry.Register<QuestionIndexActor>(userQuestionIndex);

                        var roomIndex =
                            system.ActorOf(Props.Create<RoomIndexActor>("room-index",
                                    sp.GetService<IHubContext<RoomHub>>(),
                                    sp.GetService<IUserService>()),
                                "room-index");

                        registry.Register<RoomIndexActor>(roomIndex);
                    });

                if (ctx.HostingEnvironment.IsDevelopment())
                {
                    akkaBuilder.WithRemoting("localhost", 7918)
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


        return hostBuilder;
    }
}