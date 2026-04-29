using System.Globalization;
using System.Resources;

namespace Kanelson.Common;

public static class ValidationMessages
{
    private static readonly ResourceManager _resourceManager =
        new("Kanelson.Common.ValidationMessages", typeof(ValidationMessages).Assembly);

    public static string ValidationRequired =>
        _resourceManager.GetString(nameof(ValidationRequired), CultureInfo.CurrentUICulture)!;

    public static string ValidationStringLength =>
        _resourceManager.GetString(nameof(ValidationStringLength), CultureInfo.CurrentUICulture)!;

    public static string ValidationRange =>
        _resourceManager.GetString(nameof(ValidationRange), CultureInfo.CurrentUICulture)!;

    public static string ValidationUrl =>
        _resourceManager.GetString(nameof(ValidationUrl), CultureInfo.CurrentUICulture)!;
}
