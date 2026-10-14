namespace Hexalith.Agents.Server.Tests;

/// <summary>A deterministic local evaluation clock, never production qualification evidence.</summary>
internal sealed class Story54TimeProvider : TimeProvider
{
    /// <summary>Gets or sets the local fixture instant.</summary>
    public DateTimeOffset Now { get; set; } = new(2026, 10, 14, 10, 0, 0, TimeSpan.Zero);
    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => Now;
}
