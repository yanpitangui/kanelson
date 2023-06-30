using System.Collections.Immutable;
using Akka.Actor;
using Akka.Persistence;
using Kanelson.Models;

namespace Kanelson.Actors;



public sealed class UserIndexActor : ReceivePersistentActor, IHasSnapshotInterval
{
   
    private UserIndexState _state;
    public UserIndexActor(string persistenceId)
    {
        PersistenceId = persistenceId;
        _state = new UserIndexState();
        
        Recover<UpsertUser>(HandleUpsert);
        
        Command<UpsertUser>(o =>
        {
            Persist(o, HandleUpsert);
        });
        Command<GetUserInfos>(o =>
        {
            var users = _state.Users.Where(x => o.Ids.Contains(x.Id, StringComparer.OrdinalIgnoreCase)).ToImmutableArray();
            Sender.Tell(users);
        });
        
        Recover<SnapshotOffer>(o =>
        {
            if (o.Snapshot is UserIndexState state)
            {
                _state = state;
            }
        });
        
        Command<SaveSnapshotSuccess>(_ => { });
    }

    private void HandleUpsert(UpsertUser user)
    {
        _state.Users.RemoveWhere(x => string.Equals(x.Id, user.Id, StringComparison.OrdinalIgnoreCase));
        _state.Users.Add(new UserInfo(user.Id, user.Name));
        ((IHasSnapshotInterval) this).SaveSnapshotIfPassedInterval(_state);
    }

    public override string PersistenceId { get; }
}

public record UpsertUser(string Id, string Name);

public record GetUserInfos(params string[] Ids);