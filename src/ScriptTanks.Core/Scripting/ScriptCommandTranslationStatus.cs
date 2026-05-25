namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Outcomes for future script command translation from intent to concrete kernel requests.
/// </summary>
/// <remarks>
/// Pure classification values only; no translation logic, execution, match mutation, logging,
/// replay recording, diagnostics, or Godot integration.
/// </remarks>
public enum ScriptCommandTranslationStatus
{
    NoIntent = 0,
    Translated = 1,
    UnsupportedCommand = 2,
    InvalidCommandArgument = 3,
    MissingHardware = 4
}
