using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Kanelson.Extensions;

public static class HubCallerContextExtensions
{
    public static string GetUserId(this HubCallerContext context) =>
        context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!;
}