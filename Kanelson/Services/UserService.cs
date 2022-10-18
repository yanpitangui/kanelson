using System.Collections.Immutable;
using System.Security.Claims;
using Orleans;
using Shared.Grains;
using Shared.Models;

namespace Kanelson.Services;

public class UserService : IUserService
{
    private readonly IGrainFactory _grainFactory;
    public UserService(IHttpContextAccessor httpContextAccessor, IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
        CurrentUser = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }

    public string CurrentUser { get; }
    
    public async Task Upsert(string id, string name)
    {
        var grain = _grainFactory.GetGrain<IUserManagerGrain>(0);
        await grain.Upsert(id, name);
    }

    public async Task<ImmutableArray<UserInfo>> GetUserInfo(params string[] ids)
    {
        var grain = _grainFactory.GetGrain<IUserManagerGrain>(0);
        return await grain.GetUserInfo(ids);
    }
}

public interface IUserService
{
    string CurrentUser { get; }
    
    public Task Upsert(string id, string name);

    public Task<ImmutableArray<UserInfo>> GetUserInfo(params string[] ids);
}