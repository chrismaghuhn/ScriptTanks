using System;
using System.Linq;
using ScriptTanks.Core.Combat;
using Xunit;

namespace ScriptTanks.Core.Tests.Combat;

public sealed class FireVelocityStatusTests
{
    [Theory]
    [InlineData(FireVelocityStatus.Resolved, 0)]
    [InlineData(FireVelocityStatus.TankIndexOutOfRange, 1)]
    [InlineData(FireVelocityStatus.TankDestroyed, 2)]
    [InlineData(FireVelocityStatus.WeaponSlotMissing, 3)]
    [InlineData(FireVelocityStatus.MissingAimDirection, 4)]
    [InlineData(FireVelocityStatus.MissingWeaponSpeed, 5)]
    public void Enum_members_have_explicit_stable_values(
        FireVelocityStatus status,
        int expected)
    {
        Assert.Equal(expected, (int)status);
    }

    [Fact]
    public void Enum_contains_exactly_expected_members()
    {
        string[] names = Enum.GetNames<FireVelocityStatus>();

        Assert.Equal(6, names.Length);

        Assert.Contains(nameof(FireVelocityStatus.Resolved), names);
        Assert.Contains(nameof(FireVelocityStatus.TankIndexOutOfRange), names);
        Assert.Contains(nameof(FireVelocityStatus.TankDestroyed), names);
        Assert.Contains(nameof(FireVelocityStatus.WeaponSlotMissing), names);
        Assert.Contains(nameof(FireVelocityStatus.MissingAimDirection), names);
        Assert.Contains(nameof(FireVelocityStatus.MissingWeaponSpeed), names);

        Assert.Equal(6, Enum.GetValues<FireVelocityStatus>().Cast<int>().Distinct().Count());
    }
}
