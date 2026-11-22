using Dapr.Workflow;
using Dapr.Workflow.Worker;
using Hexalith.Agents.Server.Application.Activities;
using Hexalith.Agents.Server.Application.Workflows;
using Hexalith.Agents.Server.Composition;
using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.DomainService;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>The Dapr Workflow graph requires the exact runtime security record.</summary>
public sealed class OpenOrMismatchedDaprSecurityDecisionBlocksWorkloadTests
{
    [Fact]
    public void UnavailableExactSecurityRecordLeavesTheServiceGraphUntouched()
    {
        var services = new ServiceCollection();
        var error = Should.Throw<NotSupportedException>(() => services.AddAgentsInteractionWorkflow(new CommittedAgentDependencyAvailability()));
        error.Message.ShouldBe("DependencyNotAvailable(OD-DAPR-SECURITY-1)"); services.ShouldBeEmpty();
    }

    [Fact]
    public void DefaultHostHasOnlyTheDeferredSchedulerAndNoWorkflowRuntime()
    {
        var builder = CreateDomainHostBuilder();
        using var provider = builder.Services.BuildServiceProvider();

        provider.GetServices<IInteractionWorkflowScheduler>().ShouldHaveSingleItem().ShouldBeOfType<DeferredInteractionWorkflowScheduler>();
        provider.GetServices<IDaprWorkflowClient>().ShouldBeEmpty();
        provider.GetServices<DaprWorkflowClient>().ShouldBeEmpty();
        provider.GetServices<IWorkflowsFactory>().ShouldBeEmpty();
        provider.GetServices<IHostedService>().ShouldNotContain(service => service.GetType().FullName == WorkflowWorkerType);
    }

    [Fact]
    public async Task AvailableSecurityRecordRegistersOneRuntimeWithTheWorkflowAndActivity()
    {
        var builder = CreateDomainHostBuilder();
        var availability = Substitute.For<IAgentDependencyAvailability>();
        builder.Services.AddAgentsInteractionWorkflow(availability);
        await using var provider = builder.Services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        availability.Received(1).Check("OD-DAPR-SECURITY-1");
        provider.GetServices<IInteractionWorkflowScheduler>().ShouldHaveSingleItem().ShouldBeOfType<DaprInteractionWorkflowScheduler>();
        provider.GetServices<DaprWorkflowClient>().ShouldHaveSingleItem();
        var registrations = Substitute.For<IWorkflowsFactory>();
        provider.GetRequiredService<WorkflowRuntimeOptions>().ApplyRegistrations(registrations);
        registrations.Received(1).RegisterWorkflow<AutomaticInteractionWorkflow>(Arg.Any<string>());
        registrations.Received(1).RegisterActivity<ExecuteAutomaticInteractionStepActivity>(Arg.Any<string>());
        provider.GetServices<IHostedService>().Count(service => service.GetType().FullName == WorkflowWorkerType).ShouldBe(1);

        var factory = provider.GetServices<IWorkflowsFactory>().ShouldHaveSingleItem();
        factory.TryCreateWorkflow(nameof(AutomaticInteractionWorkflow), scope.ServiceProvider, out var workflow, out var workflowError).ShouldBeTrue();
        workflowError.ShouldBeNull();
        workflow.ShouldBeOfType<AutomaticInteractionWorkflow>();
        factory.TryCreateActivity(nameof(ExecuteAutomaticInteractionStepActivity), scope.ServiceProvider, out var activity, out var activityError).ShouldBeTrue();
        activityError.ShouldBeNull();
        activity.ShouldBeOfType<ExecuteAutomaticInteractionStepActivity>();
    }

    [Fact]
    public async Task QualifiedSchedulerUsesAnExplicitHostClientWhenRegistered()
    {
        var builder = CreateDomainHostBuilder();
        var client = Substitute.For<IDaprWorkflowClient>();
        builder.Services.AddSingleton(client);
        builder.Services.AddAgentsInteractionWorkflow(Substitute.For<IAgentDependencyAvailability>());
        await using var provider = builder.Services.BuildServiceProvider();
        var scheduler = provider.GetServices<IInteractionWorkflowScheduler>().ShouldHaveSingleItem();
        var fixture = new AutomaticInteractionReplayFixture();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var lookupFailure = new InvalidOperationException("host client lookup");
        client.GetWorkflowStateAsync(fixture.InteractionId, false, timeout.Token)
            .Returns(Task.FromException<WorkflowState>(lookupFailure));

        var error = await Should.ThrowAsync<InvalidOperationException>(() => scheduler.EnsureScheduledAsync(fixture.Input, timeout.Token));

        error.ShouldBeSameAs(lookupFailure);
        await client.Received(1).GetWorkflowStateAsync(fixture.InteractionId, false, timeout.Token);
    }

    private const string WorkflowWorkerType = "Dapr.Workflow.Worker.WorkflowWorker";

    private static WebApplicationBuilder CreateDomainHostBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddEventStoreDomainService(typeof(AgentsAssemblyMarker).Assembly, typeof(ServerAssemblyMarker).Assembly);
        AgentDomainHostComposition.Configure(builder);
        return builder;
    }
}
