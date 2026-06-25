namespace Hexalith.Agents.Contracts.Operations;

/// <summary>
/// Optional paging metadata for list-shaped public Agents operations.
/// </summary>
/// <param name="ContinuationToken">Opaque continuation token, if another page is available.</param>
/// <param name="Count">Number of items represented by the current page.</param>
public sealed record AgentOperationPage(
    string? ContinuationToken,
    int Count);
