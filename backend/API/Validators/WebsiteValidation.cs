namespace API.Validators;

internal static class WebsiteValidation
{
    internal static bool IsValid(string? website)
    {
        return Uri.TryCreate(website, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
