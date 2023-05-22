using System.Collections.Immutable;
using Akka.Actor;
using Kanelson.Contracts.Models;

namespace Kanelson.Grains.Questions;

public class UserQuestionsActor : ReceiveActor
{

    private UserQuestionsState _state;
    //public override string PersistenceId { get; }

    public UserQuestionsActor(string userId)
    {
        //PersistenceId = userId;
        _state = new UserQuestionsState();

        Receive<UpsertQuestion>(o =>
        {
            _state.Questions[o.Question.Id] = o.Question;
        });


        Receive<DeleteQuestion>(o =>
        {
            _state.Questions.Remove(o.Id);
        });

        Receive<GetQuestionsSummary>(o =>
        {
            Sender.Tell(_state.Questions.Values.Select(x => new QuestionSummary
            {
                Id = x.Id,
                Name = x.Name
            }).ToImmutableArray());
        });

        Receive<GetQuestions>(o =>
        {
            Sender.Tell(_state.Questions.Values.Where(x => o.Ids.Contains(x.Id)).ToImmutableArray());
        });
        
    }

    public static Props Props(string userId)
    {
        return Akka.Actor.Props.Create(() => new UserQuestionsActor(userId));
    }
}


public record UpsertQuestion(Question Question);

public record DeleteQuestion(Guid Id);

public record GetQuestions(params Guid[] Ids);

public record GetQuestionsSummary();