using Akka.Actor;
using Akka.Persistence.TestKit;
using Akka.TestKit;
using Bogus;
using FluentAssertions;
using Kanelson.Actors.Templates;
using Kanelson.Models;
using Template = Kanelson.Actors.Templates.Template;

namespace Kanelson.Tests;

public class TemplateSpecs : PersistenceTestKit
{
    private readonly Guid TemplateId = Guid.NewGuid();
    private readonly TestActorRef<Template> _testActor;
    private static readonly Faker<Alternative> _alternativeGenerator = new Faker<Alternative>()
        .RuleFor(x => x.Correct, f => f.PickRandom(true, false))
        .RuleFor(x => x.Description, f => f.Lorem.Paragraph());
    private static readonly Faker<TemplateQuestion> _questionGenerator = new Faker<TemplateQuestion>()
        .RuleFor(x => x.Id, Guid.NewGuid())
        .RuleFor(x => x.Name,  f => f.Lorem.Sentence(5))
        .RuleFor(x => x.Points, f=> f.PickRandom(0, 1000, 2000))
        .RuleFor(x => x.ImageUrl, f=> f.Image.PlaceImgUrl())
        .RuleFor(x => x.Type, f => f.PickRandom<QuestionType>())
        .RuleFor(x => x.TimeLimit, f => f.PickRandom(5, 10, 20, 30, 60, 90, 120, 240))
        .RuleFor(x => x.Alternatives, f => f.Make(f.Random.Int(1, 5), () => _alternativeGenerator.Generate()));
    
    public TemplateSpecs()
    {
        _testActor = new TestActorRef<Template>(Sys, Template.Props(TemplateId));
    }
    
    [Fact]
    public async Task Upserted_template_info_should_return_in_get()
    {
        // arrange
        var template = new Models.Template
        {
            Id = TemplateId,
            Name = "Test template", Questions = _questionGenerator.Generate(5)
        };
        _testActor.Tell(new TemplateCommands.Upsert(template));

        // act
        var getTemplate = await _testActor.Ask<Models.Template>(TemplateQueries.GetTemplate.Instance);

        // assert
        getTemplate.Should().BeEquivalentTo(template);
    }
    
    [Fact]
    public async Task Upserted_template_basic_info_should_return_in_summary()
    {
        // arrange
        var template = new Models.Template
        {
            Id = TemplateId,
            Name = "Test template", 
            Questions = _questionGenerator.Generate(5)
        };
        _testActor.Tell(new TemplateCommands.Upsert(template));

        // act
        var getTemplate = await _testActor.Ask<TemplateSummary>(TemplateQueries.GetSummary.Instance);

        // assert
        getTemplate.Should().BeEquivalentTo(new  TemplateSummary(template.Id,template.Name));
    }

    [Fact]
    public async Task Restarting_actor_should_recover_previous_state()
    {
        // arrange
        var template = new Models.Template
        {
            Id = TemplateId,
            Name = "Test template", 
            Questions = _questionGenerator.Generate(5)
        };

        for (int i = 0; i < 10; i++)
        {
            template = template with
            {
                Name = $"Test template {i}",
                Questions = _questionGenerator.Generate(3)
            };
            _testActor.Tell(new TemplateCommands.Upsert(template));
        }

        var previousTemplate = await _testActor.Ask<Models.Template>(TemplateQueries.GetTemplate.Instance);
        
        // act
        await _testActor.GracefulStop(TimeSpan.FromSeconds(3));
        var recoveringActor = new TestActorRef<Template>(Sys, Template.Props(TemplateId));
        var recoveredTemplate = await recoveringActor.Ask<Models.Template>(TemplateQueries.GetTemplate.Instance);

        
        // assert
        recoveredTemplate.Should().BeEquivalentTo(previousTemplate);

    }


    
    
}