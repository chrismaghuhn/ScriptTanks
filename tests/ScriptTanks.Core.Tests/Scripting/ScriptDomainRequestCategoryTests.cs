using System;
using System.Linq;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptDomainRequestCategoryTests
{
    [Theory]
    [InlineData(ScriptDomainRequestCategory.None, 0)]
    [InlineData(ScriptDomainRequestCategory.Sensor, 1)]
    [InlineData(ScriptDomainRequestCategory.Weapon, 2)]
    [InlineData(ScriptDomainRequestCategory.Turret, 3)]
    [InlineData(ScriptDomainRequestCategory.Movement, 4)]
    [InlineData(ScriptDomainRequestCategory.Unsupported, 5)]
    public void Enum_members_have_explicit_stable_values(
        ScriptDomainRequestCategory category,
        int expected)
    {
        Assert.Equal(expected, (int)category);
    }

    [Fact]
    public void Enum_contains_exactly_expected_members()
    {
        string[] names = Enum.GetNames<ScriptDomainRequestCategory>();

        Assert.Equal(6, names.Length);

        Assert.Contains(nameof(ScriptDomainRequestCategory.None), names);
        Assert.Contains(nameof(ScriptDomainRequestCategory.Sensor), names);
        Assert.Contains(nameof(ScriptDomainRequestCategory.Weapon), names);
        Assert.Contains(nameof(ScriptDomainRequestCategory.Turret), names);
        Assert.Contains(nameof(ScriptDomainRequestCategory.Movement), names);
        Assert.Contains(nameof(ScriptDomainRequestCategory.Unsupported), names);

        Assert.Equal(
            6,
            Enum.GetValues<ScriptDomainRequestCategory>().Cast<int>().Distinct().Count());
    }
}
