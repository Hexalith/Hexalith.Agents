namespace Hexalith.Agents.Contracts.Agent;

/// <summary>Current safe per-source basis shared by API and UI; contains no Party profile or target identity.</summary>
/// <param name="Kind">Exact configured source kind.</param><param name="Status">Current fail-closed source status.</param>
public sealed record ApproverPolicyBasisEntry(ApproverPolicySourceKind Kind, ApproverPolicyValidationStatus Status);
