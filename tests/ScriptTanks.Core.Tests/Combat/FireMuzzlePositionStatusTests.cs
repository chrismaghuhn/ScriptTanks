using System;
using System.Linq;
using ScriptTanks.Core.Combat;
using Xunit;

namespace ScriptTanks.Core.Tests.Combat;

public sealed class FireMuzzlePositionStatusTests
{
    [Theory]
    [InlineData(FireMuzzlePositionStatus.Resolved, 0)]
    [InlineData(FireMuzzlePositionStatus.TankIndexOutOfRange, 1)]
    [InlineData(FireMuzzlePositionStatus.TankDestroyed, 2)]
    [InlineData(FireMuzzlePositionStatus.WeaponSlotMissing, 3)]
    [InlineData(FireMuzzlePositionStatus.MissingAimDirection, 4)]
    [InlineData(FireMuzzlePositionStatus.MissingWeaponGeometry, 5)]
    public void Enum_members_have_explicit_stable_values(
        FireMuzzlePositionStatus status,
        int expected)
    {
        Assert.Equal(expected, (int)status);
    }

    [Fact]
    public void Enum_contains_exactly_expected_members()
    {
        string[] names = Enum.GetNames<FireMuzzlePositionStatus>();

        Assert.Equal(6, names.Length);

        Assert.Contains(nameof(FireMuzzlePositionStatus.Resolved), names);
        Assert.Contains(nameof(FireMuzzlePositionStatus.TankIndexOutOfRange), names);
        Assert.Contains(nameof(FireMuzzlePositionStatus.TankDestroyed), names);
        Assert.Contains(nameof(FireMuzzlePositionStatus.WeaponSlotMissing), names);
        Assert.Contains(nameof(FireMuzzlePositionStatus.MissingAimDirection), names);
        Assert.Contains(nameof(FireMuzzlePositionStatus.MissingWeaponGeometry), names);

        Assert.Equal(6, Enum.GetValues<FireMuzzlePositionStatus>().Cast<int>().Distinct().Count());
    }
}
