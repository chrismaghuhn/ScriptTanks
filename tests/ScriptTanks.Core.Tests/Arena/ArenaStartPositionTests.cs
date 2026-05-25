using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Tests.Arena;

public sealed class ArenaStartPositionTests
{
    [Fact]
    public void Constructor_PreservesSlot()
    {
        ArenaStartPosition start = new ArenaStartPosition(
            new PlayerSlot(1),
            FixedVec2.FromInts(10, 10));

        Assert.Equal(new PlayerSlot(1), start.Slot);
    }

    [Fact]
    public void Constructor_PreservesPosition()
    {
        ArenaStartPosition start = new ArenaStartPosition(
            new PlayerSlot(1),
            FixedVec2.FromInts(10, 10));

        Assert.Equal(FixedVec2.FromInts(10, 10), start.Position);
    }

    [Fact]
    public void Constructor_AcceptsSlotZero()
    {
        ArenaStartPosition start = new ArenaStartPosition(
            new PlayerSlot(0),
            FixedVec2.FromInts(10, 10));

        Assert.Equal(new PlayerSlot(0), start.Slot);
    }

    [Fact]
    public void Equals_AndObject_Work()
    {
        ArenaStartPosition a = new ArenaStartPosition(
            new PlayerSlot(0),
            FixedVec2.FromInts(10, 10));
        ArenaStartPosition b = new ArenaStartPosition(
            new PlayerSlot(0),
            FixedVec2.FromInts(10, 10));
        ArenaStartPosition differentSlot = new ArenaStartPosition(
            new PlayerSlot(1),
            FixedVec2.FromInts(10, 10));
        ArenaStartPosition differentPosition = new ArenaStartPosition(
            new PlayerSlot(0),
            FixedVec2.FromInts(11, 10));

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(differentSlot));
        Assert.False(a.Equals(differentPosition));
        Assert.False(a.Equals("not an ArenaStartPosition"));
        Assert.False(a.Equals(null));
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForEqualValues()
    {
        ArenaStartPosition a = new ArenaStartPosition(
            new PlayerSlot(1),
            FixedVec2.FromInts(10, 10));
        ArenaStartPosition b = new ArenaStartPosition(
            new PlayerSlot(1),
            FixedVec2.FromInts(10, 10));

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ContainsSlotAndPosition_SmokeOnly()
    {
        ArenaStartPosition start = new ArenaStartPosition(
            new PlayerSlot(1),
            FixedVec2.FromInts(10, 10));

        string text = start.ToString();

        Assert.Contains("slot=", text);
        Assert.Contains("position=", text);
        Assert.Contains("1", text);
    }
}
