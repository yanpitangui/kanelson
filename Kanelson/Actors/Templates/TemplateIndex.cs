using System.Buffers;
using System.Collections.Immutable;
using Akka.Actor;
using Akka.Persistence;
using Kanelson.Models;

namespace Kanelson.Actors.Templates;

public class TemplateIndex : BaseWithSnapshotFrequencyActor
{
    public override string PersistenceId { get; }

    private readonly Dictionary<Guid, IActorRef> _children;

    private TemplateIndexState _state;

    public TemplateIndex(string userId)
    {
        PersistenceId = $"template-index-{userId}";
        _state = new TemplateIndexState();

        _children = new();
        
        Recover<Unregister>(unregister =>
        {
            HandleUnregister(unregister);
            var exists = _children.TryGetValue(unregister.Id, out var actorRef);
            if (!Equals(actorRef, ActorRefs.Nobody) && exists)
            {
                actorRef.Tell(ShutdownCommand.Instance);
            }
        });

        Command<Unregister>(o =>
        {
            var exists = _children.TryGetValue(o.Id, out var actorRef);
            if (!Equals(actorRef, ActorRefs.Nobody) && exists)
            {
                actorRef.Tell(ShutdownCommand.Instance);
            }
            
            Persist(o, HandleUnregister);
        });
        
        Recover<Register>(HandleRegister);

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
                Persist(new Register(o.Id), HandleRegister);
            }
            Sender.Tell(actorRef);
        });
        
        
        Command<GetAllSummaries>(_ =>
        {
            
            var sender = Sender;
            
            var tasks = _state.Items.Select(x => _children[x].Ask<TemplateSummary>(GetSummary.Instance));
            
            async Task<ImmutableArray<TemplateSummary>> ExecuteWork()
            {
                var result = await Task.WhenAll(tasks);
                return result.ToImmutableArray();
            }


            ExecuteWork().PipeTo(sender);
        });

        Command<Exists>(o => Sender.Tell(_state.Items.Contains(o.Id)));
        
        Recover<SnapshotOffer>(o =>
        {
            if (o.Snapshot is TemplateIndexState state)
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

    private void HandleRegister(Register r)
    {
        if (_state.Items.Add(r.Id))
        {
            SaveSnapshotIfPassedInterval(_state);
        }
    }

    private void HandleUnregister(Unregister r)
    {
        _state.Items.Remove(r.Id);
        SaveSnapshotIfPassedInterval(_state);
    }

    private static IActorRef GetChildTemplateActorRef(Guid id)
    {
        return Context.ActorOf(Template.Props(id), $"template-{id}");
    }


    public static Props Props(string userId)
    {
        return Akka.Actor.Props.Create<TemplateIndex>(userId);
    }

    private sealed record Register(Guid Id);

}


public record GetAllSummaries
{
    private GetAllSummaries()
    {
    }

    public static GetAllSummaries Instance { get; } = new();
}

public record Exists(Guid Id);

public record GetRef(Guid Id);

public record Unregister(Guid Id);