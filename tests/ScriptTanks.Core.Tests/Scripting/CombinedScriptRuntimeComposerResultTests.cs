using System;
using System.Collections.Generic;
using System.Reflection;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class CombinedScriptRuntimeComposerResultTests
{
    private sealed class ValidOptionAChain
    {
        public MatchSensorRuntimeState Runtime { get; init; } = null!;

        public MatchScriptIntentIntegrationResult Integration { get; init; } = null!;

        public MatchScriptDomainRequestMappingResult Mapping { get; init; } = null!;

        public ScriptMappedSensorRequestApplicationResult Sensor { get; init; } = null!;

        public ScriptMappedTurretRequestApplicationResult Turret { get; init; } = null!;

        public ScriptMappedFireRequestConstructionPipelineResult Construction { get; init; } = null!;

        public ScriptMappedFireRequestApplicationResult Fire { get; init; } = null!;

        public ScriptMappedMovementRequestApplicationResult Movement { get; init; } = null!;

        public MatchSensorRuntimeState FinalRuntime { get; init; } = null!;

        public ProjectileIdSequence FinalSequence { get; init; }
    }

    private static TankState CreateTank(int id, int ownerSlot)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(FixedVec2.FromInts(10 + id, 20), FixedVec2.Zero),
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation: Fixed.Zero);
    }

    private static TankWeaponLoadout CreateWeaponLoadout(params WeaponState[] weapons)
    {
        return new TankWeaponLoadout(weapons);
    }

    private static MatchState CreateMatchState(
        SimTick tick,
        TankWeaponLoadout[] weaponLoadouts,
        params TankState[] tanks)
    {
        return new MatchState(
            ArenaCatalog.OpenTestArena,
            tick,
            tanks,
            weaponLoadouts,
            Array.Empty<ProjectileState>());
    }

    private static TankSensorLoadout CreateSensorLoadout(
        SensorState first,
        SensorState? second = null)
    {
        return second.HasValue
            ? new TankSensorLoadout(new[] { first, second.Value })
            : new TankSensorLoadout(new[] { first });
    }

    private static MatchSensorRuntimeState CreateRuntime(
        MatchState state,
        params TankSensorLoadout[] sensorLoadouts)
    {
        return new MatchSensorRuntimeState(
            state,
            new MatchSensorLoadoutState(sensorLoadouts));
    }

    private static ScriptProgram ScanWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "scan",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.ScanEnemy, "default")),
            });
    }

    private static ScriptProgram FireWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "fire",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.Fire, argument: string.Empty)),
            });
    }

    private static ScriptProgram MoveToPatrolPointWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "patrol",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.MoveToPatrolPoint, "next")),
            });
    }

    private static MatchSensorRuntimeState CreateSingleTankRuntime()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static MatchSensorRuntimeState CreateTwoTankRuntime()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0),
            CreateTank(1, 1));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static ValidOptionAChain BuildValidOptionAChain(
        MatchSensorRuntimeState runtime,
        ProjectileIdSequence sequence,
        params ScriptProgram[] programs)
    {
        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(runtime, programs);

        MatchScriptDomainRequestMappingResult mapping =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        ScriptMappedSensorRequestApplicationResult sensor =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtime, mapping);

        ScriptMappedTurretRequestApplicationResult turret =
            ScriptMappedTurretRequestApplicationPipeline.Apply(runtime, mapping);

        ScriptMappedFireRequestConstructionPipelineResult construction =
            ScriptMappedFireRequestConstructionPipeline.Construct(
                turret.FinalRuntime,
                mapping,
                sequence);

        ScriptMappedFireRequestApplicationResult fire =
            ScriptMappedFireRequestApplicationPipeline.Apply(turret.FinalRuntime, construction);

        ScriptMappedMovementRequestApplicationResult movement =
            ScriptMappedMovementRequestApplicationPipeline.Apply(fire.FinalRuntime, mapping);

        MatchSensorRuntimeState finalRuntime = movement.FinalRuntime.WithSensorLoadouts(
            sensor.FinalRuntime.SensorLoadouts);

        return new ValidOptionAChain
        {
            Runtime = runtime,
            Integration = integration,
            Mapping = mapping,
            Sensor = sensor,
            Turret = turret,
            Construction = construction,
            Fire = fire,
            Movement = movement,
            FinalRuntime = finalRuntime,
            FinalSequence = construction.FinalProjectileIdSequence,
        };
    }

    private static CombinedScriptRuntimeComposerResult CreateResult(ValidOptionAChain chain)
    {
        return new CombinedScriptRuntimeComposerResult(
            chain.Runtime,
            chain.Integration,
            chain.Mapping,
            chain.Sensor,
            chain.Turret,
            chain.Construction,
            chain.Fire,
            chain.Movement,
            chain.FinalRuntime,
            chain.FinalSequence);
    }

    [Fact]
    public void Constructor_preserves_all_references_and_values()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();
        ValidOptionAChain chain = BuildValidOptionAChain(
            runtime,
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        CombinedScriptRuntimeComposerResult result = CreateResult(chain);

        Assert.Same(chain.Runtime, result.InitialRuntime);
        Assert.Same(chain.Integration, result.IntegrationResult);
        Assert.Same(chain.Mapping, result.MappingResult);
        Assert.Same(chain.Sensor, result.SensorApplicationResult);
        Assert.Same(chain.Turret, result.TurretApplicationResult);
        Assert.Same(chain.Construction, result.FireConstructionPipelineResult);
        Assert.Same(chain.Fire, result.FireApplicationResult);
        Assert.Same(chain.Movement, result.MovementApplicationResult);
        Assert.Same(chain.FinalRuntime, result.FinalRuntime);
        Assert.Same(chain.Runtime, result.IntegrationResult.Runtime);
        Assert.Same(chain.Movement.FinalRuntime.State, result.FinalRuntime.State);
        Assert.Same(
            chain.Sensor.FinalRuntime.SensorLoadouts,
            result.FinalRuntime.SensorLoadouts);
        Assert.Equal(chain.FinalSequence, result.FinalProjectileIdSequence);
        Assert.Equal(
            chain.Construction.FinalProjectileIdSequence.NextValue,
            result.FinalProjectileIdSequence.NextValue);
    }

    [Fact]
    public void Constructor_NullInitialRuntime_Throws_ParamName_initialRuntime()
    {
        ValidOptionAChain chain = BuildValidOptionAChain(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new CombinedScriptRuntimeComposerResult(
                null!,
                chain.Integration,
                chain.Mapping,
                chain.Sensor,
                chain.Turret,
                chain.Construction,
                chain.Fire,
                chain.Movement,
                chain.FinalRuntime,
                chain.FinalSequence));

        Assert.Equal("initialRuntime", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullIntegrationResult_Throws_ParamName_integrationResult()
    {
        ValidOptionAChain chain = BuildValidOptionAChain(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new CombinedScriptRuntimeComposerResult(
                chain.Runtime,
                null!,
                chain.Mapping,
                chain.Sensor,
                chain.Turret,
                chain.Construction,
                chain.Fire,
                chain.Movement,
                chain.FinalRuntime,
                chain.FinalSequence));

        Assert.Equal("integrationResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullMappingResult_Throws_ParamName_mappingResult()
    {
        ValidOptionAChain chain = BuildValidOptionAChain(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new CombinedScriptRuntimeComposerResult(
                chain.Runtime,
                chain.Integration,
                null!,
                chain.Sensor,
                chain.Turret,
                chain.Construction,
                chain.Fire,
                chain.Movement,
                chain.FinalRuntime,
                chain.FinalSequence));

        Assert.Equal("mappingResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullSensorApplicationResult_Throws_ParamName_sensorApplicationResult()
    {
        ValidOptionAChain chain = BuildValidOptionAChain(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new CombinedScriptRuntimeComposerResult(
                chain.Runtime,
                chain.Integration,
                chain.Mapping,
                null!,
                chain.Turret,
                chain.Construction,
                chain.Fire,
                chain.Movement,
                chain.FinalRuntime,
                chain.FinalSequence));

        Assert.Equal("sensorApplicationResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullTurretApplicationResult_Throws_ParamName_turretApplicationResult()
    {
        ValidOptionAChain chain = BuildValidOptionAChain(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new CombinedScriptRuntimeComposerResult(
                chain.Runtime,
                chain.Integration,
                chain.Mapping,
                chain.Sensor,
                null!,
                chain.Construction,
                chain.Fire,
                chain.Movement,
                chain.FinalRuntime,
                chain.FinalSequence));

        Assert.Equal("turretApplicationResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullFireConstructionPipelineResult_Throws_ParamName_fireConstructionPipelineResult()
    {
        ValidOptionAChain chain = BuildValidOptionAChain(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new CombinedScriptRuntimeComposerResult(
                chain.Runtime,
                chain.Integration,
                chain.Mapping,
                chain.Sensor,
                chain.Turret,
                null!,
                chain.Fire,
                chain.Movement,
                chain.FinalRuntime,
                chain.FinalSequence));

        Assert.Equal("fireConstructionPipelineResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullFireApplicationResult_Throws_ParamName_fireApplicationResult()
    {
        ValidOptionAChain chain = BuildValidOptionAChain(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new CombinedScriptRuntimeComposerResult(
                chain.Runtime,
                chain.Integration,
                chain.Mapping,
                chain.Sensor,
                chain.Turret,
                chain.Construction,
                null!,
                chain.Movement,
                chain.FinalRuntime,
                chain.FinalSequence));

        Assert.Equal("fireApplicationResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_MismatchedMovementApplicationMapping_Throws_ParamName_movementApplicationResult()
    {
        MatchSensorRuntimeState runtimeA = CreateTwoTankRuntime();
        MatchSensorRuntimeState runtimeB = CreateTwoTankRuntime();

        ValidOptionAChain chainA = BuildValidOptionAChain(
            runtimeA,
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        MatchScriptDomainRequestMappingResult mappingB =
            ScriptTranslatedCommandDomainMapper.MapAll(
                MatchScriptIntentIntegrationComposer.EvaluateIntents(
                    runtimeB,
                    new List<ScriptProgram> { ScanWhenAlways(), FireWhenAlways() }));

        ScriptMappedMovementRequestApplicationResult movementB =
            ScriptMappedMovementRequestApplicationPipeline.Apply(
                chainA.Fire.FinalRuntime,
                mappingB);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedScriptRuntimeComposerResult(
                chainA.Runtime,
                chainA.Integration,
                chainA.Mapping,
                chainA.Sensor,
                chainA.Turret,
                chainA.Construction,
                chainA.Fire,
                movementB,
                chainA.FinalRuntime,
                chainA.FinalSequence));

        Assert.Equal("movementApplicationResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_preserves_movement_application_result()
    {
        ValidOptionAChain chain = BuildValidOptionAChain(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        CombinedScriptRuntimeComposerResult result = CreateResult(chain);

        Assert.Same(chain.Movement, result.MovementApplicationResult);
        Assert.Same(chain.Mapping, result.MovementApplicationResult.MappingResult);
    }

    [Fact]
    public void Constructor_NullFinalRuntime_Throws_ParamName_finalRuntime()
    {
        ValidOptionAChain chain = BuildValidOptionAChain(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new CombinedScriptRuntimeComposerResult(
                chain.Runtime,
                chain.Integration,
                chain.Mapping,
                chain.Sensor,
                chain.Turret,
                chain.Construction,
                chain.Fire,
                chain.Movement,
                null!,
                chain.FinalSequence));

        Assert.Equal("finalRuntime", ex.ParamName);
    }

    [Fact]
    public void Constructor_MismatchedInitialRuntime_Throws_ParamName_initialRuntime()
    {
        MatchSensorRuntimeState runtimeA = CreateTwoTankRuntime();
        ValidOptionAChain chain = BuildValidOptionAChain(
            runtimeA,
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        MatchSensorRuntimeState runtimeB = CreateTwoTankRuntime();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedScriptRuntimeComposerResult(
                runtimeB,
                chain.Integration,
                chain.Mapping,
                chain.Sensor,
                chain.Turret,
                chain.Construction,
                chain.Fire,
                chain.Movement,
                chain.FinalRuntime,
                chain.FinalSequence));

        Assert.Equal("initialRuntime", ex.ParamName);
    }

    [Fact]
    public void Constructor_MismatchedMappingIntegration_Throws_ParamName_mappingResult()
    {
        MatchSensorRuntimeState runtimeA = CreateTwoTankRuntime();
        MatchSensorRuntimeState runtimeB = CreateTwoTankRuntime();

        ValidOptionAChain chainA = BuildValidOptionAChain(
            runtimeA,
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        MatchScriptIntentIntegrationResult integrationB =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtimeB,
                new List<ScriptProgram> { ScanWhenAlways(), FireWhenAlways() });

        MatchScriptDomainRequestMappingResult mappingB =
            ScriptTranslatedCommandDomainMapper.MapAll(integrationB);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedScriptRuntimeComposerResult(
                chainA.Runtime,
                chainA.Integration,
                mappingB,
                chainA.Sensor,
                chainA.Turret,
                chainA.Construction,
                chainA.Fire,
                chainA.Movement,
                chainA.FinalRuntime,
                chainA.FinalSequence));

        Assert.Equal("mappingResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_MismatchedSensorMapping_Throws_ParamName_sensorApplicationResult()
    {
        MatchSensorRuntimeState runtimeA = CreateTwoTankRuntime();
        MatchSensorRuntimeState runtimeB = CreateTwoTankRuntime();

        ValidOptionAChain chainA = BuildValidOptionAChain(
            runtimeA,
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        MatchScriptDomainRequestMappingResult mappingB =
            ScriptTranslatedCommandDomainMapper.MapAll(
                MatchScriptIntentIntegrationComposer.EvaluateIntents(
                    runtimeB,
                    new List<ScriptProgram> { ScanWhenAlways(), FireWhenAlways() }));

        ScriptMappedSensorRequestApplicationResult sensorB =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtimeB, mappingB);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedScriptRuntimeComposerResult(
                chainA.Runtime,
                chainA.Integration,
                chainA.Mapping,
                sensorB,
                chainA.Turret,
                chainA.Construction,
                chainA.Fire,
                chainA.Movement,
                chainA.FinalRuntime,
                chainA.FinalSequence));

        Assert.Equal("sensorApplicationResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_MismatchedTurretApplicationMapping_Throws_ParamName_turretApplicationResult()
    {
        MatchSensorRuntimeState runtimeA = CreateTwoTankRuntime();
        MatchSensorRuntimeState runtimeB = CreateTwoTankRuntime();

        ValidOptionAChain chainA = BuildValidOptionAChain(
            runtimeA,
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        MatchScriptDomainRequestMappingResult mappingB =
            ScriptTranslatedCommandDomainMapper.MapAll(
                MatchScriptIntentIntegrationComposer.EvaluateIntents(
                    runtimeB,
                    new List<ScriptProgram> { ScanWhenAlways(), FireWhenAlways() }));

        ScriptMappedTurretRequestApplicationResult turretB =
            ScriptMappedTurretRequestApplicationPipeline.Apply(runtimeB, mappingB);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedScriptRuntimeComposerResult(
                chainA.Runtime,
                chainA.Integration,
                chainA.Mapping,
                chainA.Sensor,
                turretB,
                chainA.Construction,
                chainA.Fire,
                chainA.Movement,
                chainA.FinalRuntime,
                chainA.FinalSequence));

        Assert.Equal("turretApplicationResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_MismatchedFireConstructionMapping_Throws_ParamName_fireConstructionPipelineResult()
    {
        MatchSensorRuntimeState runtimeA = CreateTwoTankRuntime();
        MatchSensorRuntimeState runtimeB = CreateTwoTankRuntime();

        ValidOptionAChain chainA = BuildValidOptionAChain(
            runtimeA,
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        MatchScriptDomainRequestMappingResult mappingB =
            ScriptTranslatedCommandDomainMapper.MapAll(
                MatchScriptIntentIntegrationComposer.EvaluateIntents(
                    runtimeB,
                    new List<ScriptProgram> { ScanWhenAlways(), FireWhenAlways() }));

        ScriptMappedFireRequestConstructionPipelineResult constructionB =
            ScriptMappedFireRequestConstructionPipeline.Construct(
                runtimeB,
                mappingB,
                new ProjectileIdSequence(0));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedScriptRuntimeComposerResult(
                chainA.Runtime,
                chainA.Integration,
                chainA.Mapping,
                chainA.Sensor,
                chainA.Turret,
                constructionB,
                chainA.Fire,
                chainA.Movement,
                chainA.FinalRuntime,
                chainA.FinalSequence));

        Assert.Equal("fireConstructionPipelineResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_MismatchedFireApplicationConstruction_Throws_ParamName_fireApplicationResult()
    {
        MatchSensorRuntimeState runtimeA = CreateTwoTankRuntime();
        MatchSensorRuntimeState runtimeB = CreateTwoTankRuntime();

        ValidOptionAChain chainA = BuildValidOptionAChain(
            runtimeA,
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ValidOptionAChain chainB = BuildValidOptionAChain(
            runtimeB,
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedScriptRuntimeComposerResult(
                chainA.Runtime,
                chainA.Integration,
                chainA.Mapping,
                chainA.Sensor,
                chainA.Turret,
                chainA.Construction,
                chainB.Fire,
                chainA.Movement,
                chainA.FinalRuntime,
                chainA.FinalSequence));

        Assert.Equal("fireApplicationResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_FinalRuntimeWrongSensorLoadouts_Throws_ParamName_finalRuntime()
    {
        ValidOptionAChain chain = BuildValidOptionAChain(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        MatchSensorRuntimeState wrongFinalRuntime = chain.Movement.FinalRuntime.WithSensorLoadouts(
            chain.Runtime.SensorLoadouts);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedScriptRuntimeComposerResult(
                chain.Runtime,
                chain.Integration,
                chain.Mapping,
                chain.Sensor,
                chain.Turret,
                chain.Construction,
                chain.Fire,
                chain.Movement,
                wrongFinalRuntime,
                chain.FinalSequence));

        Assert.Equal("finalRuntime", ex.ParamName);
    }

    [Fact]
    public void Constructor_FinalRuntimeStateMustComeFromMovementApplication_Throws_ParamName_finalRuntime()
    {
        ValidOptionAChain chain = BuildValidOptionAChain(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            MoveToPatrolPointWhenAlways(),
            FireWhenAlways());

        MatchSensorRuntimeState wrongFinalRuntime =
            chain.Fire.FinalRuntime.WithSensorLoadouts(
                chain.Sensor.FinalRuntime.SensorLoadouts);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedScriptRuntimeComposerResult(
                chain.Runtime,
                chain.Integration,
                chain.Mapping,
                chain.Sensor,
                chain.Turret,
                chain.Construction,
                chain.Fire,
                chain.Movement,
                wrongFinalRuntime,
                chain.FinalSequence));

        Assert.Equal("finalRuntime", ex.ParamName);
    }

    [Fact]
    public void Constructor_FinalProjectileIdSequenceMismatch_Throws_ParamName_finalProjectileIdSequence()
    {
        ValidOptionAChain chain = BuildValidOptionAChain(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(0),
            ScanWhenAlways(),
            FireWhenAlways());

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new CombinedScriptRuntimeComposerResult(
                chain.Runtime,
                chain.Integration,
                chain.Mapping,
                chain.Sensor,
                chain.Turret,
                chain.Construction,
                chain.Fire,
                chain.Movement,
                chain.FinalRuntime,
                new ProjectileIdSequence(99)));

        Assert.Equal("finalProjectileIdSequence", ex.ParamName);
    }

    [Fact]
    public void Constructor_AcceptsCorrectOptionAMergedRuntime()
    {
        ValidOptionAChain chain = BuildValidOptionAChain(
            CreateTwoTankRuntime(),
            new ProjectileIdSequence(7),
            ScanWhenAlways(),
            FireWhenAlways());

        CombinedScriptRuntimeComposerResult result = CreateResult(chain);

        Assert.Same(chain.FinalRuntime, result.FinalRuntime);
        Assert.Same(chain.Movement.FinalRuntime.State, result.FinalRuntime.State);
        Assert.Same(
            chain.Sensor.FinalRuntime.SensorLoadouts,
            result.FinalRuntime.SensorLoadouts);
    }

    [Fact]
    public void Type_does_not_override_object_equals()
    {
        MethodInfo? equals = typeof(CombinedScriptRuntimeComposerResult).GetMethod(
            "Equals",
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: new[] { typeof(object) },
            modifiers: null);

        Assert.NotNull(equals);
        Assert.Equal(typeof(object), equals!.DeclaringType);
    }
}
