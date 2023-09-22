using Akka.Actor;
using Akka.Persistence;
using Kanelson.Common;

namespace Kanelson.Domain.Users;



public sealed class User : BaseWithSnapshotFrequencyActor
{
   
    private UserInfo _state;
    public User(string userId)
    {
        PersistenceId = userId;
        _state = new UserInfo(userId);
        
        Recover<UserCommands.UpsertUser>(HandleUpsert);
        
        Command<UserCommands.UpsertUser>(o =>
        {
            Persist(o, HandleUpsert);
        });
        Command<UserQueries.GetUserInfo>(_ =>
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

    private void HandleUpsert(UserCommands.UpsertUser user)
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


