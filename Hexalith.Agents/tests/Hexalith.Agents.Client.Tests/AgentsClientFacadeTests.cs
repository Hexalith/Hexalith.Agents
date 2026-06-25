namespace Hexalith.Agents.Client.Tests;

using System.Reflection;

using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.Operations;

using Shouldly;

/// <summary>
/// Public client facade tests for Story 4.1.
/// </summary>
public sealed class AgentsClientFacadeTests
{
    private static readonly string[] _forbiddenTypeNameParts =
    [
        "CommandEnvelope",
        "EventStore",
        "Aggregate",
        "Stream",
        "Projection",
        "Dapr",
        "IServiceProvider",
        "SemanticKernel",
        "Microsoft.Agents",
        "ModelContextProtocol",
    ];

    [Fact]
    public async Task Unavailable_client_returns_structured_unavailable_results()
    {
        IAgentsClient client = AgentsClient.Unavailable();

        AgentOperationResult<AgentReadinessStatus> result =
            await client.Status.GetAgentReadinessAsync("agent-1").ConfigureAwait(true);

        result.Status.ShouldBe(AgentOperationStatus.Unavailable);
        result.Error.ShouldNotBeNull();
        result.Error.Code.ShouldBe(AgentOperationErrorCode.Unavailable);
        result.Error.Message.ShouldBe("The operation is currently unavailable.");
    }

    [Fact]
    public async Task Unavailable_client_validates_public_string_arguments()
    {
        IAgentsClient client = AgentsClient.Unavailable();

        await Should.ThrowAsync<ArgumentException>(async () =>
            await client.Status.GetAgentReadinessAsync(" ").ConfigureAwait(true));
    }

    [Fact]
    public async Task Unavailable_client_returns_structured_unavailable_for_each_operation_group()
    {
        IAgentsClient client = AgentsClient.Unavailable();

        AssertUnavailable(await client.ProviderCatalog.ListEntriesAsync(includeDisabled: true).ConfigureAwait(true));
        AssertUnavailable(await client.AgentAdministration.CreateAsync(new CreateAgent(
            "tenant-1",
            "Hexa",
            "Assistant",
            "Keep replies concise.")).ConfigureAwait(true));
        AssertUnavailable(await client.AgentInteractions.RequestAsync(new RequestAgentInteraction(
            "agent-1",
            "conversation-1",
            "party-1",
            "Summarize decisions.",
            "idem-1",
            Snapshot: null,
            "corr-1")).ConfigureAwait(true));
        AssertUnavailable(await client.ProposalWorkflow.GetDetailAsync("interaction-1").ConfigureAwait(true));
        AssertUnavailable(await client.Status.GetProviderModelReadinessAsync("provider-1", "model-1").ConfigureAwait(true));
        AssertUnavailable(await client.Audit.GetGenerationEvidenceAsync("interaction-1").ConfigureAwait(true));
    }

    [Fact]
    public async Task Unavailable_client_validates_null_commands()
    {
        IAgentsClient client = AgentsClient.Unavailable();

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await client.AgentAdministration.CreateAsync(null!).ConfigureAwait(true));
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await client.AgentInteractions.RequestAsync(null!).ConfigureAwait(true));
    }

    [Fact]
    public void Client_surface_exposes_operation_groups()
    {
        IAgentsClient client = AgentsClient.Unavailable();

        client.ProviderCatalog.ShouldNotBeNull();
        client.AgentAdministration.ShouldNotBeNull();
        client.AgentInteractions.ShouldNotBeNull();
        client.ProposalWorkflow.ShouldNotBeNull();
        client.Status.ShouldNotBeNull();
        client.Audit.ShouldNotBeNull();
    }

    [Fact]
    public void Client_public_methods_do_not_expose_internal_substrate_types()
    {
        Type[] publicTypes = typeof(IAgentsClient).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "Hexalith.Agents.Client")
            .ToArray();

        foreach (Type type in publicTypes)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                AssertTypeIsPublicContract(method.ReturnType, $"{type.Name}.{method.Name} return type");

                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    AssertTypeIsPublicContract(parameter.ParameterType, $"{type.Name}.{method.Name} parameter {parameter.Name}");
                }
            }
        }
    }

    private static void AssertTypeIsPublicContract(Type type, string context)
    {
        Type inspected = Nullable.GetUnderlyingType(type) ?? type;

        if (inspected.IsGenericType)
        {
            inspected = inspected.GetGenericTypeDefinition();
        }

        string displayName = inspected.FullName ?? inspected.Name;

        foreach (string forbidden in _forbiddenTypeNameParts)
        {
            displayName.ShouldNotContain(forbidden, Case.Sensitive, $"{context} must not expose {forbidden}.");
        }
    }

    private static void AssertUnavailable(AgentOperationResult result)
    {
        result.Status.ShouldBe(AgentOperationStatus.Unavailable);
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Code.ShouldBe(AgentOperationErrorCode.Unavailable);
    }

    private static void AssertUnavailable<T>(AgentOperationResult<T> result)
    {
        result.Status.ShouldBe(AgentOperationStatus.Unavailable);
        result.IsSuccess.ShouldBeFalse();
        result.Value.ShouldBe(default);
        result.Error.ShouldNotBeNull();
        result.Error.Code.ShouldBe(AgentOperationErrorCode.Unavailable);
    }
}
