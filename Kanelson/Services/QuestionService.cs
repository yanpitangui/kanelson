using System.Collections.Immutable;
using Akka.Actor;
using FluentValidation.Results;
using Kanelson.Contracts.Models;
using Kanelson.Grains.Questions;

namespace Kanelson.Services;

public class QuestionService : IQuestionService
{
    private readonly ActorSystem _actorSystem;
    private readonly IUserService _userService;

    public QuestionService(IUserService userService, ActorSystem actorSystem)
    {
        _userService = userService;
        _actorSystem = actorSystem;
    }

    public Task<ValidationResult> SaveQuestion(Question question)
    {
        var actor = _actorSystem.ActorOf(UserQuestionsActor.Props(_userService.CurrentUser));
        actor.Tell(new UpserQuestion(question));
        return Task.FromResult(new ValidationResult());
    }

    public Task DeleteQuestion(Guid id)
    {
        var actor = _actorSystem.ActorOf(UserQuestionsActor.Props(_userService.CurrentUser));
        actor.Tell(new DeleteQuestion(id));
        return Task.CompletedTask;
    }

    public async Task<Question?> GetQuestion(Guid id)
    {
        var actor = _actorSystem.ActorOf(UserQuestionsActor.Props(_userService.CurrentUser));
        var result = await actor.Ask<ImmutableArray<Question>>(new GetQuestions(id));
        return result.FirstOrDefault();
    }

    public async Task<ImmutableArray<QuestionSummary>> GetQuestionsSummary()
    {
        var actor = _actorSystem.ActorOf(UserQuestionsActor.Props(_userService.CurrentUser));
        return await actor.Ask<ImmutableArray<QuestionSummary>>(new GetQuestionsSummary());
    }

    public async Task<ImmutableArray<Question>> GetQuestions(HashSet<Guid> ids)
    {
        var actor = _actorSystem.ActorOf(UserQuestionsActor.Props(_userService.CurrentUser));
        return await actor.Ask<ImmutableArray<Question>>(new GetQuestions(ids.ToArray()));
    }
}


public interface IQuestionService
{
    public Task<ValidationResult> SaveQuestion(Question question);
    public Task<ImmutableArray<QuestionSummary>> GetQuestionsSummary();

    Task DeleteQuestion(Guid id);
    Task<Question?> GetQuestion(Guid id);
    Task<ImmutableArray<Question>> GetQuestions(HashSet<Guid> ids);
}