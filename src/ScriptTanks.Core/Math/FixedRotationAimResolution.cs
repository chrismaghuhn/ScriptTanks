namespace ScriptTanks.Core.Math;

/// <summary>
/// Outcome of resolving a direction vector to a turret turn-fraction rotation.
/// </summary>
/// <remarks>
/// Pure data carrier for cardinal MVP aim resolution. Does not use floating-point
/// math or trigonometry, mutate match state, or integrate scripting pipelines.
/// </remarks>
public readonly struct FixedRotationAimResolution
{
    public bool IsResolved { get; }

    public Fixed Rotation { get; }

    private FixedRotationAimResolution(bool isResolved, Fixed rotation)
    {
        IsResolved = isResolved;
        Rotation = rotation;
    }

    public static FixedRotationAimResolution Resolved(Fixed rotation)
        => new FixedRotationAimResolution(isResolved: true, rotation);

    public static FixedRotationAimResolution Unresolved()
        => new FixedRotationAimResolution(isResolved: false, rotation: default);
}
