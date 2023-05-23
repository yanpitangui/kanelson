using System.Collections.Immutable;
using Akka.Actor;
using Akka.Persistence;
using Kanelson.Contracts.Models;

namespace Kanelson.Actors;



public class UserIndexActor : ReceivePersistentActor
{
   
    private UserIndexState _state;
    public UserIndexActor(string persistenceId)
    {
        PersistenceId = persistenceId;
        _state = new UserIndexState();


        Command<UpsertUser>(o =>
        {
            _state.Users.RemoveWhere(x => x.Id == o.Id);
            _state.Users.Add(new UserInfo(o.Id, o.Name));
            SaveSnapshot(_state);
        });
        Command<GetUserInfos>(o =>
        {
            var users = _state.Users.Where(x => o.Ids.Contains(x.Id)).ToImmutableArray();
            Sender.Tell(users);
        });
        
        Recover<SnapshotOffer>(o =>
        {
            if (o.Snapshot is UserIndexState state)
            {
                _state = state;
            }
        });
        
        Command<SaveSnapshotSuccess>(success => {
            // soft-delete the journal up until the sequence # at
            // which the snapshot was taken
            DeleteMessages(success.Metadata.SequenceNr); 
            DeleteSnapshots(new SnapshotSelectionCriteria(success.Metadata.SequenceNr - 1));

        });
        
        Command<DeleteSnapshotsSuccess>(_ => { });
        Command<DeleteMessagesSuccess>(_ => { });
    }
    
    public override string PersistenceId { get; }
}

public record UpsertUser(string Id, string Name);

public record GetUserInfos(params string[] Ids);