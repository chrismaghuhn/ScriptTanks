using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptMappedFireRequestApplicationRecordTests
{
    private static ScriptCommandTranslationOutput MinimalTranslationOutput()
    {
        return new ScriptCommandTranslationOutput(
            ScriptCommandTranslationResult.NoIntent(),
            ScriptTranslatedCommandRequest.None());
    }

    private static ScriptDomainRequestMappingRecord MinimalWeaponMappingRecord()
    {
        return ScriptDomainRequestMappingRecord.Weapon(
            tankIndex: 0,
            tankId: new TankId(1),
            MinimalTranslationOutput(),
            fireRequest: null);
    }

    private static MatchFireRequest SampleFireRequest()
        => new MatchFireRequest(
            new TankId(1),
            new WeaponSlot(0),
            new ProjectileId(10),
            FixedVec2.FromInts(1, 2),
            FixedVec2.Zero);

    private static MatchState CreateState(SimTick tick)
    {
        TankState[] tanks =
        {
            new TankState(
                new TankId(0),
                new PlayerSlot(0),
                TankCatalog.BasicTank,
                new MovementState(FixedVec2.FromInts(10, 20), FixedVec2.Zero),
                TankCatalog.BasicTank.Stats.MaxHitPoints,
                Fixed.Zero,
                Fixed.Zero),
            new TankState(
                new TankId(1),
                new PlayerSlot(1),
                TankCatalog.BasicTank,
                new MovementState(FixedVec2.FromInts(90, 20), FixedVec2.Zero),
                TankCatalog.BasicTank.Stats.MaxHitPoints,
                Fixed.Zero,
                Fixed.Zero),
        };

        TankWeaponLoadout[] loadouts =
        {
            new TankWeaponLoadout(new[] { WeaponState.Ready(WeaponCatalog.StandardCannon) }),
            new TankWeaponLoadout(new[] { WeaponState.Ready(WeaponCatalog.StandardCannon) }),
        };

        return new MatchState(
            ArenaCatalog.OpenTestArena,
            tick,
            tanks,
            loadouts,
            Array.Empty<ProjectileState>());
    }

    private static ProjectileState CreateProjectileForState(int id)
    {
        ProjectileDefinition definition = new ProjectileDefinition(
            rawDamage: 25,
            speedPerTick: Fixed.FromInt(1),
            maxRange: Fixed.FromInt(40),
            radius: Fixed.FromRatio(1, 4));

        return new ProjectileState(
            new ProjectileId(id),
            definition,
            new TankId(0),
            new WeaponSlot(0),
            new SimTick(0),
            FixedVec2.FromInts(50, 50),
            FixedVec2.FromInts(1, 0),
            definition.MaxRange,
            isActive: true);
    }

    private static ScriptMappedFireRequestConstructionRecord ConstructedConstructionRecord()
    {
        ScriptDomainRequestMappingRecord mapping = MinimalWeaponMappingRecord();

        return ScriptMappedFireRequestConstructionRecord.Constructed(
            0,
            mapping,
            SampleFireRequest());
    }

    private static ScriptMappedFireRequestConstructionRecord NotConstructedConstructionRecord()
    {
        return ScriptMappedFireRequestConstructionRecord.NotWeapon(
            0,
            MinimalWeaponMappingRecord());
    }

    [Fact]
    public void Constructor_preserves_values_when_skipped()
    {
        ScriptMappedFireRequestConstructionRecord construction = NotConstructedConstructionRecord();

        var record = new ScriptMappedFireRequestApplicationRecord(
            recordIndex: 2,
            construction,
            ScriptMappedFireRequestApplicationStatus.SkippedNotConstructed,
            fireOutcome: null);

        Assert.Equal(2, record.RecordIndex);
        Assert.Same(construction, record.ConstructionRecord);
        Assert.Equal(ScriptMappedFireRequestApplicationStatus.SkippedNotConstructed, record.Status);
        Assert.Null(record.FireOutcome);
        Assert.False(record.DidApply);
    }

    [Fact]
    public void Constructor_preserves_values_when_applied()
    {
        ScriptMappedFireRequestConstructionRecord construction = ConstructedConstructionRecord();
        MatchState state = CreateState(new SimTick(5));
        MatchStateFireOutcome fireOutcome = MatchStateFireOutcome.Fired(
            state,
            CreateProjectileForState(1));

        var record = new ScriptMappedFireRequestApplicationRecord(
            1,
            construction,
            ScriptMappedFireRequestApplicationStatus.Applied,
            fireOutcome);

        Assert.Equal(1, record.RecordIndex);
        Assert.Same(construction, record.ConstructionRecord);
        Assert.Equal(ScriptMappedFireRequestApplicationStatus.Applied, record.Status);
        Assert.Same(fireOutcome, record.FireOutcome);
        Assert.True(record.DidApply);
    }

    [Fact]
    public void Constructor_preserves_values_when_rejected()
    {
        ScriptMappedFireRequestConstructionRecord construction = ConstructedConstructionRecord();
        MatchState state = CreateState(new SimTick(5));
        MatchStateFireOutcome fireOutcome = MatchStateFireOutcome.NotReady(state);

        var record = new ScriptMappedFireRequestApplicationRecord(
            3,
            construction,
            ScriptMappedFireRequestApplicationStatus.FireRejected,
            fireOutcome);

        Assert.Equal(3, record.RecordIndex);
        Assert.Equal(ScriptMappedFireRequestApplicationStatus.FireRejected, record.Status);
        Assert.Same(fireOutcome, record.FireOutcome);
        Assert.False(record.DidApply);
    }

    [Fact]
    public void Constructor_rejects_negative_recordIndex_ParamName_recordIndex()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScriptMappedFireRequestApplicationRecord(
                -1,
                NotConstructedConstructionRecord(),
                ScriptMappedFireRequestApplicationStatus.SkippedNotConstructed,
                fireOutcome: null));

        Assert.Equal("recordIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_constructionRecord_ParamName_constructionRecord()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptMappedFireRequestApplicationRecord(
                0,
                null!,
                ScriptMappedFireRequestApplicationStatus.SkippedNotConstructed,
                fireOutcome: null));

        Assert.Equal("constructionRecord", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_undefined_status_ParamName_status()
    {
        var bad = (ScriptMappedFireRequestApplicationStatus)99;

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScriptMappedFireRequestApplicationRecord(
                0,
                NotConstructedConstructionRecord(),
                bad,
                fireOutcome: null));

        Assert.Equal("status", ex.ParamName);
    }

    [Fact]
    public void Skipped_rejects_non_null_fireOutcome_ParamName_fireOutcome()
    {
        MatchState state = CreateState(new SimTick(5));
        MatchStateFireOutcome fireOutcome = MatchStateFireOutcome.NotReady(state);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedFireRequestApplicationRecord(
                0,
                NotConstructedConstructionRecord(),
                ScriptMappedFireRequestApplicationStatus.SkippedNotConstructed,
                fireOutcome));

        Assert.Equal("fireOutcome", ex.ParamName);
    }

    [Fact]
    public void Applied_requires_non_null_fireOutcome_ParamName_fireOutcome()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedFireRequestApplicationRecord(
                0,
                ConstructedConstructionRecord(),
                ScriptMappedFireRequestApplicationStatus.Applied,
                fireOutcome: null));

        Assert.Equal("fireOutcome", ex.ParamName);
    }

    [Fact]
    public void Applied_rejects_DidFire_false_ParamName_fireOutcome()
    {
        MatchState state = CreateState(new SimTick(5));
        MatchStateFireOutcome fireOutcome = MatchStateFireOutcome.NotReady(state);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedFireRequestApplicationRecord(
                0,
                ConstructedConstructionRecord(),
                ScriptMappedFireRequestApplicationStatus.Applied,
                fireOutcome));

        Assert.Equal("fireOutcome", ex.ParamName);
    }

    [Fact]
    public void Rejected_requires_non_null_fireOutcome_ParamName_fireOutcome()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedFireRequestApplicationRecord(
                0,
                ConstructedConstructionRecord(),
                ScriptMappedFireRequestApplicationStatus.FireRejected,
                fireOutcome: null));

        Assert.Equal("fireOutcome", ex.ParamName);
    }

    [Fact]
    public void Rejected_rejects_DidFire_true_ParamName_fireOutcome()
    {
        MatchState state = CreateState(new SimTick(5));
        MatchStateFireOutcome fireOutcome = MatchStateFireOutcome.Fired(
            state,
            CreateProjectileForState(1));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedFireRequestApplicationRecord(
                0,
                ConstructedConstructionRecord(),
                ScriptMappedFireRequestApplicationStatus.FireRejected,
                fireOutcome));

        Assert.Equal("fireOutcome", ex.ParamName);
    }

    [Fact]
    public void Applied_requires_constructed_construction_record_ParamName_constructionRecord()
    {
        MatchState state = CreateState(new SimTick(5));
        MatchStateFireOutcome fireOutcome = MatchStateFireOutcome.Fired(
            state,
            CreateProjectileForState(1));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedFireRequestApplicationRecord(
                0,
                NotConstructedConstructionRecord(),
                ScriptMappedFireRequestApplicationStatus.Applied,
                fireOutcome));

        Assert.Equal("constructionRecord", ex.ParamName);
    }

    [Fact]
    public void Rejected_requires_constructed_construction_record_ParamName_constructionRecord()
    {
        MatchState state = CreateState(new SimTick(5));
        MatchStateFireOutcome fireOutcome = MatchStateFireOutcome.NotReady(state);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedFireRequestApplicationRecord(
                0,
                NotConstructedConstructionRecord(),
                ScriptMappedFireRequestApplicationStatus.FireRejected,
                fireOutcome));

        Assert.Equal("constructionRecord", ex.ParamName);
    }

    [Fact]
    public void Skipped_requires_non_constructed_construction_record_ParamName_constructionRecord()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedFireRequestApplicationRecord(
                0,
                ConstructedConstructionRecord(),
                ScriptMappedFireRequestApplicationStatus.SkippedNotConstructed,
                fireOutcome: null));

        Assert.Equal("constructionRecord", ex.ParamName);
    }

    [Fact]
    public void DidApply_true_only_for_Applied_status()
    {
        MatchState state = CreateState(new SimTick(5));

        var skipped = ScriptMappedFireRequestApplicationRecord.SkippedNotConstructed(
            0,
            NotConstructedConstructionRecord());
        var applied = ScriptMappedFireRequestApplicationRecord.Applied(
            1,
            ConstructedConstructionRecord(),
            MatchStateFireOutcome.Fired(state, CreateProjectileForState(1)));
        var rejected = ScriptMappedFireRequestApplicationRecord.FireRejected(
            2,
            ConstructedConstructionRecord(),
            MatchStateFireOutcome.NotReady(state));

        Assert.False(skipped.DidApply);
        Assert.True(applied.DidApply);
        Assert.False(rejected.DidApply);
    }

    [Fact]
    public void Factory_SkippedNotConstructed_creates_expected_status()
    {
        ScriptMappedFireRequestConstructionRecord construction = NotConstructedConstructionRecord();

        var record = ScriptMappedFireRequestApplicationRecord.SkippedNotConstructed(0, construction);

        Assert.Equal(ScriptMappedFireRequestApplicationStatus.SkippedNotConstructed, record.Status);
        Assert.Null(record.FireOutcome);
        Assert.False(record.DidApply);
    }

    [Fact]
    public void Factory_Applied_creates_expected_status()
    {
        ScriptMappedFireRequestConstructionRecord construction = ConstructedConstructionRecord();
        MatchState state = CreateState(new SimTick(5));
        MatchStateFireOutcome fireOutcome = MatchStateFireOutcome.Fired(
            state,
            CreateProjectileForState(1));

        var record = ScriptMappedFireRequestApplicationRecord.Applied(0, construction, fireOutcome);

        Assert.Equal(ScriptMappedFireRequestApplicationStatus.Applied, record.Status);
        Assert.Same(fireOutcome, record.FireOutcome);
        Assert.True(record.DidApply);
    }

    [Fact]
    public void Factory_FireRejected_creates_expected_status()
    {
        ScriptMappedFireRequestConstructionRecord construction = ConstructedConstructionRecord();
        MatchState state = CreateState(new SimTick(5));
        MatchStateFireOutcome fireOutcome = MatchStateFireOutcome.NotReady(state);

        var record = ScriptMappedFireRequestApplicationRecord.FireRejected(0, construction, fireOutcome);

        Assert.Equal(ScriptMappedFireRequestApplicationStatus.FireRejected, record.Status);
        Assert.Same(fireOutcome, record.FireOutcome);
        Assert.False(record.DidApply);
    }

    [Fact]
    public void Factory_Applied_rejects_null_fireOutcome()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ScriptMappedFireRequestApplicationRecord.Applied(
                0,
                ConstructedConstructionRecord(),
                null!));
    }

    [Fact]
    public void Factory_FireRejected_rejects_null_fireOutcome()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ScriptMappedFireRequestApplicationRecord.FireRejected(
                0,
                ConstructedConstructionRecord(),
                null!));
    }
}
