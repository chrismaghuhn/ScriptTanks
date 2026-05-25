using System;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;

namespace ScriptTanks.Core.Tests.Movement;

public sealed class MovementIntegratorTests
{
    [Fact]
    public void Step_WithZeroVelocity_KeepsPosition()
    {
        MovementState state = new MovementState(
            FixedVec2.FromInts(7, -4),
            FixedVec2.Zero);

        MovementState next = MovementIntegrator.Step(state);

        Assert.Equal(FixedVec2.FromInts(7, -4), next.Position);
    }

    [Fact]
    public void Step_WithPositiveVelocity_MovesPosition()
    {
        MovementState state = new MovementState(
            FixedVec2.FromInts(0, 0),
            FixedVec2.FromInts(2, 3));

        MovementState next = MovementIntegrator.Step(state);

        Assert.Equal(FixedVec2.FromInts(2, 3), next.Position);
    }

    [Fact]
    public void Step_WithNegativeVelocity_MovesPositionBackward()
    {
        MovementState state = new MovementState(
            FixedVec2.FromInts(10, 5),
            FixedVec2.FromInts(-1, -2));

        MovementState next = MovementIntegrator.Step(state);

        Assert.Equal(FixedVec2.FromInts(9, 3), next.Position);
    }

    [Fact]
    public void Step_PreservesVelocity()
    {
        MovementState state = new MovementState(
            FixedVec2.FromInts(0, 0),
            FixedVec2.FromInts(2, 3));

        MovementState next = MovementIntegrator.Step(state);

        Assert.Equal(state.VelocityPerTick, next.VelocityPerTick);
    }

    [Fact]
    public void Step_WithTickCountZero_ReturnsUnchangedPosition()
    {
        MovementState state = new MovementState(
            FixedVec2.FromInts(0, 0),
            FixedVec2.FromInts(2, 3));

        MovementState next = MovementIntegrator.Step(state, 0);

        Assert.Equal(FixedVec2.FromInts(0, 0), next.Position);
        Assert.Equal(state.VelocityPerTick, next.VelocityPerTick);
    }

    [Fact]
    public void Step_WithTickCountThree_AppliesVelocityThreeTimes()
    {
        MovementState state = new MovementState(
            FixedVec2.FromInts(0, 0),
            FixedVec2.FromInts(2, 3));

        MovementState next = MovementIntegrator.Step(state, 3);

        Assert.Equal(FixedVec2.FromInts(6, 9), next.Position);
    }

    [Fact]
    public void Step_MultiStep_EqualsRepeatedSingleStep()
    {
        MovementState state = new MovementState(
            FixedVec2.FromInts(1, 2),
            FixedVec2.FromInts(2, -1));

        MovementState batch = MovementIntegrator.Step(state, 3);

        MovementState chained = MovementIntegrator.Step(
            MovementIntegrator.Step(
                MovementIntegrator.Step(state)));

        Assert.Equal(chained.Position, batch.Position);
    }

    [Fact]
    public void Step_WithTickCountFive_PreservesVelocity()
    {
        MovementState state = new MovementState(
            FixedVec2.FromInts(0, 0),
            FixedVec2.FromInts(2, 3));

        MovementState next = MovementIntegrator.Step(state, 5);

        Assert.Equal(state.VelocityPerTick, next.VelocityPerTick);
    }

    [Fact]
    public void Step_WithNegativeTickCount_Throws()
    {
        MovementState state = new MovementState(
            FixedVec2.FromInts(0, 0),
            FixedVec2.FromInts(2, 3));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => MovementIntegrator.Step(state, -1));
    }
}
