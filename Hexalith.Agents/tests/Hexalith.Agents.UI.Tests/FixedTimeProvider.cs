using System;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// Deterministic <see cref="TimeProvider"/> double for the proposal-queue tests. The page derives the "age" column
/// from <c>TimeProvider.GetUtcNow()</c>, so a fixed clock keeps the rendered age bucket stable (no
/// <see cref="DateTimeOffset.UtcNow"/> in the component path — avoids flaky tests). A dependency-free substitute that
/// adds no NuGet package to the story.
/// </summary>
public sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    /// <summary>Gets or sets the fixed instant returned by <see cref="GetUtcNow"/>.</summary>
    public DateTimeOffset UtcNow { get; set; } = utcNow;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => UtcNow;
}
