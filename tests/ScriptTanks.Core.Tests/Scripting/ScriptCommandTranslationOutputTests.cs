using System;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptCommandTranslationOutputTests
{
    [Fact]
    public void Constructor_rejects_null_result()
    {
        ScriptTranslatedCommandRequest request = ScriptTranslatedCommandRequest.None();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptCommandTranslationOutput(null!, request));

        Assert.Equal("result", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_request()
    {
        ScriptCommandTranslationResult result = ScriptCommandTranslationResult.NoIntent();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptCommandTranslationOutput(result, null!));

        Assert.Equal("request", ex.ParamName);
    }

    [Fact]
    public void Constructor_accepts_Translated_with_non_none_request()
    {
        ScriptCommand command = CreateCommand();
        ScriptCommandTranslationResult result =
            ScriptCommandTranslationResult.Translated(
                routineIndex: 0,
                command,
                message: "ok");

        ScriptTranslatedCommandRequest request =
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.Fire,
                routineIndex: 0,
                command,
                payload: "default");

        var output = new ScriptCommandTranslationOutput(result, request);

        Assert.Same(result, output.Result);
        Assert.Same(request, output.Request);
    }

    [Fact]
    public void Constructor_accepts_NoIntent_with_None_request()
    {
        ScriptCommandTranslationResult result = ScriptCommandTranslationResult.NoIntent();
        ScriptTranslatedCommandRequest request = ScriptTranslatedCommandRequest.None();

        var output = new ScriptCommandTranslationOutput(result, request);

        Assert.Same(result, output.Result);
        Assert.Same(request, output.Request);
    }

    [Fact]
    public void Constructor_accepts_UnsupportedCommand_with_None_request()
    {
        ScriptCommand command = CreateCommand();
        ScriptCommandTranslationResult result =
            ScriptCommandTranslationResult.UnsupportedCommand(
                routineIndex: 1,
                command,
                message: "unsupported");

        ScriptTranslatedCommandRequest request = ScriptTranslatedCommandRequest.None();

        var output = new ScriptCommandTranslationOutput(result, request);

        Assert.Same(result, output.Result);
        Assert.Same(request, output.Request);
    }

    [Fact]
    public void Constructor_accepts_InvalidCommandArgument_with_None_request()
    {
        ScriptCommand command = CreateCommand();
        ScriptCommandTranslationResult result =
            ScriptCommandTranslationResult.InvalidCommandArgument(
                routineIndex: 1,
                command,
                message: "bad arg");

        ScriptTranslatedCommandRequest request = ScriptTranslatedCommandRequest.None();

        var output = new ScriptCommandTranslationOutput(result, request);

        Assert.Same(result, output.Result);
        Assert.Same(request, output.Request);
    }

    [Fact]
    public void Constructor_accepts_MissingHardware_with_None_request()
    {
        ScriptCommand command = CreateCommand();
        ScriptCommandTranslationResult result =
            ScriptCommandTranslationResult.MissingHardware(
                routineIndex: 1,
                command,
                message: "missing");

        ScriptTranslatedCommandRequest request = ScriptTranslatedCommandRequest.None();

        var output = new ScriptCommandTranslationOutput(result, request);

        Assert.Same(result, output.Result);
        Assert.Same(request, output.Request);
    }

    [Fact]
    public void Constructor_rejects_Translated_with_None_request()
    {
        ScriptCommand command = CreateCommand();
        ScriptCommandTranslationResult result =
            ScriptCommandTranslationResult.Translated(
                routineIndex: 0,
                command,
                message: "ok");

        ScriptTranslatedCommandRequest request = ScriptTranslatedCommandRequest.None();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptCommandTranslationOutput(result, request));

        Assert.Equal("request", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_NoIntent_with_non_none_request()
    {
        ScriptCommand command = CreateCommand();
        ScriptCommandTranslationResult result = ScriptCommandTranslationResult.NoIntent();

        ScriptTranslatedCommandRequest request =
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.Fire,
                routineIndex: 0,
                command,
                payload: "x");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptCommandTranslationOutput(result, request));

        Assert.Equal("request", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_UnsupportedCommand_with_non_none_request()
    {
        ScriptCommand command = CreateCommand();
        ScriptCommandTranslationResult result =
            ScriptCommandTranslationResult.UnsupportedCommand(
                routineIndex: 0,
                command,
                message: "no");

        ScriptTranslatedCommandRequest request =
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.Fire,
                routineIndex: 0,
                command,
                payload: "x");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptCommandTranslationOutput(result, request));

        Assert.Equal("request", ex.ParamName);
    }

    private static ScriptCommand CreateCommand(
        ScriptCommandType type = ScriptCommandType.Fire,
        string argument = "")
    {
        return new ScriptCommand(type, argument);
    }
}
