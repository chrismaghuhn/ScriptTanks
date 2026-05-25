using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptMappedFireRequestConstructionRecordTests
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

    [Fact]
    public void Constructor_preserves_values_when_constructed()
    {
        ScriptDomainRequestMappingRecord mapping = MinimalWeaponMappingRecord();
        MatchFireRequest fire = SampleFireRequest();

        var record = new ScriptMappedFireRequestConstructionRecord(
            recordIndex: 3,
            mapping,
            ScriptMappedFireRequestConstructionStatus.Constructed,
            fire);

        Assert.Equal(3, record.RecordIndex);
        Assert.Same(mapping, record.MappingRecord);
        Assert.Equal(ScriptMappedFireRequestConstructionStatus.Constructed, record.Status);
        Assert.Equal(fire, record.FireRequest);
        Assert.True(record.DidConstruct);
    }

    [Fact]
    public void Constructor_rejects_negative_recordIndex_ParamName_recordIndex()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScriptMappedFireRequestConstructionRecord(
                -1,
                MinimalWeaponMappingRecord(),
                ScriptMappedFireRequestConstructionStatus.NotWeapon,
                fireRequest: null));

        Assert.Equal("recordIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_mappingRecord_ParamName_mappingRecord()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptMappedFireRequestConstructionRecord(
                0,
                null!,
                ScriptMappedFireRequestConstructionStatus.NotWeapon,
                fireRequest: null));

        Assert.Equal("mappingRecord", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_undefined_status_ParamName_status()
    {
        var bad = (ScriptMappedFireRequestConstructionStatus)99;

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScriptMappedFireRequestConstructionRecord(
                0,
                MinimalWeaponMappingRecord(),
                bad,
                fireRequest: null));

        Assert.Equal("status", ex.ParamName);
    }

    [Fact]
    public void Constructed_requires_non_null_fire_request_ParamName_fireRequest()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedFireRequestConstructionRecord(
                0,
                MinimalWeaponMappingRecord(),
                ScriptMappedFireRequestConstructionStatus.Constructed,
                fireRequest: null));

        Assert.Equal("fireRequest", ex.ParamName);
    }

    [Fact]
    public void Non_constructed_rejects_non_null_fire_request_ParamName_fireRequest()
    {
        MatchFireRequest fire = SampleFireRequest();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedFireRequestConstructionRecord(
                0,
                MinimalWeaponMappingRecord(),
                ScriptMappedFireRequestConstructionStatus.WeaponNotReady,
                fire));

        Assert.Equal("fireRequest", ex.ParamName);
    }

    [Fact]
    public void DidConstruct_false_for_failure_status()
    {
        var record = new ScriptMappedFireRequestConstructionRecord(
            0,
            MinimalWeaponMappingRecord(),
            ScriptMappedFireRequestConstructionStatus.WeaponNotReady,
            fireRequest: null);

        Assert.False(record.DidConstruct);
    }

    [Fact]
    public void Factory_NotWeapon_creates_expected_status()
    {
        ScriptDomainRequestMappingRecord mapping = MinimalWeaponMappingRecord();

        var record = ScriptMappedFireRequestConstructionRecord.NotWeapon(0, mapping);

        Assert.Equal(ScriptMappedFireRequestConstructionStatus.NotWeapon, record.Status);
        Assert.Null(record.FireRequest);
        Assert.False(record.DidConstruct);
    }

    [Fact]
    public void Factory_NoFireCommand_creates_expected_status()
    {
        ScriptDomainRequestMappingRecord mapping = MinimalWeaponMappingRecord();

        var record = ScriptMappedFireRequestConstructionRecord.NoFireCommand(1, mapping);

        Assert.Equal(ScriptMappedFireRequestConstructionStatus.NoFireCommand, record.Status);
        Assert.False(record.DidConstruct);
    }

    [Fact]
    public void Factory_Unsupported_creates_expected_status()
    {
        ScriptDomainRequestMappingRecord mapping = MinimalWeaponMappingRecord();

        var record = ScriptMappedFireRequestConstructionRecord.Unsupported(2, mapping);

        Assert.Equal(ScriptMappedFireRequestConstructionStatus.Unsupported, record.Status);
        Assert.False(record.DidConstruct);
    }

    [Fact]
    public void Factory_Constructed_wraps_fire_request()
    {
        ScriptDomainRequestMappingRecord mapping = MinimalWeaponMappingRecord();
        MatchFireRequest fire = SampleFireRequest();

        var record = ScriptMappedFireRequestConstructionRecord.Constructed(0, mapping, fire);

        Assert.Equal(ScriptMappedFireRequestConstructionStatus.Constructed, record.Status);
        Assert.Equal(fire, record.FireRequest);
        Assert.True(record.DidConstruct);
    }
}
