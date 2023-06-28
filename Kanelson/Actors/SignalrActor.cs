using Akka.Actor;
using Microsoft.AspNetCore.SignalR;

namespace Kanelson.Actors;

public sealed class SignalrActor : ReceiveActor
{
    public SignalrActor(IHubContext hubContext)
    {
        Receive<SendSignalrGroupMessage>(o =>
        {
            hubContext.Clients.Group(o.GroupId).SendAsync(o.MessageName, o.Data).PipeTo(Sender, Self);
        });

        Receive<SendSignalrUserMessage>(o =>
        {
            hubContext.Clients.User(o.UserId).SendAsync(o.MessageName, o.Data).PipeTo(Sender, Self);
        });
        
        
    }

    public static Props Props(IHubContext context)
    {
        return Akka.Actor.Props.Create<SignalrActor>(context);
    } 
    

}

public record SendSignalrGroupMessage(string GroupId, string MessageName, object? Data = null);
    
public record SendSignalrUserMessage(string UserId, string MessageName, object? Data = null);