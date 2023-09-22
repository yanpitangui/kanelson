using FluentValidation.Results;
using Microsoft.AspNetCore.Components.Forms;
using System.Collections.Immutable;

namespace Kanelson.Domain.Questions;

public interface IQuestionService
{
    public ValidationResult SaveQuestion(Question question);
    public Task<ImmutableArray<QuestionSummary>> GetQuestionsSummary();

    void RemoveQuestion(Guid id);
    Task<Question> GetQuestion(Guid id);
    Task<ImmutableArray<Question>> GetQuestions(HashSet<Guid> ids);
    Task<ValidationResult> UploadQuestions(IBrowserFile file);
}