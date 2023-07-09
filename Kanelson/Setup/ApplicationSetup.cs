using FluentValidation;
using IdGen;
using IdGen.DependencyInjection;
using Kanelson.Services;
using Kanelson.Validators;

namespace Kanelson.Setup;

public static class ApplicationSetup
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<ITemplateService, TemplateService>();
        services.AddSingleton<IQuestionService, QuestionService>();
        services.AddSingleton<IUserService, UserService>();
        services.AddSingleton<IRoomService, RoomService>();
        services.AddHttpContextAccessor();
        services.AddValidatorsFromAssemblyContaining<FileUploadValidator>(ServiceLifetime.Singleton);
        
        services.AddIdGen(0, () =>
        {
            var epoch = new DateTimeOffset(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var structure = new IdStructure(41, 10, 12);
            var options = new IdGeneratorOptions(structure, new DefaultTimeSource(epoch));
            return options;
        });


    }
}