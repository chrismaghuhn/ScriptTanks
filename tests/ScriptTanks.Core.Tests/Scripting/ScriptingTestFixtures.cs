using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Scripting;

namespace ScriptTanks.Core.Tests.Scripting;

internal static class ScriptingTestFixtures
{
    internal static ScriptVisibleTurretAimStatusResult DefaultNoTargetAimStatus { get; } =
        ScriptVisibleTurretAimStatusResult.NoTarget(new TankId(0), 0, Fixed.Zero);
}
