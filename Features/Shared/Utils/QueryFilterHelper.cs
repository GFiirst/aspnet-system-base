public static class QueryFilterHelper
{
    public const int MaxTextLength = 100;
    public const string LikeEscapeCharacter = "\\";

    public static string? CreateContainsPattern(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return null;

        var term = search.Trim();
        if (term.Length > MaxTextLength)
            term = term[..MaxTextLength];

        var escapedTerm = term
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");

        return $"%{escapedTerm}%";
    }

    public static bool UnaccentedILike(
        string matchExpression,
        string pattern,
        string escapeCharacter) =>
        throw new InvalidOperationException(
            "Esta função deve ser traduzida para SQL pelo Entity Framework.");

    public static void NormalizePagination(PagedSearchRequestDto filter)
    {
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = Math.Clamp(filter.PageSize, 1, 100);
    }
}
