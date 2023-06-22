using System.Collections.Immutable;
using Akka.Actor;
using Akka.Persistence;
using Akka.Util;
using Kanelson.Models;
using OneOf.Types;

namespace Kanelson.Actors.Questions;

public class UserQuestionsActor : ReceivePersistentActor, IHasSnapshotInterval
{

    private UserQuestionsState _state;
    public override string PersistenceId { get; }
    
    public UserQuestionsActor(string userId)
    {
        PersistenceId = $"question-index-{userId}";
        _state = new UserQuestionsState();

        
        Recover<Question>(PersistAdd);
        
        Recover<Guid>(PersistRemove);
        
        Command<UpsertQuestion>(o =>
        {
            Persist(o.Question, PersistAdd);
        });


        Command<RemoveQuestion>(o =>
        {
            Persist(o.Id, PersistRemove);
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
        
        Command<SaveSnapshotSuccess>(success => {
            // soft-delete the journal up until the sequence # at
            // which the snapshot was taken
            DeleteMessages(success.Metadata.SequenceNr); 
            DeleteSnapshots(new SnapshotSelectionCriteria(success.Metadata.SequenceNr - 1));
        });
        
        Command<DeleteSnapshotsSuccess>(_ => { });
        Command<DeleteMessagesSuccess>(_ => { });
        
    }

    private void PersistAdd(Question question)
    {
        _state.Questions[question.Id] = question;
        ((IHasSnapshotInterval)this).SaveSnapshotIfPassedInterval(_state);
    }

    private void PersistRemove(Guid id)
    {
        _state.Questions.Remove(id);
        ((IHasSnapshotInterval)this).SaveSnapshotIfPassedInterval(_state);
    }

    public static Props Props(string userId)
    {
        return Akka.Actor.Props.Create<UserQuestionsActor>(userId);
    }
}


public record UpsertQuestion(Question Question);

public record RemoveQuestion(Guid Id);

public record GetQuestions(params Guid[] Ids);

public record GetQuestion(Guid Id);

public record GetQuestionsSummary
{
    private GetQuestionsSummary()
    {
    }

    public static GetQuestionsSummary Instance { get; } = new();
}