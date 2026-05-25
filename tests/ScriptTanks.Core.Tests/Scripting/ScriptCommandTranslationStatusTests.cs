using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptCommandTranslationStatusTests
{
    [Fact]
    public void Enum_values_are_explicit_and_stable()
    {
        Assert.Equal(0, (int)ScriptCommandTranslationStatus.NoIntent);
        Assert.Equal(1, (int)ScriptCommandTranslationStatus.Translated);
        Assert.Equal(2, (int)ScriptCommandTranslationStatus.UnsupportedCommand);
        Assert.Equal(3, (int)ScriptCommandTranslationStatus.InvalidCommandArgument);
        Assert.Equal(4, (int)ScriptCommandTranslationStatus.MissingHardware);
    }

    [Fact]
    public void Enum_contains_all_translation_statuses()
    {
        Assert.Equal(5, Enum.GetValues<ScriptCommandTranslationStatus>().Length);
    }
}
