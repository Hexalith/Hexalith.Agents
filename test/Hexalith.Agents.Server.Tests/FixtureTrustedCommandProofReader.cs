using System.Security.Claims;
using Hexalith.Agents.EventStore;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Unit-only already-admitted proof fixture; never used by production or a qualification command.</summary>
internal sealed class FixtureTrustedCommandProofReader : IAgentsTrustedCommandProofReader
{
    /// <inheritdoc />
    public bool HasAdmittedProof(ClaimsPrincipal principal, SubmitCommandRequest command, string key, string value) => true;
}
