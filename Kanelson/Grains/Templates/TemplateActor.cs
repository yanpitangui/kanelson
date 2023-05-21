using Akka.Actor;
using Kanelson.Contracts.Models;

namespace Kanelson.Grains.Templates;

public class TemplateActor : ReceiveActor
{

    private TemplateState _state;
    public TemplateActor()
    {
        _state = new TemplateState();
        
        Receive<Upsert>(o =>
        {
            _state.Template = o.Template;
            _state.OwnerId = o.OwnerId;
        });

        Receive<GetOwner>(_ => Sender.Tell(_state.OwnerId));

        Receive<Delete>(_ => Self.Tell(PoisonPill.Instance));

        Receive<GetTemplate>(_ => Sender.Tell(_state.Template));

        
        // TODO: Retornar o verdadeiro ID do template
        Receive<GetSummary>(_ => Sender.Tell(new TemplateSummary(Guid.Empty, _state.Template.Name)));
    }
}

public record GetSummary;


public record GetTemplate;


public record GetOwner;

public record Delete;

public record Upsert(Template Template, string OwnerId);
