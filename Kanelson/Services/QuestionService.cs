using System.Collections.Immutable;
using Akka.Actor;
using Akka.Hosting;
using Akka.Util;
using FluentValidation;
using FluentValidation.Results;
using Kanelson.Actors.Questions;
using Kanelson.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.Text.Json;

namespace Kanelson.Services;

public class QuestionService : IQuestionService
{
    private readonly ActorRegistry _actorRegistry;
    private readonly IUserService _userService;
    private readonly IValidator<HashSet<Question>?> _questionValidator;
    private readonly IValidator<IBrowserFile> _fileValidator;

    public QuestionService(IUserService userService, ActorRegistry actorRegistry, 
        IValidator<IBrowserFile> fileValidator,
        IValidator<HashSet<Question>?> questionValidator)
    {
        _userService = userService;
        _actorRegistry = actorRegistry;
        _fileValidator = fileValidator;
        _questionValidator = questionValidator;
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

    public async Task<ValidationResult> UploadQuestions(IBrowserFile file)
    {
        var result = await _fileValidator.ValidateAsync(file);
        if (!result.IsValid) return result;

        await using var stream = file.OpenReadStream();
        var questionList = await JsonSerializer.DeserializeAsync<HashSet<Question>>(stream);

        result = await _questionValidator.ValidateAsync(questionList);
        if (!result.IsValid) return result;
        
        var index = await _actorRegistry.GetAsync<QuestionIndexActor>();
        var userQuestionsActor = await index.Ask<IActorRef>(new GetRef(_userService.CurrentUser));

        foreach (var question in questionList!) // Validação garante que não está nulo 
        {
            userQuestionsActor.Tell(new UpsertQuestion(question));
        }

        return new ValidationResult();
    }
}