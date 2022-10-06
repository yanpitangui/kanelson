using FluentValidation.Results;
using Orleans;
using Shared;
using Shared.Models;

namespace Kanelson.Services;

public class QuestionService : IQuestionService
{
    private readonly IGrainFactory _grainFactory;
    private static Guid _currentUser = new("509db63c-22f9-468b-94a5-2ed50f5663a3");

    public QuestionService(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public async Task<ValidationResult> SaveQuestion(Question question)
    {
        var grain = _grainFactory.GetGrain<IQuestionGrain>(_currentUser);
        await grain.SaveQuestion(question);
        return new ValidationResult();
    }

    public async Task<bool> DeleteQuestion(Guid id)
    {
        var grain = _grainFactory.GetGrain<IQuestionGrain>(_currentUser);
        return await grain.DeleteQuestion(id);
    }

    public async Task<Question?> GetQuestion(Guid id)
    {
        var grain = _grainFactory.GetGrain<IQuestionGrain>(_currentUser);
        return await grain.GetQuestion(id);
    }

    public async Task<List<QuestionSummary>> GetQuestions()
    {
        var grain = _grainFactory.GetGrain<IQuestionGrain>(_currentUser);
        return await grain.GetQuestions();
    }
}


public interface IQuestionService
{
    public Task<ValidationResult> SaveQuestion(Question question);
    public Task<List<QuestionSummary>> GetQuestions();

    Task<bool> DeleteQuestion(Guid id);
    Task<Question?> GetQuestion(Guid id);
}