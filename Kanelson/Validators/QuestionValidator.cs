using FluentValidation;
using Kanelson.Models;

namespace Kanelson.Validators;

public class QuestionListValidator : AbstractValidator<HashSet<Question>?>
{
    public QuestionListValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x).NotNull()
            .NotEmpty();
        RuleFor(x => x!.Count)
            .LessThanOrEqualTo(1000);

        RuleForEach(x =>
            x)
            .NotNull()
            .NotEmpty()
            .SetValidator(new QuestionValidator());
    }
}


file class QuestionValidator : AbstractValidator<Question>
{
    public QuestionValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.Id)
            .NotEmpty();


        When(x => string.IsNullOrWhiteSpace(x.ImageUrl), () =>
        {
            RuleFor(x => x.ImageUrl);

        });

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(3, 200);

        RuleFor(x => x.TimeLimit)
            .GreaterThanOrEqualTo(5)
            .LessThanOrEqualTo(240);

        RuleFor(x => x.Points)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(2000);

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.Alternatives.Count)
            .GreaterThanOrEqualTo(2);
        

        RuleForEach(x => x.Alternatives)
            .NotEmpty()
            .SetValidator(new AlternativeValidator());
        
        RuleFor(x => x.Alternatives)
            .NotEmpty()
            .Must(x => x.Exists(y => y.Correct));

    }
}

file class AlternativeValidator : AbstractValidator<Alternative>
{
    public AlternativeValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Description)
            .NotEmpty()
            .Length(4, 200);
    }
}