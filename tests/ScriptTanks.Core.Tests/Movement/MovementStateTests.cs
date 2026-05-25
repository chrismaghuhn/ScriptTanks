using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;

namespace ScriptTanks.Core.Tests.Movement;

public sealed class MovementStateTests
{
    [Fact]
    public void Constructor_PreservesPositionAndVelocity()
    {
        FixedVec2 position = FixedVec2.FromInts(3, -7);
        FixedVec2 velocity = FixedVec2.FromInts(1, 2);

        MovementState state = new MovementState(position, velocity);

        Assert.Equal(position, state.Position);
        Assert.Equal(velocity, state.VelocityPerTick);
    }

    [Fact]
    public void WithPosition_ChangesOnlyPosition()
    {
        FixedVec2 originalPosition = FixedVec2.FromInts(0, 0);
        FixedVec2 velocity = FixedVec2.FromInts(2, 3);
        MovementState state = new MovementState(originalPosition, velocity);

        FixedVec2 newPosition = FixedVec2.FromInts(10, -5);
        MovementState updated = state.WithPosition(newPosition);

        Assert.Equal(newPosition, updated.Position);
        Assert.Equal(velocity, updated.VelocityPerTick);
    }

    [Fact]
    public void WithVelocityPerTick_ChangesOnlyVelocity()
    {
        FixedVec2 position = FixedVec2.FromInts(4, 4);
        FixedVec2 originalVelocity = FixedVec2.FromInts(1, 1);
        MovementState state = new MovementState(position, originalVelocity);

        FixedVec2 newVelocity = FixedVec2.FromInts(-2, 3);
        MovementState updated = state.WithVelocityPerTick(newVelocity);

        Assert.Equal(position, updated.Position);
        Assert.Equal(newVelocity, updated.VelocityPerTick);
    }

    [Fact]
    public void Equals_MovementState_AndObject_Work()
    {
        MovementState a = new MovementState(
            FixedVec2.FromInts(1, 2),
            FixedVec2.FromInts(3, 4));
        MovementState b = new MovementState(
            FixedVec2.FromInts(1, 2),
            FixedVec2.FromInts(3, 4));
        MovementState differentPosition = new MovementState(
            FixedVec2.FromInts(9, 2),
            FixedVec2.FromInts(3, 4));
        MovementState differentVelocity = new MovementState(
            FixedVec2.FromInts(1, 2),
            FixedVec2.FromInts(9, 4));

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(differentPosition));
        Assert.False(a.Equals(differentVelocity));
        Assert.False(a.Equals("not a MovementState"));
        Assert.False(a.Equals(null));
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForEqualValues()
    {
        MovementState a = new MovementState(
            FixedVec2.FromInts(7, 8),
            FixedVec2.FromInts(-1, 1));
        MovementState b = new MovementState(
            FixedVec2.FromInts(7, 8),
            FixedVec2.FromInts(-1, 1));

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ContainsPositionAndVelocityComponents_SmokeOnly()
    {
        MovementState state = new MovementState(
            FixedVec2.FromInts(1, 2),
            FixedVec2.FromInts(3, 4));

        string text = state.ToString();

        Assert.Contains("1", text);
        Assert.Contains("2", text);
        Assert.Contains("3", text);
        Assert.Contains("4", text);
    }
}
