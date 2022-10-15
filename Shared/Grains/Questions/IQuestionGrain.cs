using Orleans;
using Shared.Models;

namespace Shared.Grains.Questions;

public interface IQuestionGrain : IGrainWithStringKey
{

    public Task SaveQuestion(Question question);
    public Task<ImmutableArray<QuestionSummary>> GetQuestionsSummary();
    public Task<bool> DeleteQuestion(Guid id);
    Task<Question?> GetQuestion(Guid id);
    Task<ImmutableArray<Question>> GetQuestions(HashSet<Guid>? ids = null);
}