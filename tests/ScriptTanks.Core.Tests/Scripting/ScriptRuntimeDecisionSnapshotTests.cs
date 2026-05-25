using System;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptRuntimeDecisionSnapshotTests
{
    [Fact]
    public void Constructor_rejects_negative_tankIndex_with_ParamName_tankIndex()
    {
        ScriptRuntimeDecisionResult result = CreateResult();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScriptRuntimeDecisionSnapshot(
                new SimTick(42),
                tankIndex: -1,
                result));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_result_with_ParamName_result()
    {
        ScriptRuntimeDecisionResult? result = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptRuntimeDecisionSnapshot(
                new SimTick(42),
                tankIndex: 1,
                result!));

        Assert.Equal("result", ex.ParamName);
    }

    [Fact]
    public void Constructor_preserves_tick()
    {
        ScriptRuntimeDecisionResult result = CreateResult();

        var snapshot = new ScriptRuntimeDecisionSnapshot(
            new SimTick(42),
            tankIndex: 1,
            result);

        Assert.Equal(new SimTick(42), snapshot.Tick);
    }

    [Fact]
    public void Constructor_preserves_tankIndex()
    {
        ScriptRuntimeDecisionResult result = CreateResult();

        var snapshot = new ScriptRuntimeDecisionSnapshot(
            new SimTick(42),
            tankIndex: 1,
            result);

        Assert.Equal(1, snapshot.TankIndex);
    }

    [Fact]
    public void Constructor_preserves_result_reference()
    {
        ScriptRuntimeDecisionResult result = CreateResult();

        var snapshot = new ScriptRuntimeDecisionSnapshot(
            new SimTick(42),
            tankIndex: 1,
            result);

        Assert.Same(result, snapshot.Result);
    }

    [Fact]
    public void Constructor_allows_tankIndex_zero()
    {
        ScriptRuntimeDecisionResult result = CreateResult();

        var snapshot = new ScriptRuntimeDecisionSnapshot(
            new SimTick(0),
            tankIndex: 0,
            result);

        Assert.Equal(0, snapshot.TankIndex);
    }

    [Fact]
    public void Two_snapshots_with_same_values_are_not_reference_equal()
    {
        ScriptRuntimeDecisionResult result = CreateResult();

        var first = new ScriptRuntimeDecisionSnapshot(
            new SimTick(42),
            1,
            result);

        var second = new ScriptRuntimeDecisionSnapshot(
            new SimTick(42),
            1,
            result);

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Same_snapshot_reference_equals_itself()
    {
        ScriptRuntimeDecisionResult result = CreateResult();

        var first = new ScriptRuntimeDecisionSnapshot(
            new SimTick(42),
            1,
            result);

        Assert.True(first.Equals(first));
    }

    private static ScriptRuntimeDecisionResult CreateResult()
    {
        ScriptRoutineDecision decision = ScriptRoutineDecision.None();
        ScriptCommandIntent intent = ScriptCommandIntent.FromDecision(decision);
        ScriptCommandTranslationResult translation =
            ScriptCommandTranslator.Translate(intent);

        return new ScriptRuntimeDecisionResult(
            decision,
            intent,
            translation);
    }
}
