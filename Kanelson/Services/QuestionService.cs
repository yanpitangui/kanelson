﻿using System.Collections.Immutable;
using System.Security.Claims;
using FluentValidation.Results;
using Orleans;
using Shared.Grains.Questions;
using Shared.Models;

namespace Kanelson.Services;

public class QuestionService : IQuestionService
{
    private readonly IGrainFactory _grainFactory;
    private readonly string _currentUser;

    public QuestionService(IGrainFactory grainFactory, IHttpContextAccessor httpContextAccessor)
    {
        _currentUser = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
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

    public async Task<ImmutableArray<QuestionSummary>> GetQuestionsSummary()
    {
        var grain = _grainFactory.GetGrain<IQuestionGrain>(_currentUser);
        return await grain.GetQuestionsSummary();
    }

    public async Task<ImmutableArray<Question>> GetQuestions(HashSet<Guid>? ids = null)
    {
        var grain = _grainFactory.GetGrain<IQuestionGrain>(_currentUser);
        return await grain.GetQuestions(ids);
    }
}


public interface IQuestionService
{
    public Task<ValidationResult> SaveQuestion(Question question);
    public Task<ImmutableArray<QuestionSummary>> GetQuestionsSummary();

    Task<bool> DeleteQuestion(Guid id);
    Task<Question?> GetQuestion(Guid id);
    Task<ImmutableArray<Question>> GetQuestions(HashSet<Guid>? ids = null);
}