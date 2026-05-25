using System;
using System.Collections.Generic;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Sensors;
using Xunit;

namespace ScriptTanks.Core.Tests.Sensors;

public sealed class SensorCatalogTests
{
    [Fact]
    public void BasicRadar_HasExpectedValues()
    {
        SensorDefinition definition = SensorCatalog.BasicRadar;

        Assert.Equal("basic_radar", definition.Id);
        Assert.Equal("Basic Radar", definition.DisplayName);
        Assert.Equal(Fixed.FromInt(35), definition.Range);
        Assert.Equal(Fixed.FromInt(90), definition.ConeAngleDegrees);
        Assert.Equal(5, definition.CooldownTicks);
        Assert.Equal(3, definition.CpuCost);
    }

    [Fact]
    public void WideScanner_HasExpectedValues()
    {
        SensorDefinition definition = SensorCatalog.WideScanner;

        Assert.Equal("wide_scanner", definition.Id);
        Assert.Equal("Wide Scanner", definition.DisplayName);
        Assert.Equal(Fixed.FromInt(25), definition.Range);
        Assert.Equal(Fixed.FromInt(180), definition.ConeAngleDegrees);
        Assert.Equal(8, definition.CooldownTicks);
        Assert.Equal(5, definition.CpuCost);
    }

    [Fact]
    public void All_PreservesExpectedOrder()
    {
        IReadOnlyList<SensorDefinition> all = SensorCatalog.All;

        Assert.Equal(2, all.Count);
        Assert.Equal(SensorCatalog.BasicRadar, all[0]);
        Assert.Equal(SensorCatalog.WideScanner, all[1]);
    }

    [Fact]
    public void All_IsReadOnly()
    {
        IList<SensorDefinition> list = (IList<SensorDefinition>)SensorCatalog.All;

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add(SensorCatalog.BasicRadar));
    }

    [Fact]
    public void GetById_ReturnsBasicRadar()
    {
        SensorDefinition definition = SensorCatalog.GetById("basic_radar");

        Assert.Equal(SensorCatalog.BasicRadar, definition);
    }

    [Fact]
    public void GetById_ReturnsWideScanner()
    {
        SensorDefinition definition = SensorCatalog.GetById("wide_scanner");

        Assert.Equal(SensorCatalog.WideScanner, definition);
    }

    [Fact]
    public void GetById_RejectsNullEmptyWhitespace()
    {
        Assert.Throws<ArgumentNullException>(() => SensorCatalog.GetById(null!));
        Assert.Throws<ArgumentException>(() => SensorCatalog.GetById(string.Empty));
        Assert.Throws<ArgumentException>(() => SensorCatalog.GetById("   "));
    }

    [Fact]
    public void GetById_RejectsUnknownId()
    {
        Assert.Throws<KeyNotFoundException>(() => SensorCatalog.GetById("missing"));
    }

    [Fact]
    public void GetById_IsOrdinalAndCaseSensitive()
    {
        Assert.Throws<KeyNotFoundException>(() => SensorCatalog.GetById("BASIC_RADAR"));
    }
}
