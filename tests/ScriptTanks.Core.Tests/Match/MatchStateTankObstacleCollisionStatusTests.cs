using System;
using System.Linq;
using ScriptTanks.Core.Match;
using Xunit;

namespace ScriptTanks.Core.Tests.Match;

public sealed class MatchStateTankObstacleCollisionStatusTests
{
    [Theory]
    [InlineData(MatchStateTankObstacleCollisionStatus.Unchanged, 0)]
    [InlineData(MatchStateTankObstacleCollisionStatus.BlockedByObstacle, 1)]
    [InlineData(MatchStateTankObstacleCollisionStatus.SkippedDestroyed, 2)]
    [InlineData(MatchStateTankObstacleCollisionStatus.StartedInsideObstacle, 3)]
    public void Enum_members_have_explicit_stable_values(
        MatchStateTankObstacleCollisionStatus status,
        int expected)
    {
        Assert.Equal(expected, (int)status);
    }

    [Fact]
    public void Enum_contains_exactly_expected_members()
    {
        string[] names = Enum.GetNames<MatchStateTankObstacleCollisionStatus>();

        Assert.Equal(4, names.Length);

        Assert.Contains(nameof(MatchStateTankObstacleCollisionStatus.Unchanged), names);
        Assert.Contains(nameof(MatchStateTankObstacleCollisionStatus.BlockedByObstacle), names);
        Assert.Contains(nameof(MatchStateTankObstacleCollisionStatus.SkippedDestroyed), names);
        Assert.Contains(nameof(MatchStateTankObstacleCollisionStatus.StartedInsideObstacle), names);

        Assert.Equal(
            4,
            Enum.GetValues<MatchStateTankObstacleCollisionStatus>().Cast<int>().Distinct().Count());
    }
}
