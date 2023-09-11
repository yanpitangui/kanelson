using System.Collections.Immutable;
using Akka.Actor;
using Akka.Persistence;
using Akka.Util;
using Kanelson.Models;

namespace Kanelson.Actors.Questions;

public class UserQuestions : BaseWithSnapshotFrequencyActor
{

    private UserQuestionsState _state;
    public override string PersistenceId { get; }
    
    public UserQuestions(string userId)
    {
        PersistenceId = $"question-index-{userId}";
        _state = new UserQuestionsState();

        
        Recover<Question>(PersistAdd);
        
        Recover<RemoveQuestion>(PersistRemove);
        
        Command<UpsertQuestion>(o =>
        {
            Persist(o.Question, PersistAdd);
        });


        Command<RemoveQuestion>(o =>
        {
            Persist(o, PersistRemove);
        });

        Command<GetQuestionsSummary>(o =>
        {
            Sender.Tell(_state.Questions.Values.Select(x => new QuestionSummary
            {
                Id = x.Id,
                Name = x.Name
            }).ToImmutableArray());
        });


        Command<GetQuestions>(o =>
        {
            Sender.Tell(_state.Questions.Values.Where(x => o.Ids.Contains(x.Id)).ToImmutableArray());
        });
        
        Command<GetQuestion>(o =>
        {
            var found = _state.Questions.TryGetValue(o.Id, out var question);
            // Faz uma cópia simples da questão
            Sender.Tell(found ? Option<Question>.Create(question! with
            {
                Alternatives = question.Alternatives.Select(x => x with {}).ToList()
            }) : Option<Question>.Create(null!));
        });
        
        
        Recover<SnapshotOffer>(o =>
        {
            if (o.Snapshot is UserQuestionsState state)
            {
                _state = state;
            }
        });
        
        Command<SaveSnapshotSuccess>(_ => {});
        
    }

    private void PersistAdd(Question question)
    {
        _state.Questions[question.Id] = question;
        SaveSnapshotIfPassedInterval(_state);
    }

    private void PersistRemove(RemoveQuestion removeQuestion)
    {
        _state.Questions.Remove(removeQuestion.Id);
        SaveSnapshotIfPassedInterval(_state);
    }

    public static Props Props(string userId)
    {
        return Akka.Actor.Props.Create<UserQuestions>(userId);
    }
}


public sealed record UpsertQuestion(string UserId, Question Question) : IWithUserId;

public sealed record RemoveQuestion(string UserId, Guid Id): IWithUserId;

public sealed record GetQuestions(string UserId, params Guid[] Ids): IWithUserId;

public sealed record GetQuestion(string UserId, Guid Id) : IWithUserId;

public sealed record GetQuestionsSummary(string UserId) : IWithUserId;