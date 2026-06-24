using System;
using System.Linq;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.UI.Components.Shared;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC1, AC3 — the pure generation-kind version-label mapping. Every switch is total over <see cref="AgentGenerationKind"/>
/// so a reserved/future kind renders through a safe default (never throws); generated and edited versions get
/// <b>distinct</b> whole-string label keys (the AC3 core: generated vs edited content labeled distinctly).
/// </summary>
public sealed class AgentGenerationKindPresentationTests
{
    [Theory]
    [InlineData(AgentGenerationKind.Generated, "Agents.GenerationKind.Label.Generated")]
    [InlineData(AgentGenerationKind.Edited, "Agents.GenerationKind.Label.Edited")]
    [InlineData(AgentGenerationKind.Regenerated, "Agents.GenerationKind.Label.Regenerated")]
    [InlineData(AgentGenerationKind.Unknown, "Agents.GenerationKind.Label.Unknown")]
    public void LabelKeyFor_maps_each_kind_to_its_whole_string_key(AgentGenerationKind kind, string expected)
        => AgentGenerationKindPresentation.LabelKeyFor(kind).ShouldBe(expected);

    [Fact]
    public void LabelKeyFor_is_total_and_never_throws_over_every_kind()
    {
        foreach (AgentGenerationKind kind in Enum.GetValues<AgentGenerationKind>())
        {
            AgentGenerationKindPresentation.LabelKeyFor(kind).ShouldNotBeNullOrWhiteSpace();
        }

        // A reserved value not yet defined still maps through the total default.
        AgentGenerationKindPresentation.LabelKeyFor((AgentGenerationKind)999).ShouldBe("Agents.GenerationKind.Label.Unknown");
    }

    [Fact]
    public void Generated_edited_and_regenerated_get_distinct_labels()
    {
        string generated = AgentGenerationKindPresentation.LabelKeyFor(AgentGenerationKind.Generated);
        string edited = AgentGenerationKindPresentation.LabelKeyFor(AgentGenerationKind.Edited);
        string regenerated = AgentGenerationKindPresentation.LabelKeyFor(AgentGenerationKind.Regenerated);

        new[] { generated, edited, regenerated }.Distinct().Count().ShouldBe(3);
    }
}
