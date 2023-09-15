using Akka.Actor;
using Akka.Persistence.TestKit;
using Akka.TestKit;
using FluentAssertions;
using Kanelson.Actors.Users;
using Kanelson.Models;

namespace Kanelson.Tests;

public class UserSpecs : PersistenceTestKit
{
    private const string UserId = "12345";
    private readonly TestActorRef<User> _testActor;
    
    public UserSpecs()
    {
        _testActor = new TestActorRef<User>(Sys, User.Props(UserId));
    }


    [Fact]
    public async Task Upserting_user_should_set_name()
    {
        // act
        _testActor.Tell(new UserCommands.UpsertUser(UserId, "test user yay"));
        
        // assert
        var result = await _testActor.Ask<UserInfo>(new UserQueries.GetUserInfo(UserId));
        result.Should().BeEquivalentTo(new UserInfo(UserId) { Name = "test user yay"});
    }
    
    [Fact]
    public async Task Upserting_user_multiple_times_should_set_last_name_sent()
    {
        // arrange
        for (int i = 0; i < 20; i++)
        {
            _testActor.Tell(new UserCommands.UpsertUser(UserId, $"test user {i}"));
        }
        
        // act
        _testActor.Tell(new UserCommands.UpsertUser(UserId, "test user"));

        
        // assert
        var result = await _testActor.Ask<UserInfo>(new UserQueries.GetUserInfo(UserId));
        result.Should().BeEquivalentTo(new UserInfo(UserId) { Name = "test user"});
    }
    
    [Fact]
    public async Task Restarting_actor_should_recover_previous_state()
    {
        
        // arrange
        for (int i = 0; i < 20; i++)
        {
            _testActor.Tell(new UserCommands.UpsertUser(UserId, $"test user {i}"));
        }

        var userSnapshot = await GetUserInfo();
        
        // act
        await _testActor.GracefulStop(TimeSpan.FromSeconds(3));
        
        var recoveringActor = new TestActorRef<User>(Sys, User.Props(UserId));
        var userInfoAfterRecovery = await GetUserInfo(recoveringActor);

        // assert
        userSnapshot.Should().BeEquivalentTo(userInfoAfterRecovery);

    }

    private async Task<UserInfo> GetUserInfo(IActorRef? actorRef = null)
    {
        if(actorRef is not null) return await actorRef.Ask<UserInfo>(new UserQueries.GetUserInfo(UserId));
        return await _testActor.Ask<UserInfo>(new UserQueries.GetUserInfo(UserId));
    }
}