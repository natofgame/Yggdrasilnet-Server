using Xunit;
using Yggdrasilnet.Server.Bootstrap;
using Yggdrasilnet.Server.Plugin;

namespace Yggdrasilnet.Server.Tests;

public sealed class PluginVersionTests {
    [Theory]
    [InlineData("0.0.1", 0, 0, 1)]
    [InlineData("0.0.1-alpha", 0, 0, 1)]
    [InlineData("1.2.3+abcdef", 1, 2, 3)]
    [InlineData("1.2", 1, 2, 0)]
    public void TryParse_ReadsNumericPart(string value, int major, int minor, int patch) {
        Assert.True(ServerVersion.TryParse(value, out var version));
        Assert.Equal(new Version(major, minor, patch), version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    public void TryParse_RejectsInvalidValues(string? value) {
        Assert.False(ServerVersion.TryParse(value, out _));
    }

    [Fact]
    public void ServerVersion_MatchesProjectVersion() {
        Assert.Equal("0.0.1-alpha", ServerVersion.Current);
    }

    [Fact]
    public void IsCompatible_AcceptsCurrentOrOlderRequirement() {
        Assert.True(PluginManager.IsCompatible(typeof(object), new PluginAttribute("a", "1.0.0", yggdrasilVersion: "0.0.1")));
        Assert.True(PluginManager.IsCompatible(typeof(object), new PluginAttribute("a", "1.0.0", yggdrasilVersion: "0.0.0")));
        Assert.True(PluginManager.IsCompatible(typeof(object), new PluginAttribute("a", "1.0.0")));
    }

    [Fact]
    public void IsCompatible_RejectsNewerOrInvalidRequirement() {
        Assert.False(PluginManager.IsCompatible(typeof(object), new PluginAttribute("a", "1.0.0", yggdrasilVersion: "0.1.0")));
        Assert.False(PluginManager.IsCompatible(typeof(object), new PluginAttribute("a", "1.0.0", yggdrasilVersion: "nope")));
    }
}
