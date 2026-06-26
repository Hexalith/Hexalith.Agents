namespace Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;

/// <summary>
/// Provider administration authorization failed closed (AC3). Emitted before any mutation when the caller is
/// not an authorized provider administrator. Deliberately reveals nothing about whether unrelated provider/model
/// entries exist — it carries only the catalog identity, the actor, and the attempted command name.
/// </summary>
/// <param name="CatalogId">The provider-catalog aggregate identifier the command targeted.</param>
/// <param name="ActorUserId">The unauthorized actor.</param>
/// <param name="CommandName">The attempted command.</param>
public record ProviderCatalogAdministrationDeniedRejection(
    string CatalogId,
    string ActorUserId,
    string CommandName) : IRejectionEvent;
