namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// The fail-closed classification of a selected-generated-version read for posting (Story 2.5; AC2; AD-12, AD-14). Only
/// <see cref="Available"/> permits posting; <see cref="NotAvailable"/> fails closed.
/// </summary>
public enum AgentGeneratedVersionReadOutcome
{
    /// <summary>Not-an-outcome sentinel — treated as not-available (fail closed).</summary>
    Unknown = 0,

    /// <summary>The selected generated version (its id + content) was read and is available to post.</summary>
    Available,

    /// <summary>The selected generated version could not be read (missing, not-found, or read failure) — fail closed.</summary>
    NotAvailable,
}

/// <summary>
/// Server-internal result of reading the selected generated version to post (Story 2.5; AC2; AD-12, AD-14). It carries
/// the fail-closed <see cref="Outcome"/>, the selected <see cref="VersionId"/>, and the version's
/// <see cref="GeneratedContent"/> (present only on an <see cref="AgentGeneratedVersionReadOutcome.Available"/> read).
/// </summary>
/// <remarks>
/// <para>
/// This is not a public contract — it is a Server-only port type. <b>Sensitive content (AD-14):</b>
/// <see cref="GeneratedContent"/> is conversation-derived content of the same class as the caller prompt. It is read ONLY
/// here, handed straight to the message poster, and NEVER placed on the posting command/result/event/state-posting-fields
/// or logs. It is non-null only on an available read; for a not-available read it is <see langword="null"/>.
/// </para>
/// </remarks>
/// <param name="Outcome">The fail-closed read classification (only <see cref="AgentGeneratedVersionReadOutcome.Available"/> permits posting).</param>
/// <param name="VersionId">The selected generated version identifier (a safe id; empty when not available).</param>
/// <param name="GeneratedContent">The selected version's generated content — sensitive; stays server-side (non-null only on an available read), or <see langword="null"/> (AD-14).</param>
public sealed record AgentGeneratedVersionReadResult(
    AgentGeneratedVersionReadOutcome Outcome,
    string VersionId,
    string? GeneratedContent)
{
    /// <summary>Gets the fail-closed not-available result (the deferred default) — no version, no content.</summary>
    public static AgentGeneratedVersionReadResult NotAvailable { get; } = new(AgentGeneratedVersionReadOutcome.NotAvailable, string.Empty, null);
}
