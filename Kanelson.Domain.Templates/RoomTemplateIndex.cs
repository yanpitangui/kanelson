using Akka.Actor;
using Akka.Persistence;
using Kanelson.Common;
using Kanelson.Domain.Templates.Models;
using System.Collections.Immutable;

namespace Kanelson.Domain.Templates;

public class RoomTemplateIndex : BaseWithSnapshotFrequencyActor
{
    public override string PersistenceId { get; }

    private readonly Dictionary<Guid, IActorRef> _children;

    private RoomTemplateIndexState _state;

    public RoomTemplateIndex(string userId)
    {
        PersistenceId = $"template-index-{userId}";
        _state = new RoomTemplateIndexState();

        _children = new();
        
        Recover<RoomTemplateQueries.Unregister>(unregister =>
        {
            HandleUnregister(unregister);
            var exists = _children.TryGetValue(unregister.Id, out var actorRef);
            if (!actorRef.IsNobody() && exists)
            {
                actorRef.Tell(ShutdownCommand.Instance);
            }
        });

        Command<RoomTemplateQueries.Unregister>(o =>
        {
            var exists = _children.TryGetValue(o.Id, out var actorRef);
            if (!actorRef.IsNobody() && exists)
            {
                actorRef.Tell(ShutdownCommand.Instance);
            }
            
            Persist(o, HandleUnregister);
        });
        
        Recover<RoomTemplateCommands.Register>(HandleRegister);

        Command<RoomTemplateQueries.GetRef>(o =>
        {
            
            var exists = _children.TryGetValue(o.Id, out var actorRef);
            if (actorRef.IsNobody() || !exists)
            {
                actorRef = GetChildTemplateActorRef(o.Id);
                _children[o.Id] = actorRef;
            }

            if (!_state.Items.Contains(o.Id))
            {
                Persist(new RoomTemplateCommands.Register(o.Id), HandleRegister);
            }
            Sender.Tell(actorRef);
        });
        
        
        Command<RoomTemplateQueries.GetAllSummaries>(_ =>
        {
            
            var sender = Sender;
            
            var tasks = _state.Items.Select(x => _children[x].Ask<TemplateSummary>(RoomTemplateQueries.GetSummary.Instance));
            
            async Task<ImmutableArray<TemplateSummary>> ExecuteWork()
            {
                var result = await Task.WhenAll(tasks);
                return result.ToImmutableArray();
            }


            ExecuteWork().PipeTo(sender);
        });

        Command<RoomTemplateQueries.Exists>(o => Sender.Tell(_state.Items.Contains(o.Id)));
        
        Recover<SnapshotOffer>(o =>
        {
            if (o.Snapshot is RoomTemplateIndexState state)
            {
                _state = state;
            }
        });
        
        Command<SaveSnapshotSuccess>(_ => { });
        
    }

    protected override void OnReplaySuccess()
    {
        base.OnReplaySuccess();
        foreach (var item in _state.Items)
        {
            _children[item] = GetChildTemplateActorRef(item);
        }
    }

    private void HandleRegister(RoomTemplateCommands.Register r)
    {
        if (_state.Items.Add(r.Id))
        {
            SaveSnapshotIfPassedInterval(_state);
        }
    }

    private void HandleUnregister(RoomTemplateQueries.Unregister r)
    {
        _state.Items.Remove(r.Id);
        SaveSnapshotIfPassedInterval(_state);
    }

    private static IActorRef GetChildTemplateActorRef(Guid id)
    {
        return Context.ActorOf(RoomTemplate.Props(id), $"template-{id}");
    }


    public static Props Props(string userId)
    {
        return Akka.Actor.Props.Create<RoomTemplateIndex>(userId);
    }


}