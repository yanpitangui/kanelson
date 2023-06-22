using System.Collections.Immutable;
using Akka.Actor;
using Akka.Hosting;
using Akka.Util;
using FluentValidation.Results;
using Kanelson.Actors.Questions;
using Kanelson.Contracts.Models;

namespace Kanelson.Services;

public class QuestionService : IQuestionService
{
    private readonly ActorRegistry _actorRegistry;
    private readonly IUserService _userService;

    public QuestionService(IUserService userService, ActorRegistry actorRegistry)
    {
        _userService = userService;
        _actorRegistry = actorRegistry;
    }

    public async Task<ValidationResult> SaveQuestion(Question question)
    {
        var index = await _actorRegistry.GetAsync<QuestionIndexActor>();
        var userQuestionsActor = await index.Ask<IActorRef>(new GetRef(_userService.CurrentUser));
        userQuestionsActor.Tell(new UpsertQuestion(question));
        return new ValidationResult();
    }

    public async Task RemoveQuestion(Guid id)
    {
        var index = await _actorRegistry.GetAsync<QuestionIndexActor>();
        var userQuestionsActor = await index.Ask<IActorRef>(new GetRef(_userService.CurrentUser));
        userQuestionsActor.Tell(new RemoveQuestion(id));
    }

    public async Task<Question> GetQuestion(Guid id)
    {
        var index = await _actorRegistry.GetAsync<QuestionIndexActor>();
        var userQuestionsActor = await index.Ask<IActorRef>(new GetRef(_userService.CurrentUser));
        var result = await userQuestionsActor.Ask<Option<Question>>(new GetQuestion(id));
        if (result.HasValue)
        {
            return result.Value;
        }
        throw new KeyNotFoundException();
    }

    public async Task<ImmutableArray<QuestionSummary>> GetQuestionsSummary()
    {
        var index = await _actorRegistry.GetAsync<QuestionIndexActor>();
        var userQuestionsActor = await index.Ask<IActorRef>(new GetRef(_userService.CurrentUser));
        return await userQuestionsActor.Ask<ImmutableArray<QuestionSummary>>(Actors.Questions.GetQuestionsSummary.Instance);
    }

    public async Task<ImmutableArray<Question>> GetQuestions(HashSet<Guid> ids)
    {
        var index = await _actorRegistry.GetAsync<QuestionIndexActor>();
        var userQuestionsActor = await index.Ask<IActorRef>(new GetRef(_userService.CurrentUser));
        return await userQuestionsActor.Ask<ImmutableArray<Question>>(new GetQuestions(ids.ToArray()));
    }
}


public interface IQuestionService
{
    public Task<ValidationResult> SaveQuestion(Question question);
    public Task<ImmutableArray<QuestionSummary>> GetQuestionsSummary();

    Task RemoveQuestion(Guid id);
    Task<Question> GetQuestion(Guid id);
    Task<ImmutableArray<Question>> GetQuestions(HashSet<Guid> ids);
}