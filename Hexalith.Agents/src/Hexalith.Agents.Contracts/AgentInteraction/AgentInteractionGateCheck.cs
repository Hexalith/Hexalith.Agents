using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The specific dependency an invocation gate verdict classifies, evaluated before any provider invocation (AC1;
/// FR-20, FR-21; AD-12). These nine values are the AC1 gate inventory in evaluation order — every one traces to the
/// AD-12 sentence ("tenant access … caller Party state … Source Conversation access … Agent lifecycle … Agent Party
/// identity … Provider/model readiness … response policy … Content Safety Policy … dependency freshness"). Like
/// <see cref="Agent.AgentActivationBlocker"/>, the enum is <em>additively extensible</em>: later gates append new
/// values without reshaping any event.
/// </summary>
/// <remarks>
/// A check is safe by construction — it classifies <em>which</em> dependency was evaluated and never carries raw
/// claims, tokens, Party personal data, provider payloads, or content (AD-14). Serialized by name so an absent value
/// never deserializes to a concrete check. <see cref="Unknown"/> (ordinal 0) is the unrecognized sentinel.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentInteractionGateCheck
{
    /// <summary>Absent/unrecognized check sentinel.</summary>
    Unknown = 0,

    /// <summary>The caller principal has the required tenant access (authorization-class; Agents' local Tenants projection — AD-12).</summary>
    TenantAccess,

    /// <summary>The caller's Party exists and is active (authorization-class; Parties directory — AD-7).</summary>
    CallerPartyState,

    /// <summary>The caller participates in the Source Conversation with sufficient role and it loaded fresh (authorization-class; Conversations authorized read — AD-6).</summary>
    SourceConversationAccess,

    /// <summary>The target Agent is in an Active lifecycle state (readiness-class).</summary>
    AgentLifecycle,

    /// <summary>The Agent's linked Party identity is present and valid (readiness-class; AD-7).</summary>
    AgentPartyIdentity,

    /// <summary>The selected Provider/model catalog entry is enabled and ready (readiness-class; AD-9).</summary>
    ProviderModelReadiness,

    /// <summary>The Response Mode is set and, in Confirmation mode, the approver policy is resolvable (readiness-class; AD-8).</summary>
    ResponsePolicy,

    /// <summary>An active Content Safety Policy is configured (readiness-class; FR-12).</summary>
    ContentSafetyPolicy,

    /// <summary>Every consulted projection is within its freshness threshold (readiness-class — fails closed on stale state; AD-12).</summary>
    DependencyFreshness,
}
