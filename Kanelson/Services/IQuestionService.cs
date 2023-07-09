using System.Collections.Immutable;
using FluentValidation.Results;
using Kanelson.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Kanelson.Services;

public interface IQuestionService
{
    public ValidationResult SaveQuestion(Question question);
    public Task<ImmutableArray<QuestionSummary>> GetQuestionsSummary();

    void RemoveQuestion(Guid id);
    Task<Question> GetQuestion(Guid id);
    Task<ImmutableArray<Question>> GetQuestions(HashSet<Guid> ids);
    Task<ValidationResult> UploadQuestions(IBrowserFile file);
}