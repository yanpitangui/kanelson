using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Kanelson.Endpoints;

public static class Culture
{
    public static void MapCulture(this WebApplication app)
    {
        app.MapGroup("culture")
            .MapGet("set", ([FromQuery] string? culture, [FromQuery] string? redirectUri, HttpContext context) =>
            {
                if (culture != null)
                {
                    context.Response.Cookies.Append(
                        CookieRequestCultureProvider.DefaultCookieName,
                        CookieRequestCultureProvider.MakeCookieValue(
                            new RequestCulture(culture, culture)));
                }

                return Results.LocalRedirect(redirectUri ?? string.Empty);
            });
    }
}