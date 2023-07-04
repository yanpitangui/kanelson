using FluentValidation;
using Microsoft.AspNetCore.Components.Forms;

namespace Kanelson.Validators;

public class FileUploadValidator : AbstractValidator<IBrowserFile>
{
    public FileUploadValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.ContentType)
            .Equal("application/json");

        RuleFor(x => x.Size)
            .LessThanOrEqualTo(1_000_000);
    }
}