namespace CorpLinkBaseMinimal.Auth;

internal static class ReturnUrlHelper
{
    /// <summary>
    /// Разрешаем только относительные пути внутри приложения (без open redirect).
    /// </summary>
    public static string Sanitize(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return "/";

        if (!Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
            return "/";

        if (returnUrl.StartsWith("//", StringComparison.Ordinal))
            return "/";

        if (returnUrl.Contains(":\\", StringComparison.Ordinal) || returnUrl.Contains("://", StringComparison.Ordinal))
            return "/";

        return returnUrl.StartsWith("/", StringComparison.Ordinal) ? returnUrl : "/" + returnUrl;
    }
}