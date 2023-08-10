using Akka.Actor;
using Akka.Persistence;
using Kanelson.Models;

namespace Kanelson.Actors.Templates;

public class Template : BaseWithSnapshotFrequencyActor
{

    public override string PersistenceId { get; }

    private TemplateState _state;
    private readonly Guid _id;
    
    public Template(Guid templateId)
    {
        PersistenceId = $"template-{templateId}";
        _state = new TemplateState();
        _id = templateId;


        Recover<Upsert>(HandleUpsert);
        Command<Upsert>(upsert => Persist(upsert, HandleUpsert));
        
        Command<GetTemplate>(_ => Sender.Tell(_state.Template));

        Command<GetSummary>(_ => Sender.Tell(new TemplateSummary(_id, _state.Template.Name)));
        
                
        Recover<SnapshotOffer>(o =>
        {
            if (o.Snapshot is TemplateState state)
            {
                _state = state;
            }
        });
        
        Command<SaveSnapshotSuccess>(_ => { });

        Command<DeleteSnapshotsSuccess>(_ => { });
        Command<DeleteMessagesSuccess>(_ => { });
        
        Command<ShutdownCommand>(_ =>
        {
            DeleteMessages(Int64.MaxValue);
            DeleteSnapshots(SnapshotSelectionCriteria.Latest);
            Context.Stop(Self);
        });
    }

    private void HandleUpsert(Upsert o)
    {
        _state.Template = o.Template;
        SaveSnapshotIfPassedInterval(_state);
    }

    public static Props Props(Guid templateId)
    {
        return Akka.Actor.Props.Create<Template>(templateId);
    }

}

public record GetSummary
{
    private GetSummary()
    {
    }

    public static GetSummary Instance { get; } = new();
}

public record GetTemplate
{
    private GetTemplate()
    {
    }

    public static GetTemplate Instance { get; } = new();
}

public record Upsert(Models.Template Template);
