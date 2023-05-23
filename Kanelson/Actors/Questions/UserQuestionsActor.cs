using System.Collections.Immutable;
using Akka.Actor;
using Akka.Persistence;
using Kanelson.Contracts.Models;

namespace Kanelson.Actors.Questions;

public class UserQuestionsActor : ReceivePersistentActor
{

    private UserQuestionsState _state;
    public override string PersistenceId { get; }

    public UserQuestionsActor(string userId)
    {
        PersistenceId = userId;
        _state = new UserQuestionsState();

        Command<UpsertQuestion>(o =>
        {
            _state.Questions[o.Question.Id] = o.Question;
            SaveSnapshot(_state);
        });


        Command<DeleteQuestion>(o =>
        {
            _state.Questions.Remove(o.Id);
            SaveSnapshot(_state);
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

    public static Props Props(string userId)
    {
        return Akka.Actor.Props.Create(() => new UserQuestionsActor(userId));
    }
}


public record UpsertQuestion(Question Question);

public record DeleteQuestion(Guid Id);

public record GetQuestions(params Guid[] Ids);

public record GetQuestionsSummary();