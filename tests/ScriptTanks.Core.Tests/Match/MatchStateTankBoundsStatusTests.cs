using System;
using System.Linq;
using ScriptTanks.Core.Match;
using Xunit;

namespace ScriptTanks.Core.Tests.Match;

public sealed class MatchStateTankBoundsStatusTests
{
    [Theory]
    [InlineData(MatchStateTankBoundsStatus.SkippedDestroyed, 0)]
    [InlineData(MatchStateTankBoundsStatus.InsideBounds, 1)]
    [InlineData(MatchStateTankBoundsStatus.Clamped, 2)]
    public void Enum_members_have_explicit_stable_values(
        MatchStateTankBoundsStatus status,
        int expected)
    {
        Assert.Equal(expected, (int)status);
    }

    [Fact]
    public void Enum_contains_exactly_expected_members()
    {
        string[] names = Enum.GetNames<MatchStateTankBoundsStatus>();

        Assert.Equal(3, names.Length);

        Assert.Contains(nameof(MatchStateTankBoundsStatus.SkippedDestroyed), names);
        Assert.Contains(nameof(MatchStateTankBoundsStatus.InsideBounds), names);
        Assert.Contains(nameof(MatchStateTankBoundsStatus.Clamped), names);

        Assert.Equal(
            3,
            Enum.GetValues<MatchStateTankBoundsStatus>().Cast<int>().Distinct().Count());
    }
}
