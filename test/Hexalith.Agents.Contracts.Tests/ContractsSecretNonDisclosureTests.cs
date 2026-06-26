namespace Hexalith.Agents.Contracts.Tests;

using System.Linq;
using System.Reflection;

using Hexalith.Agents.Contracts;

using Shouldly;

/// <summary>
/// Secret / provider non-disclosure guard (AC3; AD-9, AD-14). No public type in the contracts surface may
/// expose a secret-bearing member name or a provider-SDK type. For the empty shell this passes trivially
/// and grows with the contracts to keep credentials and provider details out of the public boundary.
/// </summary>
public sealed class ContractsSecretNonDisclosureTests
{
    private const BindingFlags PublicMembers =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly string[] _forbiddenMemberNameTokens =
    [
        "Secret", "ApiKey", "Credential", "Password", "ConnectionString",
    ];

    private static readonly string[] _forbiddenTypeNamespacePrefixes =
    [
        "Dapr", "Azure.AI", "OpenAI", "Anthropic",
        "Microsoft.SemanticKernel", "Microsoft.Agents", "ModelContextProtocol",
    ];

    [Fact]
    public void ContractsShouldNotExposeSecretBearingMemberNames()
    {
        Assembly contracts = typeof(AgentsContractsAssemblyMarker).Assembly;

        foreach (Type type in contracts.GetExportedTypes())
        {
            foreach (MemberInfo member in type.GetMembers(PublicMembers))
            {
                bool leaks = _forbiddenMemberNameTokens.Any(token =>
                    member.Name.Contains(token, StringComparison.OrdinalIgnoreCase));

                leaks.ShouldBeFalse(
                    $"Public member '{type.FullName}.{member.Name}' exposes a secret-bearing name in the contracts surface.");
            }
        }
    }

    [Fact]
    public void ContractsShouldNotExposeProviderSdkTypes()
    {
        Assembly contracts = typeof(AgentsContractsAssemblyMarker).Assembly;

        foreach (Type type in contracts.GetExportedTypes())
        {
            foreach (PropertyInfo property in type.GetProperties(PublicMembers))
            {
                AssertNotProviderType(property.PropertyType, $"{type.FullName}.{property.Name}");
            }

            foreach (FieldInfo field in type.GetFields(PublicMembers))
            {
                AssertNotProviderType(field.FieldType, $"{type.FullName}.{field.Name}");
            }
        }
    }

    private static void AssertNotProviderType(Type type, string memberPath)
    {
        string typeNamespace = type.Namespace ?? string.Empty;

        bool isProvider = _forbiddenTypeNamespacePrefixes.Any(prefix =>
            typeNamespace.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        isProvider.ShouldBeFalse(
            $"Public member '{memberPath}' exposes provider-SDK type '{type.FullName}' in the contracts surface.");
    }
}
