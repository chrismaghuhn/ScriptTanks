namespace ScriptTanks.Core.Math;

/// <summary>
/// Outcome classification for a future deterministic Fixed rotation-to-forward resolution attempt.
/// </summary>
/// <remarks>
/// Pure status labels only. This enum does not read rotation values, normalize turns,
/// consult lookup tables, derive fire direction, or mutate match state.
/// </remarks>
public enum FixedRotationDirectionStatus
{
    /// <summary>
    /// A deterministic forward direction was resolved.
    /// </summary>
    Resolved = 0,

    /// <summary>
    /// The rotation value cannot be interpreted under the active convention.
    /// </summary>
    UnsupportedRotationConvention = 1,

    /// <summary>
    /// The rotation value is invalid and cannot be normalized safely.
    /// </summary>
    InvalidRotationValue = 2,
}
