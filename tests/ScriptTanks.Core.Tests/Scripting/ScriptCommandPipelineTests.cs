using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptCommandPipelineTests
{
    [Fact]
    public void DecideAndTranslate_rejects_null_program_with_ParamName_program()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptCommandPipeline.DecideAndTranslate(
                null!,
                CreateContext()));

        Assert.Equal("program", ex.ParamName);
    }

    [Fact]
    public void DecideAndTranslate_no_matching_routine_maps_to_NoIntent_status()
    {
        ScriptProgram program = CreateProgram(
            CreateRoutine(
                "enemy",
                EnemyVisible(),
                Command(ScriptCommandType.Fire)),
            CreateRoutine(
                "weapon",
                WeaponReady(),
                Command(ScriptCommandType.ScanEnemy)));

        ScriptEvaluationContext context = CreateContext(
            enemyVisible: false,
            weaponReady: false);

        ScriptCommandTranslationResult result =
            ScriptCommandPipeline.DecideAndTranslate(
                program,
                context);

        Assert.Equal(
            ScriptCommandTranslationStatus.NoIntent,
            result.Status);
        Assert.Equal(-1, result.RoutineIndex);
        Assert.Null(result.Command);
        Assert.Empty(result.Message);
    }

    [Fact]
    public void DecideAndTranslate_no_matching_routine_HasTranslatedRequest_false()
    {
        ScriptProgram program = CreateProgram(
            CreateRoutine(
                "enemy",
                EnemyVisible(),
                Command(ScriptCommandType.Fire)),
            CreateRoutine(
                "weapon",
                WeaponReady(),
                Command(ScriptCommandType.ScanEnemy)));

        ScriptEvaluationContext context = CreateContext(
            enemyVisible: false,
            weaponReady: false);

        ScriptCommandTranslationResult result =
            ScriptCommandPipeline.DecideAndTranslate(
                program,
                context);

        Assert.False(result.HasTranslatedRequest);
    }

    [Fact]
    public void DecideAndTranslate_matching_NoOp_routine_maps_to_Translated()
    {
        ScriptRoutine routine = CreateRoutine(
            "noop",
            Always(),
            Command(ScriptCommandType.NoOp));

        ScriptProgram program = CreateProgram(routine);

        ScriptCommandTranslationResult result =
            ScriptCommandPipeline.DecideAndTranslate(
                program,
                CreateContext());

        Assert.Equal(
            ScriptCommandTranslationStatus.Translated,
            result.Status);
        Assert.True(result.HasTranslatedRequest);
    }

    [Fact]
    public void DecideAndTranslate_matching_ScanEnemy_routine_maps_to_Translated()
    {
        ScriptRoutine routine = CreateRoutine(
            "scan",
            Always(),
            Command(ScriptCommandType.ScanEnemy));

        ScriptProgram program = CreateProgram(routine);

        ScriptCommandTranslationResult result =
            ScriptCommandPipeline.DecideAndTranslate(
                program,
                CreateContext());

        Assert.Equal(
            ScriptCommandTranslationStatus.Translated,
            result.Status);
        Assert.True(result.HasTranslatedRequest);
    }

    [Fact]
    public void DecideAndTranslate_matching_Fire_routine_maps_to_Translated()
    {
        ScriptCommand fire = Command(ScriptCommandType.Fire, "primary");

        ScriptRoutine routine = CreateRoutine(
            "fire",
            Always(),
            fire);

        ScriptProgram program = CreateProgram(routine);

        ScriptCommandTranslationResult result =
            ScriptCommandPipeline.DecideAndTranslate(
                program,
                CreateContext());

        Assert.Equal(
            ScriptCommandTranslationStatus.Translated,
            result.Status);
        Assert.True(result.HasTranslatedRequest);
        Assert.Equal(0, result.RoutineIndex);
        Assert.Equal(fire, result.Command);
    }

    [Fact]
    public void DecideAndTranslate_selected_translation_preserves_routine_index()
    {
        ScriptRoutine third = CreateRoutine(
            "third",
            Always(),
            Command(ScriptCommandType.Fire));

        ScriptProgram program = CreateProgram(
            CreateRoutine("first", EnemyVisible(), Command(ScriptCommandType.NoOp)),
            CreateRoutine("second", WeaponReady(), Command(ScriptCommandType.NoOp)),
            third);

        ScriptEvaluationContext context = CreateContext(
            enemyVisible: false,
            weaponReady: false);

        ScriptCommandTranslationResult result =
            ScriptCommandPipeline.DecideAndTranslate(
                program,
                context);

        Assert.Equal(
            ScriptCommandTranslationStatus.Translated,
            result.Status);
        Assert.Equal(2, result.RoutineIndex);
    }

    [Fact]
    public void DecideAndTranslate_selected_translation_preserves_command()
    {
        ScriptCommand fire = Command(ScriptCommandType.Fire);

        ScriptRoutine expected = CreateRoutine("engage", Always(), fire);

        ScriptProgram program = CreateProgram(expected);

        ScriptCommandTranslationResult result =
            ScriptCommandPipeline.DecideAndTranslate(
                program,
                CreateContext());

        Assert.Equal(fire, result.Command);
    }

    [Fact]
    public void DecideAndTranslate_first_matching_routine_wins()
    {
        ScriptCommand scan = Command(ScriptCommandType.ScanEnemy);
        ScriptCommand fire = Command(ScriptCommandType.Fire);

        ScriptRoutine first = CreateRoutine(
            "scan",
            Always(),
            scan);

        ScriptRoutine second = CreateRoutine(
            "fire",
            Always(),
            fire);

        ScriptProgram program = CreateProgram(first, second);

        ScriptCommandTranslationResult result =
            ScriptCommandPipeline.DecideAndTranslate(
                program,
                CreateContext());

        Assert.Equal(
            ScriptCommandTranslationStatus.Translated,
            result.Status);
        Assert.Equal(0, result.RoutineIndex);
        Assert.Equal(scan, result.Command);
    }

    [Fact]
    public void DecideAndTranslate_invalid_condition_argument_propagates_with_ParamName_condition()
    {
        ScriptRoutine invalid = CreateRoutine(
            "invalid",
            MyHpBelow("abc"),
            Command(ScriptCommandType.Fire));

        ScriptProgram program = CreateProgram(invalid);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            ScriptCommandPipeline.DecideAndTranslate(
                program,
                CreateContext()));

        Assert.Equal("condition", ex.ParamName);
    }

    private static ScriptEvaluationContext CreateContext(
        bool enemyVisible = true,
        bool weaponReady = true,
        int myHitPoints = 50,
        Fixed? enemyDistance = null,
        bool sensorReady = true)
    {
        return new ScriptEvaluationContext(
            enemyVisible,
            weaponReady,
            myHitPoints,
            enemyDistance ?? Fixed.FromInt(10),
            sensorReady,
            ScriptingTestFixtures.DefaultNoTargetAimStatus);
    }

    private static ScriptRoutine CreateRoutine(
        string name,
        ScriptCondition condition,
        ScriptCommand command)
    {
        return new ScriptRoutine(
            name,
            condition,
            command);
    }

    private static ScriptCondition Always()
    {
        return ScriptCondition.Always();
    }

    private static ScriptCondition EnemyVisible()
    {
        return new ScriptCondition(
            ScriptConditionType.EnemyVisible,
            string.Empty);
    }

    private static ScriptCondition WeaponReady()
    {
        return new ScriptCondition(
            ScriptConditionType.WeaponReady,
            string.Empty);
    }

    private static ScriptCondition MyHpBelow(string threshold)
    {
        return new ScriptCondition(
            ScriptConditionType.MyHpBelow,
            threshold);
    }

    private static ScriptCommand Command(
        ScriptCommandType type,
        string argument = "")
    {
        return new ScriptCommand(type, argument);
    }

    private static ScriptProgram CreateProgram(
        params ScriptRoutine[] routines)
    {
        return new ScriptProgram(routines);
    }
}
