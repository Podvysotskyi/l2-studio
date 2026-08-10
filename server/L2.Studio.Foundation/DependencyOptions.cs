namespace L2.Studio.Foundation;

public sealed class DependencyOptions
{
    public const string SectionName = "Dependencies";

    public bool PostgreSqlRequired { get; init; } = true;
}
