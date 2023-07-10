using System.Collections.Immutable;
using Akka.Actor;
using Akka.Cluster.Sharding;
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
    private readonly IUserService _userService;
    private readonly IValidator<HashSet<Question>?> _questionValidator;
    private readonly IValidator<IBrowserFile> _fileValidator;
    private readonly IActorRef _userQuestions;

    public QuestionService(IUserService userService, 
        IValidator<IBrowserFile> fileValidator,
        IValidator<HashSet<Question>?> questionValidator,
        IActorRegistry actorRegistry)
    {
        _userService = userService;
        _userQuestions = actorRegistry.Get<UserQuestions>();
        _fileValidator = fileValidator;
        _questionValidator = questionValidator;
    }

    public ValidationResult SaveQuestion(Question question)
    {
        _userQuestions.Tell(MessageEnvelope(new UpsertQuestion(question)));
        return new ValidationResult();
    }

    public void RemoveQuestion(Guid id)
    {
        _userQuestions.Tell(MessageEnvelope(new RemoveQuestion(id)));
    }

    public async Task<Question> GetQuestion(Guid id)
    {
        var result = await _userQuestions.Ask<Option<Question>>(MessageEnvelope(new GetQuestion(id)));
        if (result.HasValue)
        {
            return result.Value;
        }
        throw new KeyNotFoundException();
    }

    public Task<ImmutableArray<QuestionSummary>> GetQuestionsSummary()
    {
        return _userQuestions.Ask<ImmutableArray<QuestionSummary>>(MessageEnvelope(Actors.Questions.GetQuestionsSummary.Instance));
    }

    public async Task<ImmutableArray<Question>> GetQuestions(HashSet<Guid> ids)
    {
        return await _userQuestions.Ask<ImmutableArray<Question>>(MessageEnvelope(new GetQuestions(ids.ToArray())));
    }

    public async Task<ValidationResult> UploadQuestions(IBrowserFile file)
    {
        var result = await _fileValidator.ValidateAsync(file);
        if (!result.IsValid) return result;

        await using var stream = file.OpenReadStream();
        var questionList = await JsonSerializer.DeserializeAsync<HashSet<Question>>(stream);

        result = await _questionValidator.ValidateAsync(questionList);
        if (!result.IsValid) return result;
        
        foreach (var question in questionList!) // Validação garante que não está nulo 
        {
            _userQuestions.Tell(MessageEnvelope(new UpsertQuestion(question)));
        }

        return new ValidationResult();
    }
    
    // TODO: Substituir por interface que contem o id do usuário
    private ShardingEnvelope MessageEnvelope<T>(T message) where T: class
    {
        return new ShardingEnvelope(_userService.CurrentUser, message);
    }
}