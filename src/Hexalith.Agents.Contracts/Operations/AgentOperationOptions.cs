using System.Collections.Generic;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>
/// Client-supplied operation metadata allowed on public Agents automation calls.
/// </summary>
/// <remarks>
/// Tenant identity, authenticated user/party context, tokens, claims, and trusted policy verdicts are server-controlled
/// and must not be accepted from this metadata. Client input is limited to correlation, idempotency, and non-authoritative
/// options that server orchestrators may sanitize or ignore.
/// </remarks>
/// <param name="CorrelationId">Optional caller correlation identifier.</param>
/// <param name="IdempotencyKey">Optional caller idempotency key.</param>
/// <param name="Options">Optional non-authoritative operation options.</param>
public sealed record AgentOperationOptions(
    string? CorrelationId = null,
    string? IdempotencyKey = null,
    IReadOnlyDictionary<string, string>? Options = null);
