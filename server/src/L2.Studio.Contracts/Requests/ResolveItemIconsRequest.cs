using L2.Studio.Contracts;

namespace L2.Studio.Contracts.Requests;

public sealed record ResolveItemIconsRequest(IReadOnlyList<ItemIconReference>? Items);
