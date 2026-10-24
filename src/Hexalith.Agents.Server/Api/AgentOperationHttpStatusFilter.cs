using System.Reflection;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Contracts.Operations;

using Microsoft.AspNetCore.Http;

namespace Hexalith.Agents.Server.Api;

/// <summary>Preserves typed operation bodies while reporting their canonical HTTP outcome.</summary>
public sealed class AgentOperationHttpStatusFilter : IEndpointFilter
{
    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        object? result = await next(context).ConfigureAwait(false);
        if (result is null or IResult)
        {
            return result;
        }

        if (result is ProviderCatalogMigrationResult migration)
        {
            return Results.Json(result, statusCode: MapName(migration.Status));
        }

        PropertyInfo? statusProperty = result.GetType().GetProperty(nameof(AgentOperationResult.Status));
        if (statusProperty?.GetValue(result) is not Enum status)
        {
            return result;
        }

        int code = Map(status);
        if (status is AgentOperationStatus.Succeeded)
        {
            object? value = result.GetType().GetProperty("Value")?.GetValue(result);
            if (PayloadOutcome(value) is { } outcome)
            {
                code = Map(outcome);
            }
        }

        return Results.Json(result, statusCode: code);
    }

    private static Enum? PayloadOutcome(object? value)
        => value switch
        {
            AgentCommandAcceptance acceptance => acceptance.TruthState,
            ProviderCatalogCommandAcceptance acceptance => acceptance.TruthState,
            AgentSetupWriteOutcome outcome => outcome,
            AgentSetupResult inspection => inspection.Status,
            ProviderCatalogInspectionResult inspection => inspection.Status,
            TenantProviderCatalogInspectionResult inspection => inspection.Status,
            TenantProviderEnablementInspectionResult inspection => inspection.Status,
            AgentCallRequestResult request => request.Status,
            AgentInteractionInspectionResult inspection => inspection.Status,
            AgentInteractionGateEvidenceResult inspection => inspection.Status,
            AgentInteractionContextEvidenceResult inspection => inspection.Status,
            AgentProposalApprovalEvidenceResult inspection => inspection.Status,
            AgentProposalEditEvidenceResult inspection => inspection.Status,
            AgentProposalRegenerationEvidenceResult inspection => inspection.Status,
            PendingProposalsResult inspection => inspection.Status,
            ProposalDetailResult inspection => inspection.Status,
            AgentProposalEditResult operation => operation.Outcome,
            AgentProposalRegenerationResult operation => operation.Outcome,
            AgentProposalApprovalResult operation => operation.Outcome,
            AgentProposalRejectionResult operation => operation.Outcome,
            AgentProposalAbandonmentResult operation => operation.Outcome,
            AgentProposalExpiryResult operation => operation.Outcome,
            _ => null, // Resource states and evidence fields are data, not operation outcomes.
        };

    private static int Map(Enum status)
    {
        if (status is AgentSetupWriteStatus)
        {
            return StatusCodes.Status422UnprocessableEntity;
        }

        if (!Enum.IsDefined(status.GetType(), status))
        {
            return StatusCodes.Status500InternalServerError;
        }

        // Rejecting a proposal is a successful requested action, unlike a rejected operation.
        return status is AgentProposalApprovalOutcome.Approved
            ? StatusCodes.Status202Accepted
            : status is AgentProposalRejectionOutcome.Rejected
            ? StatusCodes.Status200OK
            : MapName(status.ToString());
    }

    private static int MapName(string status)
        => status switch
        {
            "Succeeded" or "Success" or "Degraded" or "AlreadyApplied"
                or "Edited" or "Regenerated" or "Approved" or "Posted" or "IdempotentPosted"
                or "Abandoned" or "Expired" or "AlreadyTerminal" or "NoOp" or "ProjectionConfirmed"
                => StatusCodes.Status200OK,
            "Pending" or "Checking" or "Submitted" or "AwaitingProjection" or "Accepted"
                or "IdempotentPostingPending" or "AuthoritativePending" => StatusCodes.Status202Accepted,
            "NotAuthorized" => StatusCodes.Status403Forbidden,
            "ValidationFailed" or "Invalid" or "SelectedVersionInvalid" or "InvalidContext"
                or "InvalidLegacyInventory" => StatusCodes.Status400BadRequest,
            "NotFound" or "AgentNotFound" or "EntryNotFound" or "SelectedVersionMissing" => StatusCodes.Status404NotFound,
            "Conflict" or "Stale" or "ProposalNotPending" or "ExpiryNotReached"
                or "TargetConflict" or "DivergentLegacyMetadata" => StatusCodes.Status409Conflict,
            "Rejected" or "MembershipRejected" or "PostRejected" => StatusCodes.Status422UnprocessableEntity,
            "Blocked" or "Unavailable" or "UnableToVerify" or "AdapterFailure" or "PolicyFailure"
                or "ProviderTimeout" or "ProviderDisabled" or "ProviderUnavailable" or "ContentSafetyBlocked"
                or "PartyIdentityUnavailable" or "MembershipUnavailable" or "ConversationUnavailable"
                or "NoExpiryPolicy" or "DispatchFailed" => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError,
        };
}
