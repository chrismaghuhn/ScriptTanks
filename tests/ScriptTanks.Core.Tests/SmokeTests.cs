using ScriptTanks.Core;

namespace ScriptTanks.Core.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void CoreAssemblyMarker_ShouldExposeExpectedName()
    {
        Assert.Equal("ScriptTanks.Core", AssemblyMarker.Name);
    }
}
