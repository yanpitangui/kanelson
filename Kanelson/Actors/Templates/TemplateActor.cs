using Akka.Actor;
using Kanelson.Contracts.Models;

namespace Kanelson.Actors.Templates;

public class TemplateActor : ReceiveActor
{

    private TemplateState _state;
    private Guid _id;
    
    public TemplateActor(Guid templateId)
    {
        _state = new TemplateState();
        _id = templateId;
        Receive<Upsert>(o =>
        {
            _state.Template = o.Template;
            _state.OwnerId = o.OwnerId;
        });

        Receive<GetOwner>(_ => Sender.Tell(_state.OwnerId));
        
        Receive<GetTemplate>(_ => Sender.Tell(_state.Template));

        
        // TODO: Retornar o verdadeiro ID do template
        Receive<GetSummary>(_ => Sender.Tell(new TemplateSummary(_id, _state.Template.Name)));
    }
    
    public static Props Props(Guid templateId)
    {
        return Akka.Actor.Props.Create(() => new TemplateActor(templateId));
    }
}

public record GetSummary;


public record GetTemplate;


public record GetOwner;

public record Upsert(Template Template, string OwnerId);
