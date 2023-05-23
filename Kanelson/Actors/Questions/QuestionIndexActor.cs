using Akka.Actor;
using Akka.Persistence;

namespace Kanelson.Actors.Questions;

public class QuestionIndexActor : ReceivePersistentActor
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
                actorRef = Context.ActorOf(UserQuestionsActor.Props(o.UserId), $"user-questions-{o.UserId}");
                _children[o.UserId] = actorRef;
            }

            _state.Indexes.Add(o.UserId);
            Sender.Tell(actorRef);
            SaveSnapshot(_state);
        });
        
        Recover<SnapshotOffer>(o =>
        {
            if (o.Snapshot is QuestionIndexState state)
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


    }

}

public record GetRef(string UserId);
