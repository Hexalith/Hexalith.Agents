namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>Platform Operator's durable declaration of a specific field-level tightening.</summary>
public sealed record ProviderDataHandlingTighteningDeclaration(
    int FromVersion,
    int ToVersion,
    ProviderDataHandlingFieldDiff FieldDiff,
    string ActorUserId,
    string RoleBasis,
    DateTimeOffset DeclaredAt);
