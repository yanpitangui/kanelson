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
         _testActor = new TestActorRef<UserQuestions>(Sys, UserQuestions.Props(UserId), name: UserId);
         
     }

     [Fact]
     public async Task Upserted_question_should_return_in_summary()
     {
         // arrange
         var question = new Question {Name = "new question", Points = 1000,};
         _testActor.Tell(new UpsertQuestion(question));
         
         // act
         var questionSummary = await GetSummary();
         
         // assert
         questionSummary.Should().Contain(new QuestionSummary { Id = question.Id, Name = question.Name });
     }

     private async Task<ImmutableArray<QuestionSummary>> GetSummary()
     {
         return await _testActor.Ask<ImmutableArray<QuestionSummary>>(GetQuestionsSummary.Instance);
     }

     [Fact]
     public async Task Getting_existing_questionId_should_return_correct_question()
     {
         // arrange
         var question = new Question {Name = "new question", Points = 1000, Alternatives = new List<Alternative>(2)
         {
             new() { Correct = true, Description = "False"},
             new() { Correct = false, Description = "True"}
         }};
         _testActor.Tell(new UpsertQuestion(question));
         
         // act
         var result = await _testActor.Ask<Option<Question>>(new GetQuestion(question.Id));
         
         // assert
         await Verify(result.Value);
     }
     
     [Fact]
     public async Task Getting_non_existing_questionId_should_return_invalid_option()
     {
         // act
         var result = await _testActor.Ask<Option<Question>>(new GetQuestion(Guid.NewGuid()));
         
         // assert
         result.HasValue.Should().BeFalse();
     }

     [Fact]
     public async Task Getting_non_existing_ids_should_not_return_questions()
     {
         // arrange
         for (var i = 0; i < 10; i++)
         {
             _testActor.Tell(new UpsertQuestion(new Question {Name = $"question {i}"}));
         }
         
         // act
         var result = await _testActor.Ask<ImmutableArray<Question>>
             (new GetQuestions(Guid.NewGuid(), Guid.NewGuid(),
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
             _testActor.Tell(new UpsertQuestion(question));
             ids.Add(question.Id);
         }
         
         // act
         var result = await _testActor.Ask<ImmutableArray<Question>>
         (new GetQuestions(ids.Take(3).ToArray()));
         
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
             _testActor.Tell(new UpsertQuestion(question));
             ids.Add(question.Id);
         }
         
         // act
         _testActor.Tell(new RemoveQuestion(ids.First()));
         
         // assert
         var summary = await GetSummary();
         summary.Should().NotContain(x => x.Id == ids.First());

         var questions = await _testActor.Ask<ImmutableArray<Question>>(new GetQuestions(ids.First()));
         questions.Should().BeEmpty();

         var questionResult = await _testActor.Ask<Option<Question>>(new GetQuestion(ids.First()));
         questionResult.HasValue.Should().BeFalse();

     }
}