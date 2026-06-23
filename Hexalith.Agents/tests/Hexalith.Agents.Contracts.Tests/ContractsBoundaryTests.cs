namespace Hexalith.Agents.Contracts.Tests;

using System.Linq;
using System.Reflection;

using Hexalith.Agents.Contracts;

using Shouldly;

/// <summary>
/// Architecture boundary guard (AC2 / AC3). The public contracts assembly must not reference server
/// infrastructure, provider SDKs, EventStore <em>server</em> internals, the Dapr <em>runtime</em>, or the
/// UI shell. For the empty shell this passes trivially and stands as a guard that grows with the contracts.
/// </summary>
public sealed class ContractsBoundaryTests
{
    private static readonly string[] _forbiddenAssemblyPrefixes =
    [
        "Microsoft.AspNetCore",             // server / web infrastructure
        "Dapr",                             // Dapr runtime implementation
        "Hexalith.EventStore.Server",       // EventStore server internals
        "Hexalith.EventStore.DomainService",
        "Hexalith.FrontComposer.Shell",     // UI shell
        "Microsoft.SemanticKernel",         // provider / agent-runtime SDK
        "Microsoft.Agents",                 // Microsoft.Agents.AI / .Workflows
        "Microsoft.Extensions.AI",          // AI runtime
        "Azure.AI",                         // provider SDK
        "OpenAI",                           // provider SDK
        "Anthropic",                        // provider SDK
        "ModelContextProtocol",             // tool-host runtime
    ];

    [Fact]
    public void ContractsAssemblyShouldNotReferenceForbiddenAssemblies()
    {
        Assembly contracts = typeof(AgentsContractsAssemblyMarker).Assembly;

        foreach (string referenced in contracts.GetReferencedAssemblies().Select(name => name.Name ?? string.Empty))
        {
            bool isForbidden = _forbiddenAssemblyPrefixes.Any(prefix =>
                referenced.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            isForbidden.ShouldBeFalse(
                $"Hexalith.Agents.Contracts must not reference '{referenced}' — forbidden boundary dependency (AC2).");
        }
    }
}
