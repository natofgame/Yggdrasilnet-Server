using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Yggdrasilnet.Server.Bootstrap;

/// <summary>
///     Version of the running Yggdrasil server.
/// </summary>
public static class ServerVersion {
    /// <summary>
    ///     The full version, as set by &lt;Version&gt; in the project file (e.g. "0.0.1-alpha").
    /// </summary>
    public static string Current { get; } = ResolveCurrent();

    /// <summary>
    ///     The numeric part of <see cref="Current" />, without pre-release or build metadata.
    /// </summary>
    public static Version Numeric { get; } = TryParse(Current, out var version) ? version : new Version(0, 0, 0);

    /// <summary>
    ///     Parses a semantic version ("1.2.3", "1.2.3-alpha", "1.2.3+sha") into its numeric part.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    public static bool TryParse(string? value, [NotNullWhen(true)] out Version? version) {
        version = null;
        if (string.IsNullOrWhiteSpace(value)) {
            return false;
        }

        var numeric = value.Trim().Split('-', '+')[0];
        if (!Version.TryParse(numeric, out var parsed)) {
            return false;
        }

        version = new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0));
        return true;
    }

    private static string ResolveCurrent() {
        var assembly = typeof(ServerVersion).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational)) {
            return informational.Split('+')[0];
        }

        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }
}
