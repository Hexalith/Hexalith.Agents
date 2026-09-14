using System.Collections.Generic;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>
/// Client-supplied operation metadata allowed on public Agents automation calls.
/// </summary>
/// <remarks>
/// Tenant identity, authenticated user/party context, tokens, claims, and trusted policy verdicts are server-controlled
/// and must not be accepted from this metadata. Client input is limited to correlation, idempotency, and non-authoritative
/// options that server orchestrators may sanitize or ignore. Supplied identities must use their canonical uppercase
/// 26-character ULID representation. An exact retry must reuse the same correlation ID, idempotency key, and payload.
/// </remarks>
/// <param name="CorrelationId">
/// Optional canonical uppercase 26-character ULID used to correlate the command attempt.
/// </param>
/// <param name="IdempotencyKey">
/// Optional canonical uppercase 26-character ULID. Retries must reuse this value with the exact original payload.
/// </param>
/// <param name="Options">Optional non-authoritative operation options.</param>
public sealed record AgentOperationOptions(
    string? CorrelationId = null,
    string? IdempotencyKey = null,
    IReadOnlyDictionary<string, string>? Options = null);
