using System.Collections.Immutable;
using Akka.Actor;

namespace Kanelson.Grains.Templates;

public class TemplateManagerActor : ReceiveActor
{

    private readonly TemplateManagerState _state;
    public TemplateManagerActor()
    {
        _state = new TemplateManagerState();


        Receive<Register>(o =>
        {
            _state.Items.Add(o.Id);
        });

        Receive<Unregister>(o =>
        {
            _state.Items.Remove(o.Id);
        });


        Receive<Exists>(o => Sender.Tell(_state.Items.Contains(o.Id)));

        Receive<GetAll>(o => Sender.Tell(ImmutableArray.CreateRange(_state.Items)));
        
    }
    
}

public record Register(Guid Id);

public record Unregister(Guid Id);

public record Exists(Guid Id);

public record GetAll;