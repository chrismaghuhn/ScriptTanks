using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptRuntimeDecisionPipelineTests
{
    [Fact]
    public void Evaluate_rejects_null_program_with_ParamName_program()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptRuntimeDecisionPipeline.Evaluate(
                null!,
                CreateContext()));

        Assert.Equal("program", ex.ParamName);
    }

    [Fact]
    public void Evaluate_no_matching_routine_returns_result_with_no_command_decision()
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

        ScriptRuntimeDecisionResult result =
            ScriptRuntimeDecisionPipeline.Evaluate(
                program,
                context);

        Assert.False(result.Decision.HasCommand);
    }

    [Fact]
    public void Evaluate_no_matching_routine_returns_no_intent()
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

        ScriptRuntimeDecisionResult result =
            ScriptRuntimeDecisionPipeline.Evaluate(
                program,
                context);

        Assert.False(result.Intent.HasIntent);
    }

    [Fact]
    public void Evaluate_no_matching_routine_returns_NoIntent_translation()
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

        ScriptRuntimeDecisionResult result =
            ScriptRuntimeDecisionPipeline.Evaluate(
                program,
                context);

        Assert.Equal(
            ScriptCommandTranslationStatus.NoIntent,
            result.Translation.Status);
        Assert.False(result.Translation.HasTranslatedRequest);
    }

    [Fact]
    public void Evaluate_matching_routine_returns_command_decision()
    {
        ScriptCommand fire = Command(
            ScriptCommandType.Fire,
            "primary");

        ScriptRoutine routine = CreateRoutine(
            "fire",
            Always(),
            fire);

        ScriptProgram program = CreateProgram(routine);

        ScriptRuntimeDecisionResult result =
            ScriptRuntimeDecisionPipeline.Evaluate(
                program,
                CreateContext());

        Assert.True(result.Decision.HasCommand);
    }

    [Fact]
    public void Evaluate_matching_routine_returns_command_intent()
    {
        ScriptCommand fire = Command(
            ScriptCommandType.Fire,
            "primary");

        ScriptRoutine routine = CreateRoutine(
            "fire",
            Always(),
            fire);

        ScriptProgram program = CreateProgram(routine);

        ScriptRuntimeDecisionResult result =
            ScriptRuntimeDecisionPipeline.Evaluate(
                program,
                CreateContext());

        Assert.True(result.Intent.HasIntent);
    }

    [Fact]
    public void Evaluate_matching_routine_returns_Translated_translation_status()
    {
        ScriptCommand fire = Command(
            ScriptCommandType.Fire,
            "primary");

        ScriptRoutine routine = CreateRoutine(
            "fire",
            Always(),
            fire);

        ScriptProgram program = CreateProgram(routine);

        ScriptRuntimeDecisionResult result =
            ScriptRuntimeDecisionPipeline.Evaluate(
                program,
                CreateContext());

        Assert.Equal(
            ScriptCommandTranslationStatus.Translated,
            result.Translation.Status);
    }

    [Fact]
    public void Evaluate_matching_routine_preserves_routine_index_across_stages()
    {
        ScriptCommand fire = Command(
            ScriptCommandType.Fire,
            "primary");

        ScriptRoutine routine = CreateRoutine(
            "fire",
            Always(),
            fire);

        ScriptProgram program = CreateProgram(routine);

        ScriptRuntimeDecisionResult result =
            ScriptRuntimeDecisionPipeline.Evaluate(
                program,
                CreateContext());

        Assert.Equal(0, result.Decision.RoutineIndex);
        Assert.Equal(0, result.Intent.RoutineIndex);
        Assert.Equal(0, result.Translation.RoutineIndex);
    }

    [Fact]
    public void Evaluate_matching_routine_preserves_command_across_stages()
    {
        ScriptCommand fire = Command(
            ScriptCommandType.Fire,
            "primary");

        ScriptRoutine routine = CreateRoutine(
            "fire",
            Always(),
            fire);

        ScriptProgram program = CreateProgram(routine);

        ScriptRuntimeDecisionResult result =
            ScriptRuntimeDecisionPipeline.Evaluate(
                program,
                CreateContext());

        Assert.Equal(fire, result.Decision.Command);
        Assert.Equal(fire, result.Intent.Command);
        Assert.Equal(fire, result.Translation.Command);
    }

    [Fact]
    public void Evaluate_first_matching_routine_wins()
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

        ScriptRuntimeDecisionResult result =
            ScriptRuntimeDecisionPipeline.Evaluate(
                program,
                CreateContext());

        Assert.True(result.Decision.HasCommand);
        Assert.Equal(0, result.Decision.RoutineIndex);
        Assert.Equal(scan, result.Translation.Command);
    }

    [Fact]
    public void Evaluate_invalid_condition_argument_propagates_with_ParamName_condition()
    {
        ScriptRoutine invalid = CreateRoutine(
            "invalid",
            MyHpBelow("abc"),
            Command(ScriptCommandType.Fire));

        ScriptProgram program = CreateProgram(invalid);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            ScriptRuntimeDecisionPipeline.Evaluate(
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
