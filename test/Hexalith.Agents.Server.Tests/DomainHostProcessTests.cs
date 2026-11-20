namespace Hexalith.Agents.Server.Tests;

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using Hexalith.Agents.Contracts.Operations;
using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using Shouldly;

public sealed class DomainHostProcessTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecutableServesProtectedOperations(bool configured)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        DirectoryInfo directory = Directory.CreateTempSubdirectory("agents-host-");
        using var host = new Process();
        bool started = false;
        Task<string>? errors = null;
        Task<string>? output = null;
        try
        {
            string key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            ProcessStartInfo start = host.StartInfo;
            start.FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
            start.ArgumentList.Add(typeof(ServerAssemblyMarker).Assembly.Location);
            start.WorkingDirectory = directory.FullName;
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            // Start without any developer credentials, live endpoints or settings inherited from the runner.
            start.Environment.Clear();
            foreach (string name in new[] { "PATH", "DOTNET_ROOT", "SystemRoot", "TMP", "TEMP", "TMPDIR" })
            {
                if (Environment.GetEnvironmentVariable(name) is { } value)
                {
                    start.Environment[name] = value;
                }
            }

            start.Environment["HOME"] = directory.FullName;
            start.Environment["USERPROFILE"] = directory.FullName;
            start.Environment["DOTNET_ENVIRONMENT"] = configured ? "Testing" : "Development";
            start.Environment["ASPNETCORE_URLS"] = "http://127.0.0.1:0";
            if (configured)
            {
                start.Environment["APP_API_TOKEN"] = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                start.Environment["Authentication__JwtBearer__Issuer"] = "smoke-issuer";
                start.Environment["Authentication__JwtBearer__Audience"] = "smoke-agents";
                start.Environment["Authentication__JwtBearer__SigningKey"] = key;
                start.Environment["Authentication__JwtBearer__AllowInsecureSymmetricKey"] = "true";
                start.Environment["Agents__EventStore__AppId"] = "eventstore";
                start.Environment["Agents__EventStore__BaseUrl"] = "http://127.0.0.1:1";
            }

            started = host.Start();
            errors = host.StandardError.ReadToEndAsync();
            using var client = new HttpClient(new HttpClientHandler { UseProxy = false })
            {
                BaseAddress = await ReadAddressAsync(host, errors, timeout.Token),
            };
            output = host.StandardOutput.ReadToEndAsync();
            const string route = "/api/v1/agents/operations/agents/hexa/status";
            using HttpResponseMessage anonymous = await client.GetAsync(route, timeout.Token);
            anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

            var token = new SecurityTokenDescriptor
            {
                Issuer = "smoke-issuer",
                Audience = "smoke-agents",
                Claims = new Dictionary<string, object> { ["sub"] = "operator" },
                Expires = DateTime.UtcNow.AddMinutes(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256),
            };
            var handler = new JsonWebTokenHandler();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", handler.CreateToken(token));
            using HttpResponseMessage human = await client.GetAsync(route, timeout.Token);
            human.StatusCode.ShouldBe(configured ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.Unauthorized);
            if (configured)
            {
                var outcome = await human.Content.ReadFromJsonAsync<AgentOperationResult>(timeout.Token);
                outcome!.Error!.Code.ShouldBe(AgentOperationErrorCode.DependencyNotAvailable);
                outcome.Error.Dependency!.RecordId.ShouldBe("EXT-HOST-1");
                token.Claims[EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType] = "eventstore";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", handler.CreateToken(token));
                using HttpResponseMessage workload = await client.GetAsync(route, timeout.Token);
                workload.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            }
        }
        finally
        {
            if (started && !host.HasExited)
            {
                host.Kill(entireProcessTree: true);
                await host.WaitForExitAsync();
            }

            await Task.WhenAll(errors ?? Task.CompletedTask, output ?? Task.CompletedTask);
            directory.Delete(recursive: true);
        }
    }

    private static async Task<Uri> ReadAddressAsync(Process host, Task<string> errors, CancellationToken cancellationToken)
    {
        while (await host.StandardOutput.ReadLineAsync(cancellationToken) is { } line)
        {
            Match listening = Regex.Match(line, @"Now listening on: (http://127[.]0[.]0[.]1:[0-9]+)");
            if (listening.Success)
            {
                return new Uri(listening.Groups[1].Value);
            }
        }

        throw new InvalidOperationException("The domain service exited before listening. " + await errors);
    }
}
