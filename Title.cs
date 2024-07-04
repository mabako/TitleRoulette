namespace TitleRoulette;

public sealed class Title
{
    public required ushort Id { get; init; }
    public required string MasculineName { get; init; }
    public required string FeminineName { get; init; }
    public required bool IsPrefix { get; init; }
}
