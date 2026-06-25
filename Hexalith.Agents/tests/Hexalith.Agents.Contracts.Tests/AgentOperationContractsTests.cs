namespace Hexalith.Agents.Contracts.Tests;

using System.Reflection;
using System.Text.Json;

using Hexalith.Agents.Contracts.Operations;

using Shouldly;

/// <summary>
/// Public operation-envelope contract tests for Story 4.1.
/// </summary>
public sealed class AgentOperationContractsTests
{
    private static readonly string[] _poisonValues =
    [
        "prompt text",
        "generated content",
        "edited content",
        "provider secret",
        "raw provider payload",
        "System.InvalidOperationException",
        "stack trace",
        "other-tenant-id",
        "EventStore stream",
    ];

    [Fact]
    public void Operation_result_round_trips_status_enums_by_name()
    {
        AgentOperationResult<string> original = AgentOperationResult<string>.Succeeded("ok");

        string json = JsonSerializer.Serialize(original);
        AgentOperationResult<string>? roundTrip = JsonSerializer.Deserialize<AgentOperationResult<string>>(json);

        json.ShouldContain("\"Status\":\"Succeeded\"");
        roundTrip.ShouldNotBeNull();
        roundTrip.Status.ShouldBe(AgentOperationStatus.Succeeded);
        roundTrip.Value.ShouldBe("ok");
    }

    [Fact]
    public void Operation_error_factory_uses_safe_messages()
    {
        AgentOperationError error = AgentOperationError.FromCode(AgentOperationErrorCode.Unavailable);

        error.Code.ShouldBe(AgentOperationErrorCode.Unavailable);
        error.Message.ShouldBe("The operation is currently unavailable.");
    }

    [Fact]
    public void Operation_error_factory_does_not_serialize_poison_values()
    {
        string json = JsonSerializer.Serialize(AgentOperationResult.Unavailable());

        foreach (string poison in _poisonValues)
        {
            json.ShouldNotContain(poison, Case.Insensitive);
        }
    }

    [Fact]
    public void Operation_enums_use_unknown_zero_and_serialize_by_name()
    {
        Type[] enumTypes =
        [
            typeof(AgentOperationErrorCode),
            typeof(AgentOperationStatus),
            typeof(AgentReadinessStatus),
            typeof(ProviderModelReadinessStatus),
            typeof(AgentCallOperationStatus),
            typeof(ProposalOperationStatus),
            typeof(AuditAvailabilityStatus),
        ];

        foreach (Type enumType in enumTypes)
        {
            Enum.ToObject(enumType, 0).ToString().ShouldBe("Unknown", $"{enumType.Name} must reserve 0 for Unknown.");

            object nonZero = Enum.GetValues(enumType).Cast<object>().First(value => Convert.ToInt32(value) != 0);
            string json = JsonSerializer.Serialize(nonZero, enumType);
            json.ShouldBe($"\"{nonZero}\"", $"{enumType.Name} must serialize by enum name.");
        }
    }

    [Fact]
    public void Operation_contract_members_do_not_include_sensitive_names()
    {
        string[] forbiddenNameParts =
        [
            "Secret",
            "ApiKey",
            "Credential",
            "Password",
            "ConnectionString",
            "Prompt",
            "GeneratedContent",
            "EditedContent",
            "StackTrace",
            "StreamName",
            "TenantFingerprint",
        ];

        Type[] operationTypes = typeof(AgentOperationResult).Assembly.GetTypes()
            .Where(type => type.Namespace == "Hexalith.Agents.Contracts.Operations")
            .ToArray();

        foreach (Type type in operationTypes)
        {
            foreach (MemberInfo member in type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                foreach (string forbidden in forbiddenNameParts)
                {
                    member.Name.ShouldNotContain(forbidden, Case.Sensitive);
                }
            }
        }
    }

    [Fact]
    public void Pending_and_degraded_status_terms_are_not_success()
    {
        AgentOperationResult.Pending().IsSuccess.ShouldBeFalse();
        new AgentOperationResult(AgentOperationStatus.Checking).IsSuccess.ShouldBeFalse();
        new AgentOperationResult(AgentOperationStatus.Degraded).IsSuccess.ShouldBeFalse();

        AgentOperationResult<ProposalOperationStatus>.Succeeded(ProposalOperationStatus.PostingPending)
            .Value
            .ShouldBe(ProposalOperationStatus.PostingPending);
        ProposalOperationStatus.PostingPending.ShouldNotBe(ProposalOperationStatus.Posted);
        AuditAvailabilityStatus.AuditPending.ShouldNotBe(AuditAvailabilityStatus.AuditAvailable);
        AgentReadinessStatus.Checking.ShouldNotBe(AgentReadinessStatus.Callable);
    }
}
