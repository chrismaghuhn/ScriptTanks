using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Sensors;
using Xunit;

namespace ScriptTanks.Core.Tests.Sensors;

public sealed class DetectedTankSnapshotTests
{
    private static DetectedTankSnapshot CreateSnapshot(
        int tankId = 1,
        int ownerSlot = 1,
        FixedVec2? position = null,
        Fixed? distance = null)
    {
        return new DetectedTankSnapshot(
            new TankId(tankId),
            new PlayerSlot(ownerSlot),
            position ?? FixedVec2.FromInts(10, 20),
            distance ?? Fixed.FromInt(15));
    }

    [Fact]
    public void Constructor_PreservesValues()
    {
        DetectedTankSnapshot snapshot = new DetectedTankSnapshot(
            new TankId(7),
            new PlayerSlot(2),
            FixedVec2.FromInts(3, 4),
            Fixed.FromInt(12));

        Assert.Equal(new TankId(7), snapshot.TankId);
        Assert.Equal(new PlayerSlot(2), snapshot.OwnerSlot);
        Assert.Equal(FixedVec2.FromInts(3, 4), snapshot.Position);
        Assert.Equal(Fixed.FromInt(12), snapshot.Distance);
    }

    [Fact]
    public void Constructor_AllowsZeroDistance()
    {
        DetectedTankSnapshot snapshot = CreateSnapshot(distance: Fixed.Zero);

        Assert.Equal(Fixed.Zero, snapshot.Distance);
    }

    [Fact]
    public void Constructor_RejectsNegativeDistance()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateSnapshot(distance: Fixed.FromInt(-1)));
        Assert.Equal("distance", ex.ParamName);
    }

    [Fact]
    public void Equals_ReturnsTrue_ForSameValues()
    {
        DetectedTankSnapshot a = CreateSnapshot();
        DetectedTankSnapshot b = CreateSnapshot();

        Assert.Equal(a, b);
        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenTankIdDiffers()
    {
        DetectedTankSnapshot a = CreateSnapshot(tankId: 1);
        DetectedTankSnapshot b = CreateSnapshot(tankId: 2);

        Assert.NotEqual(a, b);
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Operators_MatchEquals()
    {
        DetectedTankSnapshot a = CreateSnapshot();
        DetectedTankSnapshot b = CreateSnapshot();
        DetectedTankSnapshot different = CreateSnapshot(tankId: 99);

        Assert.True(a == b);
        Assert.False(a != b);
        Assert.False(a == different);
        Assert.True(a != different);
    }

    [Fact]
    public void GetHashCode_ReturnsSameValue_ForEqualSnapshots()
    {
        DetectedTankSnapshot a = CreateSnapshot();
        DetectedTankSnapshot b = CreateSnapshot();

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsSmokeSafeString()
    {
        string text = CreateSnapshot().ToString();

        Assert.False(string.IsNullOrWhiteSpace(text));
    }
}
