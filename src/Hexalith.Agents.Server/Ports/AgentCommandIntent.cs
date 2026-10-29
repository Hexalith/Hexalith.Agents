namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Immutable input to host admission. It carries the logical command identity and exact bytes without constructing
/// an EventStore target command or resolving its aggregate identity before signing, replay admission and denial capture.
/// </summary>
public sealed class AgentCommandIntent
{
    private readonly byte[] _payload;
    private readonly Dictionary<string, string>? _extensions;

    /// <summary>Initializes an immutable command intent for host admission.</summary>
    /// <param name="MessageId">The logical command identity, preserved across independent delivery nonces.</param>
    /// <param name="TenantId">The authenticated target tenant to verify before command construction.</param>
    /// <param name="Domain">The target domain to verify.</param>
    /// <param name="AggregateId">The target aggregate to verify.</param>
    /// <param name="CommandType">The operation-specific command type to verify.</param>
    /// <param name="Payload">The exact serialized command bytes to authenticate.</param>
    /// <param name="CorrelationId">The correlation identity to authenticate.</param>
    /// <param name="CausationId">The optional causation identity to authenticate.</param>
    /// <param name="UserId">The stable actor or machine identity to authenticate.</param>
    /// <param name="Extensions">Sanitized metadata and server-derived assertions awaiting host verification.</param>
    public AgentCommandIntent(string MessageId, string TenantId, string Domain, string AggregateId, string CommandType,
        byte[] Payload, string CorrelationId, string? CausationId, string UserId, Dictionary<string, string>? Extensions)
    {
        ArgumentNullException.ThrowIfNull(Payload);
        this.MessageId = MessageId;
        this.TenantId = TenantId;
        this.Domain = Domain;
        this.AggregateId = AggregateId;
        this.CommandType = CommandType;
        this.CorrelationId = CorrelationId;
        this.CausationId = CausationId;
        this.UserId = UserId;
        _payload = Payload.ToArray();
        _extensions = Extensions is null ? null : new(Extensions, StringComparer.Ordinal);
    }

    /// <summary>Gets the immutable logical identity; the host's delivery nonce is separate trust metadata.</summary>
    public string MessageId { get; }

    /// <summary>Gets the exact target tenant awaiting host verification.</summary>
    public string TenantId { get; }

    /// <summary>Gets the exact target domain awaiting host verification.</summary>
    public string Domain { get; }

    /// <summary>Gets the exact target aggregate awaiting host verification.</summary>
    public string AggregateId { get; }

    /// <summary>Gets the exact operation awaiting host verification.</summary>
    public string CommandType { get; }

    /// <summary>Gets a copy of the exact bytes so callers cannot change an intent while admission is awaiting.</summary>
    public byte[] Payload => _payload.ToArray();

    /// <summary>Gets the correlation identity awaiting host verification.</summary>
    public string CorrelationId { get; }

    /// <summary>Gets the optional causation identity awaiting host verification.</summary>
    public string? CausationId { get; }

    /// <summary>Gets the stable actor or machine identity awaiting host verification.</summary>
    public string UserId { get; }

    /// <summary>Gets a copy of metadata so it cannot change between canonical verification and target construction.</summary>
    public Dictionary<string, string>? Extensions => _extensions is null ? null : new(_extensions, StringComparer.Ordinal);

    /// <summary>Gets the transient current-authority lifetime; admission must preserve this exact bound.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public AgentAuthorityValidity? Authority { get; init; }

    /// <summary>Returns only a type label, withholding payload, metadata and unadmitted target identity.</summary>
    /// <returns>The safe intent label.</returns>
    public override string ToString() => nameof(AgentCommandIntent);
}
