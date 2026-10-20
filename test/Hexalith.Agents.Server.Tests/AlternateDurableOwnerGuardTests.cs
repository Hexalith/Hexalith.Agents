using Dapr.Workflow;
using Hexalith.Agents.Server.Application.Workflows;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Every compiled Server type is swept, including internal types that could hide a second owner.</summary>
public sealed class AlternateDurableOwnerGuardTests
{
    [Fact]
    public void ThereIsExactlyOneWorkflowAndNoAgentsHostedService()
    {
        Type[] types = typeof(AutomaticInteractionWorkflow).Assembly.GetTypes();
        types.Where(t => t.BaseType?.IsGenericType == true && t.BaseType.GetGenericTypeDefinition() == typeof(Workflow<,>)).Select(t => t.FullName)
            .ShouldBe([typeof(AutomaticInteractionWorkflow).FullName]);
        types.Where(t => typeof(IHostedService).IsAssignableFrom(t)).ShouldBeEmpty();
        types.Where(t => t.Namespace != null && (t.Namespace.Contains(".Tools", StringComparison.Ordinal) || t.Namespace.StartsWith("Microsoft.Agents", StringComparison.Ordinal))).ShouldBeEmpty();
    }
}
