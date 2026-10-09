using System.Text.Json;

using Hexalith.EventStore.Contracts.Projections;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Hexalith.EventStore.DomainService.Tests;

public sealed class DomainProjectionCancellationTests {
    [Fact]
    public void Project_ForwardsOriginalCallerTokenToHandler() {
        IDomainProjectionHandler handler = Substitute.For<IDomainProjectionHandler>();
        _ = handler.Domain.Returns("test-domain");
        using var cancellation = new CancellationTokenSource();
        var request = new ProjectionRequest("tenant", "test-domain", "aggregate", []);
        var expected = new ProjectionResponse("test-projection", JsonSerializer.SerializeToElement(new { Count = 1 }));
        _ = handler.Project(Arg.Any<ProjectionRequest>(), cancellation.Token).Returns(expected);
        using ServiceProvider provider = new ServiceCollection().AddSingleton(handler).BuildServiceProvider();

        ProjectionResponse? response = DomainProjectionDispatcher.Project(provider, request, cancellation.Token);

        response.ShouldBeSameAs(expected);
        _ = handler.Received(1).Project(request, cancellation.Token);
    }
}
