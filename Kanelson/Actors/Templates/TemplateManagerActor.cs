using System.Collections.Immutable;
using Akka.Actor;

namespace Kanelson.Actors.Templates;

public class TemplateManagerActor : ReceiveActor
{

    private readonly Dictionary<Guid, IActorRef> _children;

    private readonly TemplateManagerState _state;
    public TemplateManagerActor(string userId)
    {
        _state = new TemplateManagerState();

        _children = new();

        Receive<Register>(o =>
        {
            _state.Items.Add(o.Id);
        });

        Receive<Unregister>(o =>
        {
            _state.Items.Remove(o.Id);
            var exists = _children.TryGetValue(o.Id, out var actorRef);
            if (!Equals(actorRef, ActorRefs.Nobody) && !exists)
            {
                actorRef.Tell(PoisonPill.Instance);

            }
        });

        Receive<GetRef>(o =>
        {
            var exists = _children.TryGetValue(o.Id, out var actorRef);
            if (Equals(actorRef, ActorRefs.Nobody) || !exists)
            {
                actorRef = Context.ActorOf(TemplateActor.Props(o.Id), $"template-{o.Id}");
                _children[o.Id] = actorRef;
            }

            _state.Items.Add(o.Id);
            Sender.Tell(actorRef);
        });

        Receive<Exists>(o => Sender.Tell(_state.Items.Contains(o.Id)));
        
        Receive<GetAll>(o => Sender.Tell(ImmutableArray.CreateRange(_state.Items)));
        
    }
    
    
    
    public static Props Props(string userId)
    {
        return Akka.Actor.Props.Create(() => new TemplateManagerActor(userId));
    }
    
}


public record Exists(Guid Id);

public record GetRef(Guid Id);

public record Register(Guid Id);

public record Unregister(Guid Id);

public record GetAll;