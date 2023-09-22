using Akka.Actor;
using Akka.Persistence.TestKit;
using Akka.TestKit;
using FluentAssertions;
using Kanelson.Domain.Templates;
using Kanelson.Domain.Templates.Models;
using System.Collections.Immutable;

namespace Kanelson.Tests;

public class TemplateIndexSpecs : PersistenceTestKit
{
    private const string UserId = "12345";
    private readonly TestActorRef<RoomTemplateIndex> _testActor;

    public TemplateIndexSpecs()
    {
        _testActor = new TestActorRef<RoomTemplateIndex>(Sys, RoomTemplateIndex.Props(UserId), name: UserId);
    }


    [Fact]
    public async Task Getting_summaries_when_nothing_is_registered_should_return_empty_array()
    {
        // act
        ImmutableArray<TemplateSummary> summaries = await GetSummaries();

        // assert
        summaries.Should().BeEmpty();
    }


    [Fact]
    public async Task Getting_same_id_twice_should_return_same_ref()
    {
        // arrange
        var id = Guid.NewGuid();
        var actor1 = await _testActor.Ask<IActorRef>(new RoomTemplateQueries.GetRef(UserId, id));
        
        // act
        var actor2 = await _testActor.Ask<IActorRef>(new RoomTemplateQueries.GetRef(UserId, id));

        actor1.IsNobody().Should().BeFalse();
        actor2.IsNobody().Should().BeFalse();
        actor1.Should().Be(actor2);
    }


    [Fact]
    public async Task Registering_template_should_return_in_summaries()
    {
        // arrange
        var id = Guid.NewGuid();
        var actor1 = await _testActor.Ask<IActorRef>(new RoomTemplateQueries.GetRef(UserId, id));
        actor1.Tell(new RoomTemplateCommands.Upsert(new Template
        {
            Id = id,
            Name = "Test template to be registered",
        }));
        
        // act
        var summaries = await GetSummaries();
        summaries.Should().BeEquivalentTo(new TemplateSummary(id, "Test template to be registered"));

    }
     
    [Fact]
    public async Task Unregistering_template_should_remove_from_summaries()
    {
        // arrange
        var id = Guid.NewGuid();
        var actor1 = await _testActor.Ask<IActorRef>(new RoomTemplateQueries.GetRef(UserId, id));
        actor1.Tell(new RoomTemplateCommands.Upsert(new Template
        {
            Id = id,
            Name = "Test template to be removed",
        }));
        
        // act
        _testActor.Tell(new RoomTemplateQueries.Unregister(UserId, id));
        var summaries = await GetSummaries();
        summaries.Select(x => x.Id).Should().NotContain(id);

    }

    [Fact]
    public async Task Restarting_actor_should_recover_previous_state()
    {
        // arrange
        var ids = Enumerable.Range(1, 5).Select(x => Guid.NewGuid()).ToArray();
        foreach (var id in ids)
        {
            var actor = await _testActor.Ask<IActorRef>(new RoomTemplateQueries.GetRef(UserId, id));
            actor.Tell(new RoomTemplateCommands.Upsert(new Template
            {
                Id = id,
                Name = $"Template {id}",
            }));
        }
        _testActor.Tell(new RoomTemplateQueries.Unregister(UserId, ids.First()));
        var summary = await GetSummaries();
        // act
        await _testActor.GracefulStop(TimeSpan.FromSeconds(3));
        var recoveringActor = new TestActorRef<RoomTemplateIndex>(Sys, RoomTemplateIndex.Props(UserId));
        var recoveredSummary = await recoveringActor.Ask<ImmutableArray<TemplateSummary>>(new RoomTemplateQueries.GetAllSummaries(UserId));
        
        // assert
        recoveredSummary.Should().BeEquivalentTo(summary);

    }
    
    
    private async Task<ImmutableArray<TemplateSummary>> GetSummaries()
    {
        var summaries = await _testActor.Ask<ImmutableArray<TemplateSummary>>(new RoomTemplateQueries.GetAllSummaries(UserId));
        return summaries;
    }

}