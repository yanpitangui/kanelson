﻿using System.Collections.Immutable;
using Akka.Actor;
using Akka.Hosting;
using FluentValidation.Results;
using Kanelson.Contracts.Models;
using Kanelson.Grains.Questions;

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

    public async Task DeleteQuestion(Guid id)
    {
        var index = await _actorRegistry.GetAsync<QuestionIndexActor>();
        var userQuestionsActor = await index.Ask<IActorRef>(new GetRef(_userService.CurrentUser));
        userQuestionsActor.Tell(new DeleteQuestion(id));
    }

    public async Task<Question?> GetQuestion(Guid id)
    {
        var index = await _actorRegistry.GetAsync<QuestionIndexActor>();
        var userQuestionsActor = await index.Ask<IActorRef>(new GetRef(_userService.CurrentUser));
        var result = await userQuestionsActor.Ask<ImmutableArray<Question>>(new GetQuestions(id));
        return result.FirstOrDefault();
    }

    public async Task<ImmutableArray<QuestionSummary>> GetQuestionsSummary()
    {
        var index = await _actorRegistry.GetAsync<QuestionIndexActor>();
        var userQuestionsActor = await index.Ask<IActorRef>(new GetRef(_userService.CurrentUser));
        return await userQuestionsActor.Ask<ImmutableArray<QuestionSummary>>(new GetQuestionsSummary());
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

    Task DeleteQuestion(Guid id);
    Task<Question?> GetQuestion(Guid id);
    Task<ImmutableArray<Question>> GetQuestions(HashSet<Guid> ids);
}