using Akka.Actor;
using Akka.Persistence.TestKit;
using Akka.TestKit;
using Akka.Util;
using FluentAssertions;
using Kanelson.Actors.Questions;
using Kanelson.Models;
using System.Collections.Immutable;

namespace Kanelson.Tests;

[UsesVerify]
public class UserQuestionSpecs : PersistenceTestKit
{
    private const string UserId = "12345";
    private readonly TestActorRef<UserQuestions> _testActor;

    public UserQuestionSpecs()
    {
        _testActor = new TestActorRef<UserQuestions>(Sys, UserQuestions.Props(UserId));

    }

    [Fact]
    public async Task Upserted_question_should_return_in_summary()
    {
        // arrange
        var question = new Question {Name = "new question", Points = 1000,};
        _testActor.Tell(new QuestionCommands.UpsertQuestion(UserId, question));

        // act
        var questionSummary = await GetSummary();

        // assert
        questionSummary.Should().Contain(new QuestionSummary {Id = question.Id, Name = question.Name});
    }



    [Fact]
    public async Task Getting_existing_questionId_should_return_correct_question()
    {
        // arrange
        var question = new Question
        {
            Name = "new question",
            Points = 1000,
            Alternatives = new List<Alternative>(2)
            {
                new() {Correct = true, Description = "False"}, new() {Correct = false, Description = "True"}
            }
        };
        _testActor.Tell(new QuestionCommands.UpsertQuestion(UserId, question));

        // act
        var result = await _testActor.Ask<Option<Question>>(new QuestionQueries.GetQuestion(UserId, question.Id));

        // assert
        await Verify(result.Value);
    }

    [Fact]
    public async Task Getting_non_existing_questionId_should_return_invalid_option()
    {
        // act
        var result = await _testActor.Ask<Option<Question>>(new QuestionQueries.GetQuestion(UserId, Guid.NewGuid()));

        // assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public async Task Getting_non_existing_ids_should_not_return_questions()
    {
        // arrange
        for (var i = 0; i < 10; i++)
        {
            _testActor.Tell(new QuestionCommands.UpsertQuestion(UserId, new Question {Name = $"question {i}"}));
        }

        // act
        var result = await _testActor.Ask<ImmutableArray<Question>>
        (new QuestionQueries.GetQuestions(UserId, Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid()));

        // assert
        result.Should().BeEmpty();
    }


    [Fact]
    public async Task Getting_existing_ids_should_only_return_questions_with_ids_passed()
    {
        // arrange
        var ids = new List<Guid>();
        for (var i = 0; i < 10; i++)
        {
            var question = new Question() {Name = $"question {i}"};
            _testActor.Tell(new QuestionCommands.UpsertQuestion(UserId, question));
            ids.Add(question.Id);
        }

        // act
        var result = await _testActor.Ask<ImmutableArray<Question>>
            (new QuestionQueries.GetQuestions(UserId, ids.Take(3).ToArray()));

        // assert
        await Verify(result);
    }

    [Fact]
    public async Task Removing_question_should_remove_from_get_and_summary()
    {
        // arrange
        var ids = new List<Guid>();
        for (var i = 0; i < 10; i++)
        {
            var question = new Question() {Name = $"question {i}"};
            _testActor.Tell(new QuestionCommands.UpsertQuestion(UserId, question));
            ids.Add(question.Id);
        }

        // act
        _testActor.Tell(new QuestionCommands.RemoveQuestion(UserId, ids.First()));

        // assert
        var summary = await GetSummary();
        summary.Should().NotContain(x => x.Id == ids.First());

        var questions = await _testActor.Ask<ImmutableArray<Question>>(new QuestionQueries.GetQuestions(UserId, ids.First()));
        questions.Should().BeEmpty();

        var questionResult = await _testActor.Ask<Option<Question>>(new QuestionQueries.GetQuestion(UserId,ids.First()));
        questionResult.HasValue.Should().BeFalse();

    }

    [Fact]
    public async Task Restarting_actor_should_recover_previous_state()
    {
        
        // arrange
        var ids = new List<Guid>();
        for (var i = 0; i < 10; i++)
        {
            var question = new Question() {Name = $"question {i}"};
            _testActor.Tell(new QuestionCommands.UpsertQuestion(UserId, question));
            ids.Add(question.Id);
        }

        for (var i = 0; i < 3; i++)
        {
            _testActor.Tell(new QuestionCommands.RemoveQuestion(UserId, ids[i]));
        }

        var questionsSnapshot = await GetSummary();
        
        // act
        await _testActor.GracefulStop(TimeSpan.FromSeconds(3));
        
        var recoveringActor =  new TestActorRef<UserQuestions>(Sys, UserQuestions.Props(UserId));
        var questionsAfterRecovery = await GetSummary(recoveringActor);

        questionsSnapshot.Should().BeEquivalentTo(questionsAfterRecovery);

    }
    
    private async Task<ImmutableArray<QuestionSummary>> GetSummary(IActorRef? actorRef = null)
    {
        if(actorRef is not null) return await actorRef.Ask<ImmutableArray<QuestionSummary>>(new QuestionQueries.GetQuestionsSummary(UserId));
        return await _testActor.Ask<ImmutableArray<QuestionSummary>>(new QuestionQueries.GetQuestionsSummary(UserId));
    }

}