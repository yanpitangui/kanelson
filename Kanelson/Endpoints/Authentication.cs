using Kanelson.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Kanelson.Endpoints;

public static class Authentication
{
    public static void MapAuthentication(this WebApplication app)
    {

        app.MapPost("~/signin", async ([FromForm] string provider, [FromForm] string redirectUri, HttpContext context) =>
        {
            // Note: the "provider" parameter corresponds to the external
            // authentication provider choosen by the user agent.
            if (string.IsNullOrWhiteSpace(provider))
            {
                return Results.BadRequest();
            }

            
            if (!await context.IsProviderSupportedAsync(provider))
            {
                return Results.BadRequest();
            }

            // Instruct the middleware corresponding to the requested external identity
            // provider to redirect the user agent to its own authorization endpoint.
            // Note: the authenticationScheme parameter must match the value configured in Startup.cs
            return Results.Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, new [] { provider });
        }).DisableAntiforgery();
        
        app.MapMethods("~/signout", new[] { "POST", "GET" },
            () =>
            {
                return Results.SignOut(new AuthenticationProperties { RedirectUri = "/" },
                   new [] { CookieAuthenticationDefaults.AuthenticationScheme });
            });
    }
}