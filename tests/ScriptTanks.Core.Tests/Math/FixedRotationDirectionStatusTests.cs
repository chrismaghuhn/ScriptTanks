using System;
using System.Linq;
using ScriptTanks.Core.Math;
using Xunit;

namespace ScriptTanks.Core.Tests.Math;

public sealed class FixedRotationDirectionStatusTests
{
    [Theory]
    [InlineData(FixedRotationDirectionStatus.Resolved, 0)]
    [InlineData(FixedRotationDirectionStatus.UnsupportedRotationConvention, 1)]
    [InlineData(FixedRotationDirectionStatus.InvalidRotationValue, 2)]
    public void Enum_members_have_explicit_stable_values(
        FixedRotationDirectionStatus status,
        int expected)
    {
        Assert.Equal(expected, (int)status);
    }

    [Fact]
    public void Enum_contains_exactly_expected_members()
    {
        string[] names = Enum.GetNames<FixedRotationDirectionStatus>();

        Assert.Equal(3, names.Length);

        Assert.Contains(nameof(FixedRotationDirectionStatus.Resolved), names);
        Assert.Contains(
            nameof(FixedRotationDirectionStatus.UnsupportedRotationConvention),
            names);
        Assert.Contains(nameof(FixedRotationDirectionStatus.InvalidRotationValue), names);

        Assert.Equal(
            3,
            Enum.GetValues<FixedRotationDirectionStatus>().Cast<int>().Distinct().Count());
    }
}
