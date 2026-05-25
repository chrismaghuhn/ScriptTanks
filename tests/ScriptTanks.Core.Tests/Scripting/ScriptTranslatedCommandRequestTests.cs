using System;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptTranslatedCommandRequestTests
{
    [Fact]
    public void None_returns_kind_None()
    {
        ScriptTranslatedCommandRequest none = ScriptTranslatedCommandRequest.None();

        Assert.Equal(ScriptTranslatedCommandRequestKind.None, none.Kind);
    }

    [Fact]
    public void None_returns_RoutineIndex_minus_one()
    {
        ScriptTranslatedCommandRequest none = ScriptTranslatedCommandRequest.None();

        Assert.Equal(-1, none.RoutineIndex);
    }

    [Fact]
    public void None_returns_null_Command()
    {
        ScriptTranslatedCommandRequest none = ScriptTranslatedCommandRequest.None();

        Assert.Null(none.Command);
    }

    [Fact]
    public void None_returns_empty_Payload()
    {
        ScriptTranslatedCommandRequest none = ScriptTranslatedCommandRequest.None();

        Assert.Empty(none.Payload);
    }

    [Fact]
    public void None_HasRequest_false()
    {
        ScriptTranslatedCommandRequest none = ScriptTranslatedCommandRequest.None();

        Assert.False(none.HasRequest);
    }

    [Fact]
    public void Create_rejects_kind_None_with_ParamName_kind()
    {
        ScriptCommand command = CreateCommand();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.None,
                routineIndex: 0,
                command,
                payload: string.Empty));

        Assert.Equal("kind", ex.ParamName);
    }

    [Fact]
    public void Create_rejects_undefined_kind_with_ParamName_kind()
    {
        ScriptCommand command = CreateCommand();
        var undefinedKind = (ScriptTranslatedCommandRequestKind)99;

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ScriptTranslatedCommandRequest.Create(
                undefinedKind,
                routineIndex: 0,
                command,
                payload: string.Empty));

        Assert.Equal("kind", ex.ParamName);
    }

    [Fact]
    public void Create_rejects_negative_routineIndex_with_ParamName_routineIndex()
    {
        ScriptCommand command = CreateCommand();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.Fire,
                routineIndex: -1,
                command,
                payload: string.Empty));

        Assert.Equal("routineIndex", ex.ParamName);
    }

    [Fact]
    public void Create_rejects_null_payload_with_ParamName_payload()
    {
        ScriptCommand command = CreateCommand();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.Fire,
                routineIndex: 0,
                command,
                payload: null!));

        Assert.Equal("payload", ex.ParamName);
    }

    [Fact]
    public void Create_preserves_kind()
    {
        ScriptCommand command = CreateCommand();

        ScriptTranslatedCommandRequest request =
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.Fire,
                routineIndex: 2,
                command,
                payload: "primary");

        Assert.Equal(ScriptTranslatedCommandRequestKind.Fire, request.Kind);
    }

    [Fact]
    public void Create_preserves_routineIndex()
    {
        ScriptCommand command = CreateCommand();

        ScriptTranslatedCommandRequest request =
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.Fire,
                routineIndex: 2,
                command,
                payload: "primary");

        Assert.Equal(2, request.RoutineIndex);
    }

    [Fact]
    public void Create_preserves_command()
    {
        ScriptCommand command = CreateCommand();

        ScriptTranslatedCommandRequest request =
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.Fire,
                routineIndex: 2,
                command,
                payload: "primary");

        Assert.Equal(command, request.Command);
    }

    [Fact]
    public void Create_preserves_payload_verbatim()
    {
        ScriptCommand command = CreateCommand();

        ScriptTranslatedCommandRequest request =
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.Fire,
                routineIndex: 2,
                command,
                payload: "primary");

        Assert.Equal("primary", request.Payload);
    }

    [Fact]
    public void Create_HasRequest_true()
    {
        ScriptCommand command = CreateCommand();

        ScriptTranslatedCommandRequest request =
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.Fire,
                routineIndex: 2,
                command,
                payload: "primary");

        Assert.True(request.HasRequest);
    }

    [Fact]
    public void Create_allows_empty_and_whitespace_payload()
    {
        ScriptCommand command = CreateCommand();

        ScriptTranslatedCommandRequest emptyPayload =
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.Fire,
                routineIndex: 0,
                command,
                payload: string.Empty);

        ScriptTranslatedCommandRequest whitespacePayload =
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.Fire,
                routineIndex: 0,
                command,
                payload: "   ");

        Assert.Empty(emptyPayload.Payload);
        Assert.Equal("   ", whitespacePayload.Payload);
    }

    [Fact]
    public void Two_same_value_requests_are_not_reference_equal_and_same_reference_equals_itself()
    {
        ScriptCommand command = CreateCommand();

        ScriptTranslatedCommandRequest first =
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.Fire,
                2,
                command,
                "primary");

        ScriptTranslatedCommandRequest second =
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.Fire,
                2,
                command,
                "primary");

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
