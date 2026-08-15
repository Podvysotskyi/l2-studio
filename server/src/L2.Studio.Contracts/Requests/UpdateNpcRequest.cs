namespace L2.Studio.Contracts.Requests;

public sealed record UpdateNpcRequest(
    string? Name,
    short Level,
    string? NpcTypeName,
    string? NpcRaceName,
    string? NpcSexName);
