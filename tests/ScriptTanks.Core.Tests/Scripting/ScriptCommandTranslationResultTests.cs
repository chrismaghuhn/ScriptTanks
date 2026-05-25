using System;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptCommandTranslationResultTests
{
    [Fact]
    public void NoIntent_returns_NoIntent_status()
    {
        ScriptCommandTranslationResult result = ScriptCommandTranslationResult.NoIntent();

        Assert.Equal(ScriptCommandTranslationStatus.NoIntent, result.Status);
    }

    [Fact]
    public void NoIntent_returns_RoutineIndex_minus_one()
    {
        ScriptCommandTranslationResult result = ScriptCommandTranslationResult.NoIntent();

        Assert.Equal(-1, result.RoutineIndex);
    }

    [Fact]
    public void NoIntent_returns_null_Command()
    {
        ScriptCommandTranslationResult result = ScriptCommandTranslationResult.NoIntent();

        Assert.Null(result.Command);
    }

    [Fact]
    public void NoIntent_returns_empty_Message()
    {
        ScriptCommandTranslationResult result = ScriptCommandTranslationResult.NoIntent();

        Assert.Empty(result.Message);
    }

    [Fact]
    public void NoIntent_HasTranslatedRequest_false()
    {
        ScriptCommandTranslationResult result = ScriptCommandTranslationResult.NoIntent();

        Assert.False(result.HasTranslatedRequest);
    }

    [Fact]
    public void Translated_rejects_negative_routineIndex_with_ParamName_routineIndex()
    {
        ScriptCommand command = CreateCommand();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ScriptCommandTranslationResult.Translated(-1, command, "msg"));

        Assert.Equal("routineIndex", ex.ParamName);
    }

    [Fact]
    public void Translated_rejects_null_message_with_ParamName_message()
    {
        ScriptCommand command = CreateCommand();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptCommandTranslationResult.Translated(0, command, null!));

        Assert.Equal("message", ex.ParamName);
    }

    [Fact]
    public void Translated_preserves_routineIndex()
    {
        ScriptCommand command = CreateCommand();

        ScriptCommandTranslationResult result =
            ScriptCommandTranslationResult.Translated(2, command, "fire translated");

        Assert.Equal(2, result.RoutineIndex);
    }

    [Fact]
    public void Translated_preserves_command()
    {
        ScriptCommand command = CreateCommand();

        ScriptCommandTranslationResult result =
            ScriptCommandTranslationResult.Translated(2, command, "fire translated");

        Assert.Equal(command, result.Command);
    }

    [Fact]
    public void Translated_preserves_message_verbatim()
    {
        ScriptCommandTranslationResult result =
            ScriptCommandTranslationResult.Translated(
                1,
                CreateCommand(),
                "  ok  ");

        Assert.Equal("  ok  ", result.Message);
    }

    [Fact]
    public void Translated_HasTranslatedRequest_true()
    {
        ScriptCommand command = CreateCommand();

        ScriptCommandTranslationResult result =
            ScriptCommandTranslationResult.Translated(
                routineIndex: 2,
                command,
                "fire translated");

        Assert.Equal(ScriptCommandTranslationStatus.Translated, result.Status);
        Assert.True(result.HasTranslatedRequest);
    }

    [Fact]
    public void UnsupportedCommand_returns_status_and_HasTranslatedRequest_false()
    {
        ScriptCommand command = CreateCommand();

        ScriptCommandTranslationResult result =
            ScriptCommandTranslationResult.UnsupportedCommand(1, command, "x");

        Assert.Equal(ScriptCommandTranslationStatus.UnsupportedCommand, result.Status);
        Assert.False(result.HasTranslatedRequest);
    }

    [Fact]
    public void InvalidCommandArgument_returns_status_and_HasTranslatedRequest_false()
    {
        ScriptCommand command = CreateCommand();

        ScriptCommandTranslationResult result =
            ScriptCommandTranslationResult.InvalidCommandArgument(1, command, "x");

        Assert.Equal(ScriptCommandTranslationStatus.InvalidCommandArgument, result.Status);
        Assert.False(result.HasTranslatedRequest);
    }

    [Fact]
    public void MissingHardware_returns_status_and_HasTranslatedRequest_false()
    {
        ScriptCommand command = CreateCommand();

        ScriptCommandTranslationResult result =
            ScriptCommandTranslationResult.MissingHardware(1, command, "x");

        Assert.Equal(ScriptCommandTranslationStatus.MissingHardware, result.Status);
        Assert.False(result.HasTranslatedRequest);
    }

    [Fact]
    public void Factories_allow_empty_and_whitespace_messages()
    {
        ScriptCommand command = CreateCommand();

        _ = ScriptCommandTranslationResult.Translated(0, command, string.Empty);
        _ = ScriptCommandTranslationResult.Translated(0, command, "   ");

        _ = ScriptCommandTranslationResult.UnsupportedCommand(0, command, string.Empty);
        _ = ScriptCommandTranslationResult.UnsupportedCommand(0, command, "   ");

        _ = ScriptCommandTranslationResult.InvalidCommandArgument(0, command, string.Empty);
        _ = ScriptCommandTranslationResult.InvalidCommandArgument(0, command, "   ");

        _ = ScriptCommandTranslationResult.MissingHardware(0, command, string.Empty);
        _ = ScriptCommandTranslationResult.MissingHardware(0, command, "   ");
    }

    [Fact]
    public void Two_results_with_same_values_are_not_reference_equal()
    {
        ScriptCommand command = CreateCommand();

        ScriptCommandTranslationResult first =
            ScriptCommandTranslationResult.Translated(1, command, "ok");

        ScriptCommandTranslationResult second =
            ScriptCommandTranslationResult.Translated(1, command, "ok");

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
        Assert.True(first.Equals(first));
    }

    private static ScriptCommand CreateCommand(
        ScriptCommandType type = ScriptCommandType.Fire,
        string argument = "")
    {
        return new ScriptCommand(type, argument);
    }
}
