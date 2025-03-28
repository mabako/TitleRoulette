using System;

namespace TitleRoulette;

public sealed class Title
{
    public required ushort Id { get; init; }
    public required string MasculineName { get; init; }
    public required string FeminineName { get; init; }
    public required bool IsPrefix { get; init; }

    public bool Matches(string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
            return true;

        return MasculineName.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
               FeminineName.Contains(searchText, StringComparison.CurrentCultureIgnoreCase);
    }
}
