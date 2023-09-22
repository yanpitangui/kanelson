using Akka.Actor;
using Akka.Persistence;
using Kanelson.Common;
using Kanelson.Domain.Templates.Models;

namespace Kanelson.Domain.Templates;

public class RoomTemplate : BaseWithSnapshotFrequencyActor
{

    public override string PersistenceId { get; }

    private RoomTemplateState _state;
    private readonly Guid _id;
    
    public RoomTemplate(Guid templateId)
    {
        PersistenceId = $"template-{templateId}";
        _state = new RoomTemplateState();
        _id = templateId;


        Recover<RoomTemplateCommands.Upsert>(HandleUpsert);
        Command<RoomTemplateCommands.Upsert>(upsert => Persist(upsert, HandleUpsert));
        
        Command<RoomTemplateQueries.GetTemplate>(_ => Sender.Tell(_state.Template));

        Command<RoomTemplateQueries.GetSummary>(_ => Sender.Tell(new TemplateSummary(_id, _state.Template.Name)));
        
                
        Recover<SnapshotOffer>(o =>
        {
            if (o.Snapshot is RoomTemplateState state)
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

    private void HandleUpsert(RoomTemplateCommands.Upsert o)
    {
        _state.Template = o.Template;
        SaveSnapshotIfPassedInterval(_state);
    }

    public static Props Props(Guid templateId)
    {
        return Akka.Actor.Props.Create<RoomTemplate>(templateId);
    }

}