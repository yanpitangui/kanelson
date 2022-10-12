using Orleans;
using Shared.Models;

namespace Shared.Grains;

public interface IQuestionGrain : IGrainWithStringKey
{

    public Task SaveQuestion(Question question);
    public Task<List<QuestionSummary>> GetQuestions();
    public Task<bool> DeleteQuestion(Guid id);
    Task<Question?> GetQuestion(Guid id);
}