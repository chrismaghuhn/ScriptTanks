using System;
using System.Linq;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptMappedFireRequestApplicationStatusTests
{
    [Theory]
    [InlineData(ScriptMappedFireRequestApplicationStatus.SkippedNotConstructed, 0)]
    [InlineData(ScriptMappedFireRequestApplicationStatus.Applied, 1)]
    [InlineData(ScriptMappedFireRequestApplicationStatus.FireRejected, 2)]
    public void Enum_members_have_explicit_stable_values(
        ScriptMappedFireRequestApplicationStatus status,
        int expected)
    {
        Assert.Equal(expected, (int)status);
    }

    [Fact]
    public void Enum_contains_exactly_expected_members()
    {
        string[] names = Enum.GetNames<ScriptMappedFireRequestApplicationStatus>();

        Assert.Equal(3, names.Length);

        Assert.Contains(nameof(ScriptMappedFireRequestApplicationStatus.SkippedNotConstructed), names);
        Assert.Contains(nameof(ScriptMappedFireRequestApplicationStatus.Applied), names);
        Assert.Contains(nameof(ScriptMappedFireRequestApplicationStatus.FireRejected), names);

        Assert.Equal(3, Enum.GetValues<ScriptMappedFireRequestApplicationStatus>().Cast<int>().Distinct().Count());
    }
}
