namespace Hexalith.Agents.Server.Tests;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Hexalith.Agents;                              // AgentsAssemblyMarker (domain assembly)
using Hexalith.Agents.Contracts;                    // AgentsContractsAssemblyMarker (contracts assembly)
using Hexalith.Agents.Contracts.AgentInteraction;   // AgentGenerationOutcome
using Hexalith.Agents.Server.Ports;                 // Deferred* ports, AgentGenerationProviderRequest/Result

using Hexalith.Agents.Server.Tests.Conformance;     // RequirementTraits

using Hexalith.EventStore.Contracts.Commands;       // CommandEnvelope

using Shouldly;

/// <summary>
/// Story 4.5 — cross-assembly runtime-ownership & SDK-purity conformance (AC2). Closes the AD-18/AD-19 purity gap left by
/// the per-assembly Contracts guards: the EventStore <b>domain</b> surface and the public contracts must expose no
/// framework/provider/workflow SDK type and reference no such assembly; the SDK-free <c>src/</c> projects must declare no
/// provider/runtime/workflow package; and V1 must have <b>exactly one durable owner</b> at the fail-closed seam — there is
/// no live workflow/background worker, the deferred generation/dispatch ports fail closed, and the live runtime owner
/// (Agent Framework workflow/session restore, Dapr Workflow ownership, MCP/A2A/tool schemas) is deferred per
/// <c>ARCHITECTURE-SPINE.md#Deferred</c>, verified here at the seam (no live owner, no SDK leak) rather than fabricated.
/// Every failure message cites AD-18/AD-19/AD-13.
/// </summary>
[Trait(RequirementTraits.Architecture, "AD-18")]
[Trait(RequirementTraits.Architecture, "AD-19")]
public sealed class RuntimeOwnershipConformanceTests
{
    private const BindingFlags PublicMembers =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <summary>Framework / provider / workflow SDK namespace prefixes forbidden in the public + EventStore-domain surface (AD-18/AD-19).</summary>
    private static readonly string[] _forbiddenSdkNamespacePrefixes =
    [
        "Microsoft.Agents",          // Microsoft.Agents.AI / Microsoft.Agents.AI.Workflows
        "Dapr",                      // Dapr runtime / Dapr.Workflow / Dapr AI
        "Microsoft.SemanticKernel",  // agent-runtime SDK
        "Microsoft.Extensions.AI",   // AI runtime
        "Azure.AI",                  // provider SDK
        "OpenAI",                    // provider SDK
        "Anthropic",                 // provider SDK
        "ModelContextProtocol",      // MCP tool-host runtime
    ];

    /// <summary>Provider / runtime / workflow SDK package + assembly prefixes (same families, name space).</summary>
    private static readonly string[] _forbiddenSdkPackagePrefixes = _forbiddenSdkNamespacePrefixes;

    /// <summary>The SDK-free source projects (public contracts + EventStore domain + admin UI). SDK packages may live only in Server/.AppHost/.Aspire when a live owner is bound (deferred).</summary>
    private static readonly string[] _sdkFreeProjects =
    [
        "Hexalith.Agents",            // EventStore domain
        "Hexalith.Agents.Contracts",  // public contracts
        "Hexalith.Agents.Client",     // public client
        "Hexalith.Agents.UI",         // admin UI
    ];

    /// <summary>Secret-bearing member-name tokens forbidden anywhere on the public domain/contracts surface (NFR-6, AD-9, AD-14).</summary>
    private static readonly string[] _forbiddenSecretMemberNameTokens =
    [
        "Secret", "ApiKey", "Credential", "Password", "ConnectionString",
    ];

    // ===== AD-18/AD-19: cross-assembly SDK purity =====

