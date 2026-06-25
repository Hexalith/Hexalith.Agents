namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// A complete launch-readiness metric definition (Story 4.4 AC2; FR-28; PRD §11). Each launch metric (e.g.
/// <c>SM-2</c>, <c>SM-3</c>) must define its <see cref="Numerator"/>, <see cref="Denominator"/>,
/// <see cref="Target"/> threshold, <see cref="MeasurementWindow"/>, and <see cref="LaunchCohort"/> so the launch
/// decision is recorded on explicit thresholds rather than implicit assumptions.
/// </summary>
/// <remarks>
/// All string fields are non-empty governance descriptors — safe metric metadata, never secrets, raw provider
/// payloads, or Party PII (AD-14). Concrete threshold <em>values</em> for SM-2/SM-3 are accepted downstream
/// governance blockers (OQ-5/OQ-6/OQ-11); this contract records the structure and mechanism, not provider-specific
/// numbers. The free-text descriptors are safe governance data but are kept out of telemetry dimensions (AD-14).
/// </remarks>
/// <param name="MetricId">The stable metric identifier (e.g. <c>"SM-2"</c>).</param>
/// <param name="Classification">Whether the metric is a primary, secondary, or counter-metric (AC2).</param>
/// <param name="Numerator">The governance descriptor of the metric numerator.</param>
/// <param name="Denominator">The governance descriptor of the metric denominator (e.g. the eligible-Conversation cohort).</param>
/// <param name="Target">The target threshold value for the metric.</param>
/// <param name="MeasurementWindow">The governance descriptor of the measurement window.</param>
/// <param name="LaunchCohort">The governance descriptor of the launch cohort the metric applies to.</param>
public record LaunchMetricDefinition(
    string MetricId,
    LaunchMetricClassification Classification,
    string Numerator,
    string Denominator,
    decimal Target,
    string MeasurementWindow,
    string LaunchCohort);
