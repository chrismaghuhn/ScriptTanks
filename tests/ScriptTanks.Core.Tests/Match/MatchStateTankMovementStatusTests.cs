using System;
using System.Linq;
using ScriptTanks.Core.Match;
using Xunit;

namespace ScriptTanks.Core.Tests.Match;

public sealed class MatchStateTankMovementStatusTests
{
    [Theory]
    [InlineData(MatchStateTankMovementStatus.SkippedDestroyed, 0)]
    [InlineData(MatchStateTankMovementStatus.StayedStill, 1)]
    [InlineData(MatchStateTankMovementStatus.Moved, 2)]
    public void Enum_members_have_explicit_stable_values(
        MatchStateTankMovementStatus status,
        int expected)
    {
        Assert.Equal(expected, (int)status);
    }

    [Fact]
    public void Enum_contains_exactly_expected_members()
    {
        string[] names = Enum.GetNames<MatchStateTankMovementStatus>();

        Assert.Equal(3, names.Length);

        Assert.Contains(nameof(MatchStateTankMovementStatus.SkippedDestroyed), names);
        Assert.Contains(nameof(MatchStateTankMovementStatus.StayedStill), names);
        Assert.Contains(nameof(MatchStateTankMovementStatus.Moved), names);

        Assert.Equal(
            3,
            Enum.GetValues<MatchStateTankMovementStatus>().Cast<int>().Distinct().Count());
    }
}
