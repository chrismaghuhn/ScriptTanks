using System;
using System.Collections.Generic;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptProgramTests
{
    private static ScriptRoutine CreateRoutine(
        string name,
        ScriptConditionType conditionType = ScriptConditionType.Always,
        ScriptCommandType commandType = ScriptCommandType.NoOp)
    {
        return new ScriptRoutine(
            name,
            new ScriptCondition(conditionType, string.Empty),
            new ScriptCommand(commandType, string.Empty));
    }

    [Fact]
    public void Constructor_PreservesRoutinesInOrder()
    {
        ScriptRoutine routineA = CreateRoutine("patrol");
        ScriptRoutine routineB = CreateRoutine(
            "engage",
            ScriptConditionType.EnemyVisible,
            ScriptCommandType.Fire);

        var program = new ScriptProgram(new[]
        {
            routineA,
            routineB,
        });

        Assert.Equal(routineA, program.Routines[0]);
        Assert.Equal(routineB, program.Routines[1]);
    }

    [Fact]
    public void Constructor_RejectsNullRoutines_WithParamNameRoutines()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new ScriptProgram(null!));

        Assert.Equal("routines", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsEmptyRoutines_WithParamNameRoutines()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new ScriptProgram(Array.Empty<ScriptRoutine>()));

        Assert.Equal("routines", ex.ParamName);
    }

    [Fact]
    public void Constructor_DefensivelyCopiesInputArray()
    {
        ScriptRoutine routineA = CreateRoutine("patrol");
        ScriptRoutine routineB = CreateRoutine("engage");
        ScriptRoutine replacement = CreateRoutine("retreat");

        ScriptRoutine[] routines = { routineA, routineB };

        var program = new ScriptProgram(routines);

        routines[1] = replacement;

        Assert.Equal(routineB, program.Routines[1]);
    }

    [Fact]
    public void RoutinesCollection_IsReadOnly()
    {
        var program = new ScriptProgram(new[] { CreateRoutine("only") });

        IList<ScriptRoutine> list =
            Assert.IsAssignableFrom<IList<ScriptRoutine>>(program.Routines);

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add(CreateRoutine("extra")));
    }

    [Fact]
    public void Count_ReturnsRoutineCount()
    {
        var program = new ScriptProgram(new[]
        {
            CreateRoutine("a"),
            CreateRoutine("b"),
        });

        Assert.Equal(2, program.Count);
    }

    [Fact]
    public void GetRoutineAtIndex_ReturnsRoutineAtIndexZero()
    {
        ScriptRoutine r0 = CreateRoutine("first");
        var program = new ScriptProgram(new[] { r0, CreateRoutine("second") });

        Assert.Equal(r0, program.GetRoutineAtIndex(0));
    }

    [Fact]
    public void GetRoutineAtIndex_ReturnsRoutineAtLaterIndex()
    {
        ScriptRoutine r1 = CreateRoutine("second");
        var program = new ScriptProgram(new[] { CreateRoutine("first"), r1 });

        Assert.Equal(r1, program.GetRoutineAtIndex(1));
    }

    [Fact]
    public void GetRoutineAtIndex_RejectsNegativeIndex_WithParamNameRoutineIndex()
    {
        var program = new ScriptProgram(new[] { CreateRoutine("x") });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => program.GetRoutineAtIndex(-1));

        Assert.Equal("routineIndex", ex.ParamName);
    }

    [Fact]
    public void GetRoutineAtIndex_RejectsPastEndIndex_WithParamNameRoutineIndex()
    {
        var program = new ScriptProgram(new[] { CreateRoutine("x") });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => program.GetRoutineAtIndex(1));

        Assert.Equal("routineIndex", ex.ParamName);
    }

    [Fact]
    public void TwoProgramsWithSameRoutines_UseReferenceEqualityNotValueEquality()
    {
        ScriptRoutine routine = CreateRoutine("patrol");

        var first = new ScriptProgram(new[] { routine });
        var second = new ScriptProgram(new[] { routine });

        Assert.False(ReferenceEquals(first, second));
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void SameProgramReference_EqualsItself()
    {
        var program = new ScriptProgram(new[] { CreateRoutine("patrol") });

        Assert.True(program.Equals(program));
    }
}
