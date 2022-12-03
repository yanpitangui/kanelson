using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Kanelson;

public static class Helpers
{
    public static HubConnection GetConnection(this IHttpContextAccessor httpContextAccessor,
        NavigationManager navigation)
    {
        httpContextAccessor.HttpContext!.Request.Cookies.TryGetValue(".AspNetCore.Cookies", out var value);
        var container = new CookieContainer();
        container.Add(new Cookie
        {
            Domain = navigation.ToAbsoluteUri(navigation.Uri).Host,
            Name = ".AspNetCore.Cookies",
            Value = value
        
        });
        return new HubConnectionBuilder()
            .WithUrl(navigation.ToAbsoluteUri("roomHub"), options =>
            {
                options.Cookies = container;
            })
            .WithAutomaticReconnect()
            .Build();
    }
}