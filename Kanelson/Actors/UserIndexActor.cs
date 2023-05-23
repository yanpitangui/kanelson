using Akka.Actor;
using Kanelson.Contracts.Models;

namespace Kanelson.Actors;



public class UserIndexActor : ReceiveActor
{
    //public override string PersistenceId { get; }
    
    private UserIndexState _state;
    public UserIndexActor(string persistenceId)
    {
        _state = new UserIndexState();


        Receive<UpserUser>(o =>
        {
            _state.Users.RemoveWhere(x => x.Id == o.Id);
            _state.Users.Add(new UserInfo(o.Id, o.Name));
        });
        Receive<GetUserInfos>(o =>
        {
            Sender.Tell(_state.Users.Where(x => o.Ids.Contains(x.Id)));
        });
        //PersistenceId = "user-index";
    }
}

public record UpserUser(string Id, string Name);

public record GetUserInfos(params string[] Ids);