namespace Hexalith.Agents.Server.Tests;

/// <summary>Intercepts serialized public identity requests without a network transport.</summary>
/// <param name="reply">Produces a local response for the captured HTTP request.</param>
internal sealed class CallerIdentityReplyHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return reply(request, cancellationToken);
    }
}
