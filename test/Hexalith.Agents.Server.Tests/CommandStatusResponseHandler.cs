using System.Net;
using System.Net.Http.Json;

using Hexalith.EventStore.Contracts.Commands;

using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Captures status transport requests without an external gateway.</summary>
internal sealed class CommandStatusResponseHandler : HttpMessageHandler
{
    /// <summary>Gets or sets the response status.</summary>
    internal HttpStatusCode ResponseCode { get; set; } = HttpStatusCode.OK;

    /// <summary>Gets or sets the returned command evidence.</summary>
    internal CommandStatusQueryResponse? Status { get; set; }

    /// <summary>Gets or sets an injected transport failure.</summary>
    internal Exception? Failure { get; set; }

    /// <summary>Gets the request count.</summary>
    internal int Requests { get; private set; }

    /// <summary>Gets the last requested route.</summary>
    internal string? Path { get; private set; }

    /// <summary>Gets the assertion header.</summary>
    internal string? Assertion { get; private set; }

    /// <summary>Gets the configured Dapr destination.</summary>
    internal string? DaprAppId { get; private set; }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests++;
        Path = request.RequestUri!.AbsolutePath;
        DaprAppId = request.Headers.TryGetValues("dapr-app-id", out IEnumerable<string>? appIds) ? appIds.Single() : null;
        Assertion = request.Headers.GetValues("X-Hexalith-Workload-Assertion").Single();
        request.Headers.Authorization.ShouldBeNull();
        return Failure is not null
            ? Task.FromException<HttpResponseMessage>(Failure)
            : Task.FromResult(new HttpResponseMessage(ResponseCode) { Content = JsonContent.Create(Status) });
    }
}
