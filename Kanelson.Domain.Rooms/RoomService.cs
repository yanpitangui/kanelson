using Akka.Actor;
using Akka.Hosting;
using IdGen;
using Kanelson.Common;
using Kanelson.Domain.Rooms.Models;
using Kanelson.Domain.Templates;
using Kanelson.Domain.Users;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading.Channels;

namespace Kanelson.Domain.Rooms;

public class RoomService : IRoomService
{
    private readonly ActorRegistry _actorRegistry;
    private readonly IIdGenerator<long> _idGenerator;
    private readonly IRoomTemplateService _templateService;
    private readonly IUserService _userService;

    public RoomService(IUserService userService,
        ActorRegistry actorRegistry,
        IIdGenerator<long> idGenerator,
        IRoomTemplateService templateService)
    {
        _userService = userService;
        _actorRegistry = actorRegistry;
        _idGenerator = idGenerator;
        _templateService = templateService;
    }

    public async Task<string> CreateRoom(Guid templateId, string roomName, CancellationToken ct = default)
    {
        var roomId = _idGenerator.CreateId().ToString(NumberFormatInfo.InvariantInfo);
        var template = await _templateService.GetTemplate(templateId);
        var roomShard = await _actorRegistry.GetAsync<Room>(ct);
        await roomShard.Ask<Akka.Done>(
            new RoomCommands.SetBase(roomId, roomName, _userService.CurrentUser, template), ct);
        return roomId;
    }

    public async Task<RoomStatus> GetCurrentState(string roomId, CancellationToken ct = default)
    {
        var roomShardingRef = await GetRoomShardingRef(roomId, ct);
        return await roomShardingRef.Ask<RoomStatus>(new RoomQueries.GetCurrentState(roomId), ct);
    }

    public async Task<ChannelReader<ImmutableArray<BasicRoomInfo>>> GetRoomsChannel(CancellationToken ct = default)
    {
        var indexActor = _actorRegistry.Get<AllRoomsIndexActor>();
        return await indexActor.Ask<ChannelReader<ImmutableArray<BasicRoomInfo>>>(
            AllRoomsIndexMessages.GetRoomsReader.Instance, ct);
    }

    public async Task<RoomSummary> Get(string roomId, CancellationToken ct = default)
    {
        var roomShardingRef = await GetRoomShardingRef(roomId, ct);
        return await roomShardingRef.Ask<RoomSummary>(new RoomQueries.GetSummary(roomId), ct);
    }

    public async Task<CurrentQuestionInfo> GetCurrentQuestion(string roomId, CancellationToken ct = default)
    {
        var roomShardingRef = await GetRoomShardingRef(roomId, ct);
        return await roomShardingRef.Ask<CurrentQuestionInfo>(new RoomQueries.GetCurrentQuestion(roomId), ct);
    }

    public async Task NextQuestion(string roomId, CancellationToken ct = default)
    {
        var roomShardingRef = await GetRoomShardingRef(roomId, ct);
        roomShardingRef.Tell(new RoomCommands.NextQuestion(roomId));
    }

    public async Task Start(string roomId, CancellationToken ct = default)
    {
        var roomShardingRef = await GetRoomShardingRef(roomId, ct);
        roomShardingRef.Tell(new RoomCommands.Start(roomId));
    }

    public async Task Delete(string roomId, CancellationToken ct = default)
    {
        var roomShardingRef = await GetRoomShardingRef(roomId, ct);
        var owner = await roomShardingRef.Ask<string>(new RoomQueries.GetOwner(roomId), ct);
        if (!string.Equals(owner, _userService.CurrentUser, StringComparison.OrdinalIgnoreCase))
            throw new ApplicationException("You are not the room's owner");
        roomShardingRef.Tell(new RoomCommands.Shutdown(roomId));
    }

    public async Task Answer(string roomId, CancellationToken ct = default, params Guid[] alternativeIds)
    {
        var roomShardingRef = await GetRoomShardingRef(roomId, ct);
        roomShardingRef.Tell(new RoomCommands.SendUserAnswer(roomId, _userService.CurrentUser, alternativeIds));
    }

    public async Task ExtendTime(string roomId, int seconds, CancellationToken ct = default)
    {
        var roomShardingRef = await GetRoomShardingRef(roomId, ct);
        roomShardingRef.Tell(new RoomCommands.ExtendTime(roomId, seconds));
    }

    private async Task<IActorRef> GetRoomShardingRef(string roomId, CancellationToken ct = default)
    {
        var exists = await _actorRegistry.Get<AllRoomsIndexActor>()
            .Ask<bool>(new AllRoomsIndexMessages.CheckRoomExists(roomId), ct);
        if (!exists) throw new ActorNotFoundException();
        return await _actorRegistry.GetAsync<Room>(ct);
    }
}
