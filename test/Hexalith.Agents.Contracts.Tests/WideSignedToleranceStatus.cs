namespace Hexalith.Agents.Contracts.Tests;

/// <summary>Provides signed 64-bit enum bounds for serialization compatibility tests.</summary>
internal enum WideSignedToleranceStatus : long
{
    /// <summary>The fail-closed fallback.</summary>
    Unknown = 0,

    /// <summary>The lowest signed 64-bit ordinal.</summary>
    Minimum = long.MinValue,

    /// <summary>The highest signed 64-bit ordinal.</summary>
    Maximum = long.MaxValue,
}
