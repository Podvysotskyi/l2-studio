namespace L2.Studio.Repositories.Interfaces.Models;

public sealed class ContentDeleteConflictException(string dependentType, int dependentCount)
    : InvalidOperationException($"Cannot delete this record because it is used by {dependentCount} {dependentType}.")
{
    public string DependentType { get; } = dependentType;
    public int DependentCount { get; } = dependentCount;
}
