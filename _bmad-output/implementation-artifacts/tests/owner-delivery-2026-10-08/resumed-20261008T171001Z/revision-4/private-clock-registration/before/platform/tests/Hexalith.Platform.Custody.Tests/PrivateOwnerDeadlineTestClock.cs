using NSubstitute;

namespace Hexalith.Platform.Custody.Tests;

/// <summary>Controlled monotonic private-operation budget clock; timer callbacks fire only when the test advances its retained operational time.</summary>
internal static class PrivateOwnerDeadlineTestClock
{
    /// <summary>Creates a scoped deterministic clock and its explicit elapsed-time/timer advancement.</summary>
    internal static (TimeProvider Clock, Action<TimeSpan> Advance) Create()
    {
        var clock = Substitute.For<TimeProvider>(); long ticks = 0;
        var callbacks = new System.Collections.Concurrent.ConcurrentDictionary<int, Action>(); int timerId = 0;
        clock.TimestampFrequency.Returns(TimeSpan.TicksPerSecond); clock.GetTimestamp().Returns(_ => Interlocked.Read(ref ticks));
        clock.GetUtcNow().Returns(_ => new DateTimeOffset(2026, 10, 9, 0, 0, 0, TimeSpan.Zero).AddTicks(Interlocked.Read(ref ticks)));
        clock.CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object?>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>()).Returns(call =>
        {
            int id = Interlocked.Increment(ref timerId); var timer = Substitute.For<ITimer>();
            callbacks[id] = () => call.Arg<TimerCallback>()(call.ArgAt<object?>(1));
            timer.When(t => t.Dispose()).Do(callInfo => callbacks.TryRemove(id, out _));
            if (call.ArgAt<TimeSpan>(2) <= TimeSpan.Zero) { callbacks[id](); }
            return timer;
        });
        return (clock, elapsed =>
        {
            // The worker can enter a supplied collection before its awaiting continuation registers the timer.
            // Advance only after that timer exists; direct unbounded pre-fix captures deliberately have none.
            if (elapsed > TimeSpan.Zero) { _ = SpinWait.SpinUntil(() => !callbacks.IsEmpty, TimeSpan.FromSeconds(1)); }
            Interlocked.Add(ref ticks, elapsed.Ticks);
            foreach (var callback in callbacks.Values.ToArray()) { callback(); }
        });
    }
}
