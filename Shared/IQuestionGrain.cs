using Orleans;
using Shared.Models;

namespace Shared;

public interface IQuestionGrain : IGrainWithGuidKey
{

    public Task SaveQuestion(Question question);
    public Task<List<QuestionSummary>> GetQuestions();
    public Task<bool> DeleteQuestion(Guid id);
    Task<Question?> GetQuestion(Guid id);
}