using HydroponicsExpanded.Core;
using Xunit;

namespace HydroponicsExpanded.Tests;

/// <summary>The table check runs after a wake and retries until done, instead of reading the tables every second.</summary>
public class WakeGateTests
{
    [Fact]
    public void The_first_check_runs_at_once()
    {
        Assert.True(new WakeGate(1).IsDue(now: 0, wakes: 0));
    }

    [Fact]
    public void A_check_that_is_not_done_retries_after_the_wait()
    {
        var gate = new WakeGate(1);

        Assert.True(gate.IsDue(10, 0));
        Assert.False(gate.IsDue(10.5, 0));
        Assert.True(gate.IsDue(11, 0));
    }

    [Fact]
    public void A_done_check_waits_for_the_next_wake()
    {
        var gate = new WakeGate(1);

        Assert.True(gate.IsDue(10, 0));
        gate.Done();

        Assert.False(gate.IsDue(50, 0));
        Assert.True(gate.IsDue(50.1, 1));
    }

    [Fact]
    public void A_wake_runs_the_check_at_once_even_inside_the_wait()
    {
        var gate = new WakeGate(5);

        Assert.True(gate.IsDue(10, 0));
        Assert.True(gate.IsDue(10.1, 1));
        Assert.False(gate.IsDue(10.2, 1));
    }
}
