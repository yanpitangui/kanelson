using Akka.Actor;

namespace Kanelson.Grains.Questions;

public class QuestionIndexActor : ReceiveActor
{

    private readonly Dictionary<string, IActorRef> _children;

    private QuestionIndexState _state;
    
    public QuestionIndexActor(string persistenceId)
    {
        _children = new();

        _state = new QuestionIndexState();
        Receive<GetRef>(o =>
        {
            var exists = _children.TryGetValue(o.UserId, out var actorRef);
            if (Equals(actorRef, ActorRefs.Nobody) || !exists)
            {
                actorRef = Context.ActorOf(UserQuestionsActor.Props(o.UserId));
                _children[o.UserId] = actorRef;
            }

            _state.Indexes.Add(o.UserId);
            Sender.Tell(actorRef);
        });

    }
}

public record GetRef(string UserId);
