namespace RequestSigning.Server;

public static class HeaderExtensions
{
    public static string GetHeader(this IHeaderDictionary headers, string headerName)
    {
        if (!headers.TryGetValue(headerName, out var headerValues) || headerValues.Count <= 0)
            return string.Empty;
        
        for (var i = 0; i < headerValues.Count; i++)
        {
            var headerValue = headerValues[i];
            if (!string.IsNullOrWhiteSpace(headerValue))
            {
                return headerValue;
            }
        }

        return string.Empty;
    }
}