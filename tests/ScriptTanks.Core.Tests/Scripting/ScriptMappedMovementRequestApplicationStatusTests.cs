using System;
using System.Linq;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptMappedMovementRequestApplicationStatusTests
{
    [Theory]
    [InlineData(ScriptMappedMovementRequestApplicationStatus.SkippedNotMovement, 0)]
    [InlineData(ScriptMappedMovementRequestApplicationStatus.Applied, 1)]
    [InlineData(ScriptMappedMovementRequestApplicationStatus.RejectedInvalidMovement, 2)]
    [InlineData(ScriptMappedMovementRequestApplicationStatus.TankIndexOutOfRange, 3)]
    [InlineData(ScriptMappedMovementRequestApplicationStatus.TankDestroyed, 4)]
    public void Enum_members_have_explicit_stable_values(
        ScriptMappedMovementRequestApplicationStatus status,
        int expected)
    {
        Assert.Equal(expected, (int)status);
    }

    [Fact]
    public void Enum_contains_exactly_expected_members()
    {
        string[] names = Enum.GetNames<ScriptMappedMovementRequestApplicationStatus>();

        Assert.Equal(5, names.Length);

        Assert.Contains(nameof(ScriptMappedMovementRequestApplicationStatus.SkippedNotMovement), names);
        Assert.Contains(nameof(ScriptMappedMovementRequestApplicationStatus.Applied), names);
        Assert.Contains(nameof(ScriptMappedMovementRequestApplicationStatus.RejectedInvalidMovement), names);
        Assert.Contains(nameof(ScriptMappedMovementRequestApplicationStatus.TankIndexOutOfRange), names);
        Assert.Contains(nameof(ScriptMappedMovementRequestApplicationStatus.TankDestroyed), names);

        Assert.Equal(
            5,
            Enum.GetValues<ScriptMappedMovementRequestApplicationStatus>().Cast<int>().Distinct().Count());
    }
}
