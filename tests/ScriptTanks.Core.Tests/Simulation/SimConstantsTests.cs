using ScriptTanks.Core.Math;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Tests.Simulation;

public sealed class SimConstantsTests
{
    [Fact]
    public void TicksPerSecond_Is30()
    {
        Assert.Equal(30, SimConstants.TicksPerSecond);
    }

    [Fact]
    public void DefaultMatchDurationSeconds_Is180()
    {
        Assert.Equal(180, SimConstants.DefaultMatchDurationSeconds);
    }

    [Fact]
    public void DefaultMaxTicks_Is5400()
    {
        Assert.Equal(5400, SimConstants.DefaultMaxTicks);
    }

    [Fact]
    public void DefaultMaxTicks_IsConsistentWithTicksAndDuration()
    {
        Assert.Equal(
            SimConstants.TicksPerSecond * SimConstants.DefaultMatchDurationSeconds,
            SimConstants.DefaultMaxTicks);
    }

    [Fact]
    public void FixedDeltaTime_EqualsFixedFromRatio_1_30()
    {
        Assert.Equal(Fixed.FromRatio(1, 30), SimConstants.FixedDeltaTime);
    }

    [Fact]
    public void FixedDeltaTime_RawIs33_DueToTruncation()
    {
        // 1 * Fixed.Scale (1000) / 30 = 33 (truncated). This documents the
        // deterministic truncation drift highlighted on the field's XML doc.
        Assert.Equal(33, SimConstants.FixedDeltaTime.Raw);
    }
}
