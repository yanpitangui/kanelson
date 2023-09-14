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


        Recover<TemplateCommands.Upsert>(HandleUpsert);
        Command<TemplateCommands.Upsert>(upsert => Persist(upsert, HandleUpsert));
        
        Command<TemplateQueries.GetTemplate>(_ => Sender.Tell(_state.Template));

        Command<TemplateQueries.GetSummary>(_ => Sender.Tell(new TemplateSummary(_id, _state.Template.Name)));
        
                
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

    private void HandleUpsert(TemplateCommands.Upsert o)
    {
        _state.Template = o.Template;
        SaveSnapshotIfPassedInterval(_state);
    }

    public static Props Props(Guid templateId)
    {
        return Akka.Actor.Props.Create<Template>(templateId);
    }

}