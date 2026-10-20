using System.Reflection;
using System.Text.Json;
using Dapr.Workflow;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Server.Application.Workflows;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>AD-27 reflection sweep and poison-input rejection cover every workflow and activity contract.</summary>
public sealed class WorkflowExecutionStateBoundaryTests
{
    [Fact]
    public void EveryWorkflowAndActivityContractContainsOnlyIdsRevisionsDigestsSafeEnumsAndProtectedReferences()
    {
        var contracts = typeof(AutomaticInteractionWorkflow).Assembly.GetTypes().Where(t => t.BaseType?.IsGenericType == true)
            .Select(t => t.BaseType!).Where(b => b.GetGenericTypeDefinition() == typeof(Workflow<,>) || b.GetGenericTypeDefinition() == typeof(WorkflowActivity<,>))
            .SelectMany(b => b.GetGenericArguments()).Distinct().ToArray();
        contracts.ShouldBe([typeof(WorkflowExecutionState)]);
        foreach (Type contract in contracts.Append(typeof(ProtectedContentReference)))
        {
            foreach (PropertyInfo property in contract.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Type value = property.PropertyType;
                bool allowed = value.IsEnum || value == typeof(ProtectedContentReference) || value == typeof(long) && property.Name.EndsWith("Revision", StringComparison.Ordinal)
                    || value == typeof(string) && (property.Name.EndsWith("Id", StringComparison.Ordinal) || property.Name.EndsWith("Digest", StringComparison.Ordinal));
                allowed.ShouldBeTrue($"AD-27 forbidden contract member {contract.Name}.{property.Name}");
            }
        }
    }
    [Theory]
    [InlineData("a plaintext prompt goes here")]
    [InlineData("tenant\npoison")]
    [InlineData("")]
    public void NonIdentityTextIsRejectedBeforeScheduling(string poison)
    {
        var fixture = new AutomaticInteractionReplayFixture();
        Should.Throw<ArgumentException>(() => WorkflowExecutionStateValidation.Validate(fixture.Input with { TenantId = poison }));
        fixture.Scheduled.ShouldBeEmpty();
    }
    [Fact]
    public void UnknownContentMembersAndCrossTenantReferencesCannotBeSilentlyIgnored()
    {
        var fixture = new AutomaticInteractionReplayFixture();
        string json = JsonSerializer.Serialize(fixture.Input); json = json[..^1] + ",\"Prompt\":\"poison\"}";
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<WorkflowExecutionState>(json));
        Should.Throw<ArgumentException>(() => WorkflowExecutionStateValidation.Validate(fixture.Input with { RequestReference = fixture.Input.RequestReference with { TenantId = "tenant-b" } }));
        Should.Throw<ArgumentException>(() => WorkflowExecutionStateValidation.Validate(fixture.Input with { Status = (WorkflowExecutionStatus)999 }));
    }
}
