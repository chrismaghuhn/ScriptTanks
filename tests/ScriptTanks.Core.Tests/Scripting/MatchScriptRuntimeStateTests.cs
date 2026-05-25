using System;
using System.Collections.Generic;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class MatchScriptRuntimeStateTests
{
    [Fact]
    public void Constructor_rejects_null_requests_with_ParamName_requests()
    {
        IEnumerable<ScriptRuntimeEvaluationRequest>? requests = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchScriptRuntimeState(requests!));

        Assert.Equal("requests", ex.ParamName);
    }

    [Fact]
    public void Constructor_allows_empty_requests()
    {
        var state = new MatchScriptRuntimeState(
            Array.Empty<ScriptRuntimeEvaluationRequest>());

        Assert.Empty(state.Requests);
        Assert.Equal(0, state.Count);
    }

    [Fact]
    public void Constructor_rejects_null_request_element_with_ParamName_requests()
    {
        ScriptRuntimeEvaluationRequest first = CreateRequest(0);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchScriptRuntimeState(
                new ScriptRuntimeEvaluationRequest?[] { first, null }!));

        Assert.Equal("requests", ex.ParamName);
    }

    [Fact]
    public void Constructor_preserves_requests_in_order()
    {
        ScriptRuntimeEvaluationRequest a = CreateRequest(0);
        ScriptRuntimeEvaluationRequest b = CreateRequest(1);
        ScriptRuntimeEvaluationRequest c = CreateRequest(2);

        var state = new MatchScriptRuntimeState(
            new[] { a, b, c });

        Assert.Same(a, state.Requests[0]);
        Assert.Same(b, state.Requests[1]);
        Assert.Same(c, state.Requests[2]);
    }

    [Fact]
    public void Constructor_defensively_copies_input_array()
    {
        ScriptRuntimeEvaluationRequest first = CreateRequest(0);
        ScriptRuntimeEvaluationRequest second = CreateRequest(1);
        ScriptRuntimeEvaluationRequest replacement = CreateRequest(2);

        ScriptRuntimeEvaluationRequest[] requests = { first, second };

        MatchScriptRuntimeState state = new MatchScriptRuntimeState(requests);

        requests[1] = replacement;

        Assert.Same(second, state.Requests[1]);
    }

    [Fact]
    public void Requests_collection_is_read_only()
    {
        MatchScriptRuntimeState state = new MatchScriptRuntimeState(
            new[] { CreateRequest(0) });

        IList<ScriptRuntimeEvaluationRequest> list =
            Assert.IsAssignableFrom<IList<ScriptRuntimeEvaluationRequest>>(
                state.Requests);

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add(CreateRequest(1)));
    }

    [Fact]
    public void Count_returns_request_count()
    {
        MatchScriptRuntimeState state = new MatchScriptRuntimeState(
            new[]
            {
                CreateRequest(0),
                CreateRequest(1),
            });

        Assert.Equal(2, state.Count);
    }

    [Fact]
    public void GetRequestAtIndex_returns_request_at_index_zero()
    {
        ScriptRuntimeEvaluationRequest first = CreateRequest(0);
        ScriptRuntimeEvaluationRequest second = CreateRequest(1);

        MatchScriptRuntimeState state = new MatchScriptRuntimeState(
            new[] { first, second });

        Assert.Same(first, state.GetRequestAtIndex(0));
    }

    [Fact]
    public void GetRequestAtIndex_returns_request_at_later_index()
    {
        ScriptRuntimeEvaluationRequest first = CreateRequest(0);
        ScriptRuntimeEvaluationRequest second = CreateRequest(1);

        MatchScriptRuntimeState state = new MatchScriptRuntimeState(
            new[] { first, second });

        Assert.Same(second, state.GetRequestAtIndex(1));
    }

    [Fact]
    public void GetRequestAtIndex_rejects_negative_index_with_ParamName_requestIndex()
    {
        MatchScriptRuntimeState state = new MatchScriptRuntimeState(
            new[] { CreateRequest(0) });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            state.GetRequestAtIndex(-1));

        Assert.Equal("requestIndex", ex.ParamName);
    }

    [Fact]
    public void GetRequestAtIndex_rejects_past_end_index_with_ParamName_requestIndex()
    {
        MatchScriptRuntimeState state = new MatchScriptRuntimeState(
            new[] { CreateRequest(0) });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            state.GetRequestAtIndex(1));

        Assert.Equal("requestIndex", ex.ParamName);
    }

    [Fact]
    public void WithRequestAtIndex_rejects_null_replacement_with_ParamName_request()
    {
        MatchScriptRuntimeState state = new MatchScriptRuntimeState(
            new[] { CreateRequest(0) });

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            state.WithRequestAtIndex(0, null!));

        Assert.Equal("request", ex.ParamName);
    }

    [Fact]
    public void WithRequestAtIndex_rejects_invalid_index_with_ParamName_requestIndex()
    {
        MatchScriptRuntimeState state = new MatchScriptRuntimeState(
            new[] { CreateRequest(0) });

        ScriptRuntimeEvaluationRequest replacement = CreateRequest(1);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            state.WithRequestAtIndex(1, replacement));

        Assert.Equal("requestIndex", ex.ParamName);
    }

    [Fact]
    public void WithRequestAtIndex_replaces_selected_request_and_preserves_other_requests()
    {
        ScriptRuntimeEvaluationRequest first = CreateRequest(0);
        ScriptRuntimeEvaluationRequest second = CreateRequest(1);
        ScriptRuntimeEvaluationRequest replacement = CreateRequest(
            1,
            ScriptCommandType.ScanEnemy);

        MatchScriptRuntimeState state = new MatchScriptRuntimeState(
            new[] { first, second });

        MatchScriptRuntimeState updated =
            state.WithRequestAtIndex(1, replacement);

        Assert.Same(first, updated.GetRequestAtIndex(0));
        Assert.Same(replacement, updated.GetRequestAtIndex(1));
    }

    [Fact]
    public void WithRequestAtIndex_does_not_mutate_original_state()
    {
        ScriptRuntimeEvaluationRequest first = CreateRequest(0);
        ScriptRuntimeEvaluationRequest second = CreateRequest(1);
        ScriptRuntimeEvaluationRequest replacement = CreateRequest(
            1,
            ScriptCommandType.ScanEnemy);

        MatchScriptRuntimeState state = new MatchScriptRuntimeState(
            new[] { first, second });

        _ = state.WithRequestAtIndex(1, replacement);

        Assert.Same(second, state.GetRequestAtIndex(1));
    }

    private static ScriptProgram CreateProgram(
        ScriptCommandType commandType = ScriptCommandType.Fire)
    {
        ScriptRoutine routine = new ScriptRoutine(
            "routine",
            ScriptCondition.Always(),
            new ScriptCommand(
                commandType,
                string.Empty));

        return new ScriptProgram(new[] { routine });
    }

    private static ScriptEvaluationContext CreateContext(
        int myHitPoints = 75)
    {
        return new ScriptEvaluationContext(
            enemyVisible: true,
            weaponReady: true,
            myHitPoints,
            enemyDistance: Fixed.FromInt(12),
            sensorReady: true,
            turretAimStatus: ScriptingTestFixtures.DefaultNoTargetAimStatus);
    }

    private static ScriptRuntimeEvaluationRequest CreateRequest(
        int tankIndex,
        ScriptCommandType commandType = ScriptCommandType.Fire,
        int myHitPoints = 75)
    {
        return new ScriptRuntimeEvaluationRequest(
            tankIndex,
            CreateProgram(commandType),
            CreateContext(myHitPoints));
    }
}
