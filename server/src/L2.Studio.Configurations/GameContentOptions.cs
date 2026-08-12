namespace L2.Studio.Configurations;

public sealed class GameContentOptions
{
    public const string SectionName = "GameContent";

    public bool RunMigrations { get; init; } = true;
}
