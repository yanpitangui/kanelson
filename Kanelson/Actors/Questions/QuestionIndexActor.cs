using Akka.Actor;
using Akka.Persistence;

namespace Kanelson.Actors.Questions;

public class QuestionIndexActor : ReceivePersistentActor, IHasSnapshotInterval
{

    private readonly Dictionary<string, IActorRef> _children;

    private QuestionIndexState _state;
    
    public override string PersistenceId { get; }

    
    public QuestionIndexActor(string persistenceId)
    {
        PersistenceId = persistenceId;
        _children = new();

        _state = new QuestionIndexState();
        Command<GetRef>(o =>
        {
            var exists = _children.TryGetValue(o.UserId, out var actorRef);
            if (Equals(actorRef, ActorRefs.Nobody) || !exists)
            {
                actorRef = GetChildQuestionActor(o.UserId);
                _children[o.UserId] = actorRef;
            }
            
            Persist(o.UserId, HandleAddUser);
            
            Sender.Tell(actorRef);
        });

        Recover<string>(s =>
        {
            HandleAddUser(s);
            if (!_children.TryGetValue(s, out var actorRef) || actorRef.Equals(ActorRefs.Nobody))
            {
                _children[s] = GetChildQuestionActor(s);
            }
        });
        
        Recover<SnapshotOffer>(o =>
        {
            if (o.Snapshot is QuestionIndexState state)
            {
                _state = state;
                foreach (var item in _state.Index)
                {
                    _children[item] = GetChildQuestionActor(item);
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

    private void HandleAddUser(string user)
    {
        _state.Index.Add(user);
        ((IHasSnapshotInterval) this).SaveSnapshotIfPassedInterval(_state);
    }

    private static IActorRef GetChildQuestionActor(string userId)
    {
        return Context.ActorOf(UserQuestionsActor.Props(userId), $"user-questions-{userId}");
    }
}

public record GetRef(string UserId);