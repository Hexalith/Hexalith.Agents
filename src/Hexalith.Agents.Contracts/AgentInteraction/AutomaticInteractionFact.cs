using System.Text.Json.Serialization;

using Hexalith.EventStore.Contracts.Security;
namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>One immutable server-trusted phase result. Only an allowed generated version may carry protected content.</summary>
/// <param name="Step">The Step binding.</param>
/// <param name="OperationId">The OperationId binding.</param>
/// <param name="Outcome">The Outcome binding.</param>
/// <param name="Revision">The Revision binding.</param>
/// <param name="LeaseId">The LeaseId binding.</param>
/// <param name="LeaseCommitRevision">The LeaseCommitRevision binding.</param>
/// <param name="DependencyRecordId">The DependencyRecordId binding.</param>
/// <param name="OutputAuthority">The OutputAuthority binding.</param>
/// <param name="Output">The Output binding.</param>
/// <param name="ContentDigest">The ContentDigest binding.</param>
/// <param name="DigestKeyVersion">The DigestKeyVersion binding.</param>
/// <param name="FailureKind">The safe failure classification; output safety uses the recorded authority mapping.</param>
/// <param name="DeletionRequestId">The exact owner Closing request for a Provider or posting suppression.</param>
/// <param name="DirectoryClosingRevision">The immutable authoritative Closing revision for a Provider or posting suppression.</param>
public sealed record AutomaticInteractionFact(AutomaticInteractionStep Step, string OperationId, AutomaticInteractionOutcome Outcome, long Revision, string? LeaseId = null, long LeaseCommitRevision = 0, string? DependencyRecordId = null, ProviderOutputAuthority? OutputAuthority = null, ProtectedContent? Output = null, string? ContentDigest = null, string? DigestKeyVersion = null, AutomaticInteractionFailureKind FailureKind = AutomaticInteractionFailureKind.Unknown, string? DeletionRequestId = null, long DirectoryClosingRevision = 0)
{
    /// <summary>Gets the safe reason when an authority read refused Provider invocation.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProviderOutputAuthorityRefusal? OutputAuthorityRefusal { get; init; }

    /// <summary>Gets the exact allocator admission that the no-invocation record verifies.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CapacityAdmissionId { get; init; }
}
