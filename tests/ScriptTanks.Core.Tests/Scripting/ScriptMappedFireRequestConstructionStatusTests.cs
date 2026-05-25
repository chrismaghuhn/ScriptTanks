using System;
using System.Linq;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptMappedFireRequestConstructionStatusTests
{
    [Theory]
    [InlineData(ScriptMappedFireRequestConstructionStatus.NotWeapon, 0)]
    [InlineData(ScriptMappedFireRequestConstructionStatus.NoFireCommand, 1)]
    [InlineData(ScriptMappedFireRequestConstructionStatus.MissingWeaponSlot, 2)]
    [InlineData(ScriptMappedFireRequestConstructionStatus.WeaponNotReady, 3)]
    [InlineData(ScriptMappedFireRequestConstructionStatus.TurretNotAligned, 4)]
    [InlineData(ScriptMappedFireRequestConstructionStatus.MissingProjectileId, 5)]
    [InlineData(ScriptMappedFireRequestConstructionStatus.MissingMuzzleResolver, 6)]
    [InlineData(ScriptMappedFireRequestConstructionStatus.MissingVelocityResolver, 7)]
    [InlineData(ScriptMappedFireRequestConstructionStatus.Constructed, 8)]
    [InlineData(ScriptMappedFireRequestConstructionStatus.Unsupported, 9)]
    public void Enum_members_have_explicit_stable_values(
        ScriptMappedFireRequestConstructionStatus status,
        int expected)
    {
        Assert.Equal(expected, (int)status);
    }

    [Fact]
    public void Enum_contains_exactly_expected_members()
    {
        string[] names = Enum.GetNames<ScriptMappedFireRequestConstructionStatus>();

        Assert.Equal(10, names.Length);

        Assert.Contains(nameof(ScriptMappedFireRequestConstructionStatus.NotWeapon), names);
        Assert.Contains(nameof(ScriptMappedFireRequestConstructionStatus.NoFireCommand), names);
        Assert.Contains(nameof(ScriptMappedFireRequestConstructionStatus.MissingWeaponSlot), names);
        Assert.Contains(nameof(ScriptMappedFireRequestConstructionStatus.WeaponNotReady), names);
        Assert.Contains(nameof(ScriptMappedFireRequestConstructionStatus.TurretNotAligned), names);
        Assert.Contains(nameof(ScriptMappedFireRequestConstructionStatus.MissingProjectileId), names);
        Assert.Contains(nameof(ScriptMappedFireRequestConstructionStatus.MissingMuzzleResolver), names);
        Assert.Contains(nameof(ScriptMappedFireRequestConstructionStatus.MissingVelocityResolver), names);
        Assert.Contains(nameof(ScriptMappedFireRequestConstructionStatus.Constructed), names);
        Assert.Contains(nameof(ScriptMappedFireRequestConstructionStatus.Unsupported), names);

        Assert.Equal(10, Enum.GetValues<ScriptMappedFireRequestConstructionStatus>().Cast<int>().Distinct().Count());
    }
}
