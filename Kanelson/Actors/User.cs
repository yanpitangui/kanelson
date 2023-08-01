using Akka.Actor;
using Akka.Persistence;
using Kanelson.Models;

namespace Kanelson.Actors;



public sealed class User : BaseWithSnapshotFrequencyActor
{
   
    private UserInfo _state;
    public User(string userId)
    {
        PersistenceId = userId;
        _state = new UserInfo(userId);
        
        Recover<UpsertUser>(HandleUpsert);
        
        Command<UpsertUser>(o =>
        {
            Persist(o, HandleUpsert);
        });
        Command<GetUserInfo>(_ =>
        {
            Sender.Tell(_state);
        });
        
        Recover<SnapshotOffer>(o =>
        {
            if (o.Snapshot is UserInfo state)
            {
                _state = state;
            }
        });
        
        Command<SaveSnapshotSuccess>(_ => { });
    }

    private void HandleUpsert(UpsertUser user)
    {
        _state.Name = user.Name;
        SaveSnapshotIfPassedInterval(_state);
    }

    public override string PersistenceId { get; }
    
    public static Props Props(string userId)
    {
        return Akka.Actor.Props.Create<User>(userId);
    }
}

public record UpsertUser(string Name);

public record GetUserInfo
{
    private GetUserInfo()
    {
    }

    public static GetUserInfo Instance { get; } = new();
}