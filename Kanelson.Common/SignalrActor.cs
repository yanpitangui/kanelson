using Akka.Actor;
using Microsoft.AspNetCore.SignalR;

namespace Kanelson.Common;

public sealed class SignalrActor : ReceiveActor
{
    public SignalrActor(IHubContext hubContext)
    {
        Receive<SendSignalrGroupMessage>(o =>
        {
            var sender = Sender;
            hubContext.Clients.Group(o.GroupId).SendAsync(o.MessageName, o.Data).PipeTo(sender, Self);
        });

        Receive<SendSignalrUserMessage>(o =>
        {
            var sender = Sender;
            hubContext.Clients.User(o.UserId).SendAsync(o.MessageName, o.Data).PipeTo(sender, Self);
        });
        
        
    }

    public static Props Props(IHubContext context)
    {
        return Akka.Actor.Props.Create<SignalrActor>(context);
    } 
    

}

public record SendSignalrGroupMessage(string GroupId, string MessageName, object? Data = null);
    
public record SendSignalrUserMessage(string UserId, string MessageName, object? Data = null);