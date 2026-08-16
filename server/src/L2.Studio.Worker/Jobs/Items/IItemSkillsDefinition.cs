namespace L2.Studio.Worker;

public interface IItemSkillsDefinition
{
    IReadOnlyList<ItemSkillDefinition> Skills { get; }
}
