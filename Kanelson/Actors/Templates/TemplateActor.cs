using Akka.Actor;
using Akka.Persistence;
using Kanelson.Models;

namespace Kanelson.Actors.Templates;

public class TemplateActor : ReceivePersistentActor, IHasSnapshotInterval
{

    public override string PersistenceId { get; }

    private TemplateState _state;
    private readonly Guid _id;
    
    public TemplateActor(Guid templateId)
    {
        PersistenceId = $"template-{templateId}";
        _state = new TemplateState();
        _id = templateId;


        Recover<Upsert>(HandleUpsert);
        Command<Upsert>(upsert => Persist(upsert, HandleUpsert));

        Command<GetOwner>(_ => Sender.Tell(_state.OwnerId));
        
        Command<GetTemplate>(_ => Sender.Tell(_state.Template));

        Command<GetSummary>(_ => Sender.Tell(new TemplateSummary(_id, _state.Template.Name)));
        
                
        Recover<SnapshotOffer>(o =>
        {
            if (o.Snapshot is TemplateState state)
            {
                _state = state;
            }
        });
        
        Command<SaveSnapshotSuccess>(success => {
            // soft-delete the journal up until the sequence # at
            // which the snapshot was taken
            DeleteMessages(success.Metadata.SequenceNr); 
            DeleteSnapshots(new SnapshotSelectionCriteria(success.Metadata.SequenceNr - 1));
        });

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
        _state.OwnerId = o.OwnerId;
        ((IHasSnapshotInterval) this).SaveSnapshotIfPassedInterval(_state);
    }

    public static Props Props(Guid templateId)
    {
        return Akka.Actor.Props.Create<TemplateActor>(templateId);
    }

}

public record GetSummary;


public record GetTemplate;


public record GetOwner;

public record Upsert(Template Template, string OwnerId);
