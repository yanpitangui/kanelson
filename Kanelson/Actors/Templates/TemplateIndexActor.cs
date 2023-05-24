using System.Collections.Immutable;
using Akka.Actor;
using Akka.Persistence;

namespace Kanelson.Actors.Templates;

public class TemplateIndexActor : ReceivePersistentActor, IHasSnapshotInterval
{
    public override string PersistenceId { get; }

    private readonly Dictionary<Guid, IActorRef> _children;

    private TemplateIndexState _state;

    public TemplateIndexActor(string userId)
    {
        PersistenceId = $"template-index-{userId}";
        _state = new TemplateIndexState();

        _children = new();
        
        Recover<Unregister>(HandleUnregister);

        Command<Unregister>(o =>
        {
            var exists = _children.TryGetValue(o.Id, out var actorRef);
            if (!Equals(actorRef, ActorRefs.Nobody) && !exists)
            {
                actorRef.Tell(ShutdownCommand.Instance);
            }
            
            Persist(o, HandleUnregister);
        });
        
        Recover<Guid>(o =>
        {
            HandleRegister(o);
            _children[o] = GetChildTemplateActorRef(o);
        });

        Command<GetRef>(o =>
        {
            
            var exists = _children.TryGetValue(o.Id, out var actorRef);
            if (Equals(actorRef, ActorRefs.Nobody) || !exists)
            {
                actorRef = GetChildTemplateActorRef(o.Id);
                _children[o.Id] = actorRef;
            }

            if (!_state.Items.Contains(o.Id))
            {
                Persist(o.Id, HandleRegister);
            }
            Sender.Tell(actorRef);
        });

        Command<Exists>(o => Sender.Tell(_state.Items.Contains(o.Id)));
        
        Command<GetAll>(o => Sender.Tell(ImmutableArray.CreateRange(_state.Items)));
        
        Recover<SnapshotOffer>(o =>
        {
            if (o.Snapshot is TemplateIndexState state)
            {
                _state = state;
                foreach (var item in _state.Items)
                {
                    _children[item] = GetChildTemplateActorRef(item);
                }
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
        
    }

    private void HandleRegister(Guid r)
    {
        if (_state.Items.Add(r))
        {
            ((IHasSnapshotInterval) this).SaveSnapshotIfPassedInterval(_state);
        }
    }

    private void HandleUnregister(Unregister r)
    {
        _state.Items.Remove(r.Id);
        ((IHasSnapshotInterval) this).SaveSnapshotIfPassedInterval(_state);
    }

    private IActorRef GetChildTemplateActorRef(Guid id)
    {
        return Context.ActorOf(TemplateActor.Props(id), $"template-{id}");
    }


    public static Props Props(string userId)
    {
        return Akka.Actor.Props.Create(() => new TemplateIndexActor(userId));
    }

}


public record Exists(Guid Id);

public record GetRef(Guid Id);

public record Register(Guid Id);

public record Unregister(Guid Id);

public record GetAll;