    [Fact]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.SdkPurity)]
    public void Domain_and_contracts_public_surface_exposes_no_framework_provider_or_workflow_sdk_type()
    {
        foreach (Assembly assembly in new[] { typeof(AgentsAssemblyMarker).Assembly, typeof(AgentsContractsAssemblyMarker).Assembly })
        {
            foreach (Type type in assembly.GetExportedTypes())
            {
                foreach (PropertyInfo property in type.GetProperties(PublicMembers))
                {
                    AssertSdkFree(property.PropertyType, $"{type.FullName}.{property.Name}");
                }

                foreach (FieldInfo field in type.GetFields(PublicMembers))
                {
                    AssertSdkFree(field.FieldType, $"{type.FullName}.{field.Name}");
                }

                foreach (MethodInfo method in type.GetMethods(PublicMembers))
                {
                    AssertSdkFree(method.ReturnType, $"{type.FullName}.{method.Name} (return)");
                    foreach (ParameterInfo parameter in method.GetParameters())
                    {
                        AssertSdkFree(parameter.ParameterType, $"{type.FullName}.{method.Name}({parameter.Name})");
                    }
                }
            }
        }
    }

    [Fact]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.SdkPurity)]
    public void Domain_and_contracts_assemblies_reference_no_framework_provider_or_workflow_sdk_assembly()
    {
        // The compiled-assembly-reference scan catches transitive SDK leaks the public-member scan misses.
        foreach (Assembly assembly in new[] { typeof(AgentsAssemblyMarker).Assembly, typeof(AgentsContractsAssemblyMarker).Assembly })
        {
            foreach (string referenced in assembly.GetReferencedAssemblies().Select(name => name.Name ?? string.Empty))
            {
                bool leaks = _forbiddenSdkPackagePrefixes.Any(prefix => referenced.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                leaks.ShouldBeFalse(
                    $"AD-18: '{assembly.GetName().Name}' references '{referenced}' — a framework/provider/workflow SDK must not reach the public contracts or EventStore domain assembly.");
            }
        }
    }

    [Fact]
    [Trait(RequirementTraits.Requirement, "NFR-6")]
    [Trait(RequirementTraits.Architecture, "AD-9")]
    [Trait(RequirementTraits.Architecture, "AD-14")]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.SecretNonDisclosure)]
    public void Domain_and_contracts_public_surface_exposes_no_secret_bearing_member_name()
    {
        // AC1 "provider-secret non-disclosure" gate, extended to the EventStore DOMAIN surface (events / aggregates / state /
        // commands / queries) — not only the Contracts assembly that ContractsSecretNonDisclosureTests already scans. AD-14
        // requires secrets absent from events, projections, status, and audit, which all live in the domain assembly, so a
        // domain event that added an `ApiKey` / `Secret` / `Credential` string would slip past the SDK-TYPE scans above (a
        // string is not an SDK type). This closes that hole and makes NFR-6 ("secret non-disclosure + domain purity") true
        // here, where the governance-conformance report attributes it. Mirrors the secret-token idiom of
        // ContractsSecretNonDisclosureTests over the same two-assembly loop the SDK scans use.
        foreach (Assembly assembly in new[] { typeof(AgentsAssemblyMarker).Assembly, typeof(AgentsContractsAssemblyMarker).Assembly })
        {
            foreach (Type type in assembly.GetExportedTypes())
            {
                foreach (MemberInfo member in type.GetMembers(PublicMembers))
                {
                    bool leaks = _forbiddenSecretMemberNameTokens.Any(token =>
                        member.Name.Contains(token, StringComparison.OrdinalIgnoreCase));
                    leaks.ShouldBeFalse(
                        $"NFR-6/AD-14/AD-9: public member '{type.FullName}.{member.Name}' exposes a secret-bearing name on the '{assembly.GetName().Name}' surface — secrets must never reach events, projections, status, or audit.");
                }
            }
        }
    }

    [Fact]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.SdkPurity)]
    public void Sdk_free_source_projects_declare_no_provider_or_runtime_sdk_package_reference()
    {
        foreach (string project in _sdkFreeProjects)
        {
            string projectFile = Path.Combine(ModuleLayout.ModuleRoot, "src", project, $"{project}.csproj");
            File.Exists(projectFile).ShouldBeTrue($"Expected SDK-free project '{project}' to exist for the AD-18 boundary guard.");

            foreach (string package in PackageReferenceNames(projectFile))
            {
                bool leaks = _forbiddenSdkPackagePrefixes.Any(prefix => package.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                leaks.ShouldBeFalse($"AD-18: '{project}' declares a forbidden provider/runtime/workflow SDK package '{package}'.");
            }
        }
    }

    [Fact]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.SdkPurity)]
    public void No_central_package_or_module_project_pins_a_provider_or_runtime_sdk()
    {
        // The live runtime owner is deferred, so the module pins ZERO provider/runtime/workflow SDK versions (CPM means a
        // project cannot reference a package the central manifest does not pin — this is the strongest single guard).
        XDocument packages = XDocument.Load(ModuleLayout.RootFile("Directory.Packages.props"));
        foreach (string version in packages.Descendants()
                     .Where(e => string.Equals(e.Name.LocalName, "PackageVersion", StringComparison.OrdinalIgnoreCase))
                     .Select(e => (string?)e.Attribute("Include"))
                     .Where(include => !string.IsNullOrWhiteSpace(include))
                     .Select(include => include!))
        {
            bool leaks = _forbiddenSdkPackagePrefixes.Any(prefix => version.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            leaks.ShouldBeFalse($"AD-18: Directory.Packages.props pins provider/runtime/workflow SDK '{version}' — the live owner is deferred, so zero such versions may be pinned.");
        }

        foreach (string projectFile in ModuleLayout.ProjectFiles)
        {
            foreach (string package in PackageReferenceNames(projectFile))
            {
                bool leaks = _forbiddenSdkPackagePrefixes.Any(prefix => package.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                leaks.ShouldBeFalse($"AD-18: '{Path.GetFileName(projectFile)}' references provider/runtime/workflow SDK '{package}' — no module project may bind one while the live owner is deferred.");
            }
        }
    }

    // ===== AD-18: exactly one durable owner at the fail-closed seam =====

    [Fact]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.RuntimeOwnership)]
    public void Exactly_one_durable_owner_no_live_workflow_or_background_worker()
    {
        // (a) The Workflows + Projections extension points hold no compiled durable owner / live read-model binding.
        string serverRoot = Path.Combine(ModuleLayout.ModuleRoot, "src", "Hexalith.Agents.Server");
        Directory.GetFiles(Path.Combine(serverRoot, "Application", "Workflows"), "*.cs", SearchOption.AllDirectories)
            .ShouldBeEmpty("AD-18: Server/Application/Workflows must hold no compiled durable-owner type (the live workflow owner is deferred).");
        Directory.GetFiles(Path.Combine(serverRoot, "Projections"), "*.cs", SearchOption.AllDirectories)
            .ShouldBeEmpty("AD-18: Server/Projections must hold no compiled read-model binding (the live projection owner is deferred).");

        // (b) No exported type in the domain or Server surface is a durable-workflow owner ([Workflow] / workflow base
        //     type) or a second in-memory background worker (IHostedService / BackgroundService) that could mutate
        //     AgentInteraction state outside the single EventStore command path.
        foreach (Assembly assembly in new[] { typeof(AgentsAssemblyMarker).Assembly, typeof(DeferredAgentCommandDispatcher).Assembly })
        {
            foreach (Type type in assembly.GetExportedTypes())
            {
                type.GetCustomAttributesData()
                    .Any(a => a.AttributeType.Name == "WorkflowAttribute" || IsForbiddenSdkNamespace(a.AttributeType.Namespace))
                    .ShouldBeFalse($"AD-18: '{type.FullName}' declares a durable-workflow attribute — V1 has no live durable owner.");

                for (Type? baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
                {
                    IsForbiddenSdkNamespace(baseType.Namespace)
                        .ShouldBeFalse($"AD-18: '{type.FullName}' derives from SDK type '{baseType.FullName}' — no framework/workflow durable owner may exist in V1.");
                }

                type.GetInterfaces().Any(i => i.Name is "IHostedService" or "IHostedLifecycleService")
                    .ShouldBeFalse($"AD-18: '{type.FullName}' is a background worker — the only path that mutates AgentInteraction state is an EventStore command, never a second in-memory worker.");
                IsBackgroundServiceSubclass(type)
                    .ShouldBeFalse($"AD-18: '{type.FullName}' is a BackgroundService — V1 has no second in-memory mutation owner.");
            }
        }
    }

    [Fact]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.RuntimeOwnership)]
    [Trait(RequirementTraits.Architecture, "AD-12")]
    public async Task Deferred_generation_provider_and_command_dispatcher_fail_closed()
    {
        // The generation port fails closed by RETURNING Unavailable (an accidental call yields GenerationFailed, never a
        // raw model invocation); the command dispatcher fails closed by THROWING (a missing binding can never be mistaken
        // for a successful dispatch). Reuse DeferredAgentCommandDispatcherTests as the canonical fail-closed proof.
        var generation = new DeferredAgentGenerationProvider();
        AgentGenerationProviderResult result = await generation.GenerateAsync(
            new AgentGenerationProviderRequest("openai", "gpt-4o", 1, "ctx", 16_000, 30_000, 3, "attempt-001"),
            CancellationToken.None);
        result.Outcome.ShouldBe(AgentGenerationOutcome.ProviderUnavailable, "AD-18/AD-12/FR-21: the deferred generation provider must fail closed (Unavailable), never invoke a live model.");

        var dispatcher = new DeferredAgentCommandDispatcher();
        CommandEnvelope envelope = new("msg-1", "acme", "agent", "hexa", "ActivateAgent", Array.Empty<byte>(), "corr-1", null, "admin-user", null);
        await Should.ThrowAsync<NotSupportedException>(async () => await dispatcher.DispatchAsync(envelope, CancellationToken.None));

        typeof(RuntimeOwnershipConformanceTests).Assembly.GetType("Hexalith.Agents.Server.Tests.DeferredAgentCommandDispatcherTests")
            .ShouldNotBeNull("AD-18: the deferred command-dispatcher fail-closed contract must remain proven by DeferredAgentCommandDispatcherTests.");
    }

    // ===== AD-13: deterministic generation/posting retry idempotency (where applicable) =====

    [Fact]
    [Trait(RequirementTraits.Architecture, "AD-13")]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.ReplayIdempotency)]
    public void Deterministic_generation_and_posting_id_contract_is_pinned_for_any_future_retry_owner()
    {
        // Live retry loops are deferred (no live workflow owner), so the AD-13 contract a future retry owner MUST honor —
        // deterministic generation attempt/version ids and deterministic posting MessageId/idempotency key, re-applied
        // without creating duplicate versions/messages — is pinned at the domain level (GovernanceConformanceTests Gate 4)
        // and proven per orchestrator here. Assert those server-side proofs remain present (a deletion fails this guard).
        Assembly serverTests = typeof(RuntimeOwnershipConformanceTests).Assembly;
        foreach (string suite in new[]
        {
            "AgentInteractionGenerationOrchestratorTests",  // deterministic generation attempt-id / version-id retries
            "AgentInteractionPostingOrchestratorTests",     // deterministic posting MessageId / idempotency-key retries
        })
        {
            serverTests.GetType($"Hexalith.Agents.Server.Tests.{suite}")
                .ShouldNotBeNull($"AD-13: the deterministic generation/posting retry-idempotency contract must remain proven by '{suite}'.");
        }
    }

    // ===== AD-19: "where applicable" — live runtime orchestration deferred, verified at the seam =====

    /// <summary>
    /// AD-19 "where applicable" deferral, verified at the fail-closed seam (NOT a fabricated pass for absent runtime).
    /// Agent Framework workflow/session restore, Dapr Workflow ownership, and MCP/A2A/tool-schema contracts are
    /// <b>deferred per <c>ARCHITECTURE-SPINE.md#Deferred</c></b>; this story verifies the seam (no live owner, no SDK leak)
    /// and the AC4 governance-conformance report records "verification deferred with fail-closed seam — re-verify when the
    /// live binding lands." When a live owner is bound, replace this with the real session-restore / MCP-A2A schema tests.
    /// </summary>
    [Fact]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.RuntimeOwnership)]
    public void Live_runtime_owner_session_restore_and_MCP_A2A_tool_schemas_are_deferred_and_fail_closed()
    {
        string serverRoot = Path.Combine(ModuleLayout.ModuleRoot, "src", "Hexalith.Agents.Server");

        // The seam: no live owner is bound (deferred), so there is no session-restore / MCP / A2A / tool-schema surface yet.
        Directory.GetFiles(Path.Combine(serverRoot, "Application", "Workflows"), "*.cs", SearchOption.AllDirectories)
            .ShouldBeEmpty("AD-19#Deferred: live runtime owner (workflow/session restore) is deferred — verified at the fail-closed seam.");
        Directory.Exists(Path.Combine(serverRoot, "Application", "Tools"))
            .ShouldBeTrue("AD-19#Deferred: the Tools extension point exists but is empty — MCP/A2A/tool-schema contracts are deferred.");
        Directory.GetFiles(Path.Combine(serverRoot, "Application", "Tools"), "*.cs", SearchOption.AllDirectories)
            .ShouldBeEmpty("AD-19#Deferred: no MCP/A2A/function-tool schema is wired yet — deferred per ARCHITECTURE-SPINE.md#Deferred.");

        // And no SDK leaked while deferred — the seam stays closed.
        typeof(AgentsAssemblyMarker).Assembly.GetReferencedAssemblies()
            .Select(name => name.Name ?? string.Empty)
            .Any(referenced => _forbiddenSdkPackagePrefixes.Any(prefix => referenced.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ShouldBeFalse("AD-19#Deferred: the deferred runtime seam must leak no provider/workflow SDK into the domain assembly.");
    }

    private static void AssertSdkFree(Type type, string memberPath)
    {
        if (TryFindForbiddenSdkType(type, out string offending))
        {
            offending.ShouldBeNullOrEmpty(
                $"AD-18: public member '{memberPath}' exposes framework/provider/workflow SDK type '{offending}' — the public contracts and EventStore domain surface must stay SDK-free.");
        }
    }

    private static bool TryFindForbiddenSdkType(Type type, out string offending)
    {
        offending = string.Empty;
        Type core = type;
        while (core.HasElementType)
        {
            core = core.GetElementType()!;
        }

        if (IsForbiddenSdkNamespace(core.Namespace))
        {
            offending = core.FullName ?? core.Name;
            return true;
        }

        if (core.IsGenericType)
        {
            foreach (Type argument in core.GetGenericArguments())
            {
                if (TryFindForbiddenSdkType(argument, out offending))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsForbiddenSdkNamespace(string? @namespace)
        => @namespace is not null
            && _forbiddenSdkNamespacePrefixes.Any(prefix => @namespace.StartsWith(prefix, StringComparison.Ordinal));

    private static bool IsBackgroundServiceSubclass(Type type)
    {
        for (Type? baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.Name is "BackgroundService")
            {
                return true;
            }
        }

        return false;
    }

    private static System.Collections.Generic.IEnumerable<string> PackageReferenceNames(string projectFile)
        => XDocument.Load(projectFile)
            .Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "PackageReference", StringComparison.OrdinalIgnoreCase))
            .Select(element => (string?)element.Attribute("Include"))
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!);
}
