namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>Platform Operator's durable declaration of a specific field-level tightening.</summary>
/// <param name="FromVersion">The preceding terms version.</param>
/// <param name="ToVersion">The tightened terms version.</param>
/// <param name="FieldDiff">The exact governed field differences.</param>
/// <param name="ActorUserId">The operator actor.</param>
/// <param name="RoleBasis">The operator's authority basis.</param>
/// <param name="DeclaredAt">The trusted effective time.</param>
public sealed record ProviderDataHandlingTighteningDeclaration(
    int FromVersion,
    int ToVersion,
    ProviderDataHandlingFieldDiff FieldDiff,
    string ActorUserId,
    string RoleBasis,
    DateTimeOffset DeclaredAt);
