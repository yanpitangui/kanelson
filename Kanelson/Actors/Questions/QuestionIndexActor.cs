using Akka.Actor;
using Akka.Persistence;

namespace Kanelson.Actors.Questions;

public class QuestionIndexActor : BaseWithSnapshotFrequencyActor
{

    private readonly Dictionary<string, IActorRef> _children;

    private QuestionIndexState _state;
    
    public override string PersistenceId { get; }

    
    public QuestionIndexActor(string persistenceId)
    {
        PersistenceId = persistenceId;
        
        _children = new(StringComparer.OrdinalIgnoreCase);

        _state = new QuestionIndexState();
        Command<GetRef>(o =>
        {
            var exists = _children.TryGetValue(o.UserId, out var actorRef);
            if (Equals(actorRef, ActorRefs.Nobody) || !exists)
            {
                actorRef = GetChildQuestionActor(o.UserId);
                _children[o.UserId] = actorRef;
            }

            if (!_state.Index.Contains(o.UserId))
            {
                Persist(o.UserId, HandleAddUser);
            }
            
            Sender.Tell(actorRef);
        });

        Recover<string>(HandleAddUser);
        
        Recover<SnapshotOffer>(o =>
        {
            if (o.Snapshot is QuestionIndexState state)
            {
                _state = state;
            }
        });
        
        Command<SaveSnapshotSuccess>(_ => { });
    }

    private void HandleAddUser(string user)
    {
        if (_state.Index.Add(user))
        {
            SaveSnapshotIfPassedInterval(_state);
        }
    }
    
    protected override void OnReplaySuccess()
    {
        base.OnReplaySuccess();
        foreach (var item in _state.Index)
        {
            _children[item] = GetChildQuestionActor(item);
        }
    }

    private static IActorRef GetChildQuestionActor(string userId)
    {
        return Context.ActorOf(UserQuestionsActor.Props(userId), $"user-questions-{userId}");
    }

}

public record GetRef(string UserId);
