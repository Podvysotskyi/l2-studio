namespace L2.Studio.Configurations;

public sealed class DependencyOptions
{
    public const string SectionName = "Dependencies";

    public bool PostgreSqlRequired { get; init; } = true;
}
