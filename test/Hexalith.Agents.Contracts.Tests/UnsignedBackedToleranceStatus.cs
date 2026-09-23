namespace Hexalith.Agents.Contracts.Tests;

/// <summary>Provides unsigned 64-bit enum bounds for serialization compatibility tests.</summary>
internal enum UnsignedBackedToleranceStatus : ulong
{
    /// <summary>The fail-closed fallback.</summary>
    Unknown = 0,

    /// <summary>A declared positive ordinal within the signed integer range.</summary>
    Ready = 1,

    /// <summary>The highest unsigned 64-bit ordinal.</summary>
    Maximum = ulong.MaxValue,
}
