using System.Globalization;
using System.Security.Claims;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;

using Hexalith.EventStore.Authentication;
using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.EventStore;

internal sealed class AgentsTrustedCommandExtensionPolicy(string agentsAppId) : ITrustedCommandExtensionPolicy
{
    private const string AgentDomain = "agent";
    private const string DaprCallerAppIdClaim = "dapr_caller_app_id";

    private static readonly HashSet<string> _setupCommandTypes =
    [
        nameof(CreateAgent),
        nameof(UpdateAgentConfiguration),
        nameof(ConfigureAgentResponseMode),
        nameof(ActivateAgent),
        nameof(DisableAgent),
    ];

    private readonly string _agentsAppId = agentsAppId;

    internal string AgentsAppId => _agentsAppId;

    public bool Claims(string domain, string commandType, string key)
        => string.Equals(domain, AgentDomain, StringComparison.Ordinal)
            && _setupCommandTypes.Contains(commandType)
            && (string.Equals(key, AgentSetupTrustedExtensions.AgentAdministrator, StringComparison.Ordinal)
                || (string.Equals(commandType, nameof(ActivateAgent), StringComparison.Ordinal)
                    && (string.Equals(key, AgentSetupTrustedExtensions.ProviderSelectionValidation, StringComparison.Ordinal)
                        || string.Equals(key, AgentSetupTrustedExtensions.ApproverPolicyValidation, StringComparison.Ordinal)
                        || string.Equals(key, AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion, StringComparison.Ordinal))));

    public bool Accepts(ClaimsPrincipal principal, SubmitCommandRequest command, string key, string value)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(command);

        ClaimsIdentity[] authenticatedIdentities = principal.Identities
            .Where(identity => identity.IsAuthenticated)
            .ToArray();
        if (authenticatedIdentities.Length != 1
            || !string.Equals(
                authenticatedIdentities[0].AuthenticationType,
                DaprInternalAuthenticationOptions.SchemeName,
                StringComparison.Ordinal)
            || authenticatedIdentities[0].FindAll(DaprCallerAppIdClaim).Select(claim => claim.Value).Count(value =>
                string.Equals(value, _agentsAppId, StringComparison.Ordinal)) != 1
            || principal.FindAll(DaprCallerAppIdClaim).Count() != 1
            || !Claims(command.Domain, command.CommandType, key))
        {
            return false;
        }

        if (string.Equals(key, AgentSetupTrustedExtensions.AgentAdministrator, StringComparison.Ordinal))
        {
            return string.Equals(value, "true", StringComparison.Ordinal);
        }

        if (!string.Equals(command.CommandType, nameof(ActivateAgent), StringComparison.Ordinal))
        {
            return false;
        }

        if (string.Equals(key, AgentSetupTrustedExtensions.ProviderSelectionValidation, StringComparison.Ordinal))
        {
            return IsCanonicalEnum<ProviderSelectionValidationStatus>(value);
        }

        if (string.Equals(key, AgentSetupTrustedExtensions.ApproverPolicyValidation, StringComparison.Ordinal))
        {
            return IsCanonicalEnum<ApproverPolicyValidationStatus>(value);
        }

        return string.Equals(key, AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion, StringComparison.Ordinal)
            && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int version)
            && version > 0
            && string.Equals(version.ToString(CultureInfo.InvariantCulture), value, StringComparison.Ordinal);
    }

    private static bool IsCanonicalEnum<T>(string value)
        where T : struct, Enum
        => Enum.TryParse(value, ignoreCase: false, out T parsed)
            && string.Equals(Enum.GetName(parsed), value, StringComparison.Ordinal);
}
