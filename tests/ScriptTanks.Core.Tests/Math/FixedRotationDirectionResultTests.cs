using System;
using ScriptTanks.Core.Math;
using Xunit;

namespace ScriptTanks.Core.Tests.Math;

public sealed class FixedRotationDirectionResultTests
{
    private static FixedVec2 SampleForward() => FixedVec2.FromInts(10, 20);

    [Fact]
    public void Constructor_stores_Resolved_status_and_forward()
    {
        FixedVec2 forward = FixedVec2.FromInts(1, 2);

        FixedRotationDirectionResult result = new FixedRotationDirectionResult(
            FixedRotationDirectionStatus.Resolved,
            forward);

        Assert.Equal(FixedRotationDirectionStatus.Resolved, result.Status);
        Assert.Equal(forward, result.Forward);
        Assert.True(result.IsResolved);
    }

    [Fact]
    public void Constructor_stores_non_resolved_status_with_null_forward()
    {
        FixedRotationDirectionResult result = new FixedRotationDirectionResult(
            FixedRotationDirectionStatus.UnsupportedRotationConvention,
            forward: null);

        Assert.Equal(
            FixedRotationDirectionStatus.UnsupportedRotationConvention,
            result.Status);
        Assert.Null(result.Forward);
        Assert.False(result.IsResolved);
    }

    [Fact]
    public void Constructor_rejects_undefined_status_ParamName_status()
    {
        var bad = (FixedRotationDirectionStatus)99;

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FixedRotationDirectionResult(bad, forward: null));

        Assert.Equal("status", ex.ParamName);
    }

    [Fact]
    public void Resolved_requires_non_null_forward_ParamName_forward()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new FixedRotationDirectionResult(
                FixedRotationDirectionStatus.Resolved,
                forward: null));

        Assert.Equal("forward", ex.ParamName);
    }

    [Theory]
    [InlineData(FixedRotationDirectionStatus.UnsupportedRotationConvention)]
    [InlineData(FixedRotationDirectionStatus.InvalidRotationValue)]
    public void Non_resolved_statuses_reject_non_null_forward_ParamName_forward(
        FixedRotationDirectionStatus status)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new FixedRotationDirectionResult(status, SampleForward()));

        Assert.Equal("forward", ex.ParamName);
    }

    [Fact]
    public void IsResolved_is_true_for_Resolved_factory()
    {
        FixedRotationDirectionResult result =
            FixedRotationDirectionResult.Resolved(SampleForward());

        Assert.True(result.IsResolved);
    }

    [Theory]
    [InlineData(FixedRotationDirectionStatus.UnsupportedRotationConvention)]
    [InlineData(FixedRotationDirectionStatus.InvalidRotationValue)]
    public void IsResolved_is_false_for_non_resolved_statuses(
        FixedRotationDirectionStatus status)
    {
        FixedRotationDirectionResult result = status switch
        {
            FixedRotationDirectionStatus.UnsupportedRotationConvention =>
                FixedRotationDirectionResult.UnsupportedRotationConvention(),
            FixedRotationDirectionStatus.InvalidRotationValue =>
                FixedRotationDirectionResult.InvalidRotationValue(),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

        Assert.False(result.IsResolved);
    }

    [Fact]
    public void Resolved_factory_stores_forward()
    {
        FixedVec2 forward = SampleForward();

        FixedRotationDirectionResult result =
            FixedRotationDirectionResult.Resolved(forward);

        Assert.Equal(FixedRotationDirectionStatus.Resolved, result.Status);
        Assert.Equal(forward, result.Forward);
    }

    [Fact]
    public void UnsupportedRotationConvention_factory_creates_correct_status_with_null_forward()
    {
        FixedRotationDirectionResult result =
            FixedRotationDirectionResult.UnsupportedRotationConvention();

        Assert.Equal(
            FixedRotationDirectionStatus.UnsupportedRotationConvention,
            result.Status);
        Assert.Null(result.Forward);
    }

    [Fact]
    public void InvalidRotationValue_factory_creates_correct_status_with_null_forward()
    {
        FixedRotationDirectionResult result =
            FixedRotationDirectionResult.InvalidRotationValue();

        Assert.Equal(FixedRotationDirectionStatus.InvalidRotationValue, result.Status);
        Assert.Null(result.Forward);
    }

    [Fact]
    public void No_equality_override_uses_reference_equality()
    {
        FixedVec2 forward = FixedVec2.FromInts(1, 2);

        FixedRotationDirectionResult a = FixedRotationDirectionResult.Resolved(forward);
        FixedRotationDirectionResult b = FixedRotationDirectionResult.Resolved(forward);

        Assert.NotEqual(a, b);
        Assert.False(ReferenceEquals(a, b));
        Assert.False(a.Equals(b));
    }
}
