namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Kernel subsystem category for a translated script command after domain mapping.
/// </summary>
/// <remarks>
/// Pure classification only; this enum does not execute gameplay, dispatch requests,
/// mutate match state, or integrate Godot.
/// </remarks>
public enum ScriptDomainRequestCategory
{
    None = 0,

    Sensor = 1,

    Weapon = 2,

    Turret = 3,

    Movement = 4,

    Unsupported = 5,
}
