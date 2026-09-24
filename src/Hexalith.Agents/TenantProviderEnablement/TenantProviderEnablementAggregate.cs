using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;
using Hexalith.Agents.ProviderCatalog;

using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Attributes;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.TenantProviderEnablement;

/// <summary>Pure EventStore aggregate for tenant provider visibility and terms decisions.</summary>
[EventStoreDomain(Domain)]
public sealed class TenantProviderEnablementAggregate : EventStoreAggregate<TenantProviderEnablementState>
{
    /// <summary>The EventStore domain.</summary>
    public const string Domain = "tenant-provider-enablement";

    /// <summary>Trusted server extension proving platform authority.</summary>
    public const string PlatformOperatorExtensionKey = "actor:platformOperator";

    /// <summary>Trusted server extension proving tenant administrator authority.</summary>
    public const string TenantAdministratorExtensionKey = "actor:tenantAgentAdministrator";

    /// <summary>Decides a platform enablement change for one tenant.</summary>
    /// <param name="command">The platform enablement command.</param>
    /// <param name="state">The replayed tenant state.</param>
    /// <param name="envelope">The authorized command envelope.</param>
    /// <returns>The event, rejection, or deterministic no-op.</returns>
    public static DomainResult Handle(SetTenantProviderModelEnablement command, TenantProviderEnablementState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        if (!ValidScope(envelope) || !MatchesStateTenant(state, envelope)
            || !string.Equals(command.TenantId, envelope.TenantId, StringComparison.Ordinal)
            || !HasAuthority(envelope, PlatformOperatorExtensionKey))
        {
            return Reject(envelope, command.ProviderId, command.ModelId, "NotAuthorized");
        }

        if (string.IsNullOrWhiteSpace(command.ProviderId) || string.IsNullOrWhiteSpace(command.ModelId)
            || command.Enabled && command.CurrentTerms is not { DataHandlingVersion: >= 1 }
            || command.CurrentTerms is not null && ProviderDataHandlingPolicy.Validate(
                command.CurrentTerms, current: null, allowHistoricalVersion: true) is not null)
        {
            return Reject(envelope, command.ProviderId, command.ModelId, "InvalidCurrentTerms");
        }

        string key = ProviderCatalogState.EntryKey(command.ProviderId, command.ModelId);
        int revision = state?.Revision ?? 0;
        if (command.ExpectedRevision != revision)
        {
            return Reject(envelope, command.ProviderId, command.ModelId, "StaleRevision");
        }

        if (state?.Entries.TryGetValue(key, out TenantProviderEntryState? existing) == true
            && existing.Enabled == command.Enabled
            && (command.MigratedFrom is null || string.Equals(existing.MigratedFrom, command.MigratedFrom, StringComparison.Ordinal)))
        {
            return ProviderGovernanceDomainResult.AlreadyApplied();
        }

        return ProviderGovernanceDomainResult.Applied([new TenantProviderModelEnablementSet(
            envelope.TenantId,
            command.ProviderId,
            command.ModelId,
            command.Enabled,
            revision + 1,
            envelope.UserId,
            command.MigratedFrom)]);
    }

    /// <summary>Decides an administrator's acceptance or decline of current terms.</summary>
    /// <param name="command">The terms decision.</param>
    /// <param name="state">The replayed tenant state.</param>
    /// <param name="envelope">The authorized command envelope.</param>
    /// <returns>The event, rejection, or deterministic no-op.</returns>
    public static DomainResult Handle(DecideProviderDataHandling command, TenantProviderEnablementState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        if (!ValidScope(envelope) || !MatchesStateTenant(state, envelope)
            || !HasAuthority(envelope, TenantAdministratorExtensionKey))
        {
            return Reject(envelope, command.ProviderId, command.ModelId, "NotAuthorized");
        }

        if (string.IsNullOrWhiteSpace(command.ProviderId) || string.IsNullOrWhiteSpace(command.ModelId)
            || string.IsNullOrWhiteSpace(command.Justification)
            || command.ConfirmedTerms is null
            || command.ConfirmedTerms.DataHandlingVersion != command.DataHandlingVersion
            || command.DecidedAt == default)
        {
            return Reject(envelope, command.ProviderId, command.ModelId, "InvalidDecision");
        }

        string key = ProviderCatalogState.EntryKey(command.ProviderId, command.ModelId);
        if (state is null || !state.Entries.TryGetValue(key, out TenantProviderEntryState? entry) || entry is not { Enabled: true })
        {
            return Reject(envelope, command.ProviderId, command.ModelId, "EntryNotEnabled");
        }

        if (command.ExpectedRevision != state.Revision)
        {
            return Reject(envelope, command.ProviderId, command.ModelId, "StaleRevision");
        }

        ProviderDataHandlingDecided? prior = entry.LastDecision;
        if (prior is not null && prior.ConfirmedTerms.DataHandlingVersion == command.DataHandlingVersion
            && prior.Accepted == command.Accepted && prior.ActorUserId == envelope.UserId)
        {
            return prior.Justification == command.Justification
                && ProviderDataHandlingPolicy.SameSnapshot(prior.ConfirmedTerms, command.ConfirmedTerms)
                ? ProviderGovernanceDomainResult.AlreadyApplied()
                : Reject(envelope, command.ProviderId, command.ModelId, "DivergentDuplicate");
        }

        if (prior is not null && command.DataHandlingVersion < prior.ConfirmedTerms.DataHandlingVersion)
        {
            return Reject(envelope, command.ProviderId, command.ModelId, "StaleRevision");
        }

        return ProviderGovernanceDomainResult.Applied([new ProviderDataHandlingDecided(
            envelope.TenantId,
            command.ProviderId,
            command.ModelId,
            command.Accepted,
            command.Justification,
            state.Revision + 1,
            envelope.UserId,
            "TenantAgentAdministrator",
            command.ConfirmedTerms,
            command.DecidedAt)]);
    }

    private static bool ValidScope(CommandEnvelope envelope)
        => !string.Equals(envelope.TenantId, ProviderCatalogIdentity.PlatformTenantId, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(envelope.TenantId)
            && string.Equals(envelope.AggregateId, envelope.TenantId, StringComparison.Ordinal);

    private static bool MatchesStateTenant(TenantProviderEnablementState? state, CommandEnvelope envelope)
        => state is null || string.IsNullOrWhiteSpace(state.TenantId)
            || string.Equals(state.TenantId, envelope.TenantId, StringComparison.Ordinal);

    private static bool HasAuthority(CommandEnvelope envelope, string key)
        => envelope.Extensions?.TryGetValue(key, out string? value) == true
            && string.Equals(value, "true", StringComparison.Ordinal);

    private static DomainResult Reject(CommandEnvelope envelope, string providerId, string modelId, string reason)
        => DomainResult.Rejection([new TenantProviderGovernanceRejected(envelope.TenantId, providerId, modelId, reason)]);
}
