using System.Net.Http.Json;
using System.Security.Cryptography;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Exercises the SDK bound-workload proof verifier with ephemeral local keys and unchanged command intent.</summary>
internal sealed class FixtureBoundAgentCommandHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    private static readonly RSA _key = RSA.Create(2048);

    /// <summary>Gets the public fixture verifier; no private key enters configuration or retained evidence.</summary>
    internal static string PublicKey
    {
        get
        {
            lock (_key)
            {
                return Convert.ToBase64String(_key.ExportSubjectPublicKeyInfo());
            }
        }
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath == "/api/v1/commands")
        {
            SubmitCommandRequest command = (await request.Content!.ReadFromJsonAsync<SubmitCommandRequest>(cancellationToken))!;
            var bound = new BoundWorkloadCommandRequest(command, "system:agents");
            string proof;
            try
            {
                lock (_key)
                {
                    proof = BoundWorkloadCommandProof.Issue(bound, "agents", Convert.ToBase64String(_key.ExportPkcs8PrivateKey()), DateTimeOffset.UtcNow);
                }
            }
            catch (ArgumentException)
            {
                // The SDK refuses to sign malformed command payloads. Keep their HTTP negative path at its
                // real verifier boundary rather than minting authority for an invalid command shape.
                proof = "invalid-local-fixture-proof";
            }
            request.RequestUri = new(request.RequestUri, "/api/v1/commands/bound/workload");
            request.Content?.Dispose();
            request.Content = JsonContent.Create(bound);
            request.Headers.Add(BoundWorkloadCommandProof.HeaderName, proof);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
