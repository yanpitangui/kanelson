using System.Collections.Immutable;
using Orleans;
using Orleans.Runtime;
using Shared.Grains.Questions;
using Shared.Models;

namespace Kanelson.Grains.Questions;

public class QuestionGrain : Grain, IQuestionGrain
{
    private readonly IPersistentState<QuestionState> _questions;

    public QuestionGrain(
        [PersistentState("questions", "kanelson-storage")]
        IPersistentState<QuestionState> questions)
    {
        _questions = questions;
    }

    public async Task SaveQuestion(Question question)
    {
        if (_questions.State.Questions.ContainsKey(question.Id))
        {
            _questions.State.Questions[question.Id] = question;
        }
        else
        {
            _questions.State.Questions.Add(question.Id, question);
        }
        await _questions.WriteStateAsync();
    }

    public Task<ImmutableArray<QuestionSummary>> GetQuestionsSummary()
    {
        return Task.FromResult(_questions.State.Questions.Values.Select(x => new QuestionSummary
        {
            Id = x.Id,
            Name = x.Name
        }).ToImmutableArray());
    }

    public Task<ImmutableArray<Question>> GetQuestions()
    {
        return Task.FromResult(_questions.State.Questions.Values.ToImmutableArray());
    }

    public async Task<bool> DeleteQuestion(Guid id)
    {
        var deleted = _questions.State.Questions.Remove(id);
        await _questions.WriteStateAsync();
        return deleted;
    }

    public Task<Question?> GetQuestion(Guid id)
    {
        var exists = _questions.State.Questions.TryGetValue(id, out var question);
        if (exists)
        {
            return Task.FromResult(question);
        }

        return Task.FromResult((Question?)null);
    }
}

[Serializable]
public record QuestionState
{
    public Dictionary<Guid, Question> Questions { get; set; } = new();
}