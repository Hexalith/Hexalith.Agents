using Hexalith.Agents.Server.Ports;
#if HEXALITH_PARTIES_SOURCE
using Hexalith.Parties.Contracts.Models;
using Hexalith.Parties.Contracts.ValueObjects;
#endif

namespace Hexalith.Agents.Server.Tests;

/// <summary>Contract-valid local identity responses; these fixtures confer no owner availability.</summary>
internal static class Story54PartyIdentityFixture
{
    /// <summary>The opaque stable actor, deliberately distinct from its authenticated subject.</summary>
    internal const string ActorId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";

    /// <summary>Creates transient current tenant-authorized actor evidence.</summary>
    internal static CurrentHumanActorEvidence Actor(Story54TimeProvider clock)
        => new("tenant-a", "authenticated-subject", ActorId, "party-a", 7, 11, clock.Now, clock.Now.AddMinutes(1));

#if HEXALITH_PARTIES_SOURCE
    /// <summary>Creates a current binding with finite custody derived from its immutable effective time.</summary>
    internal static HumanActorBindingEvidence Binding(Story54TimeProvider clock)
        => new("tenant-a", "party-a", ActorId, 7, 3, clock.Now.AddDays(-1), clock.Now.AddMinutes(1), "actor-source", "operator-provenance",
            new("history-policy-v1", "party-actor-history-v1", clock.Now.AddDays(364), 5, "custody-evidence", true, true, true));

    /// <summary>Creates an eligible Human identity with its required binding.</summary>
    internal static PartyIdentityEvidence Identity(Story54TimeProvider clock)
        => new(1, "tenant-a", "party-a", PartyIdentityClassification.Human, true, false, false, 11, clock.Now, "observation", Binding(clock));
#endif
}
