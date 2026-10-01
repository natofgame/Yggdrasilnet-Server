using System.Reflection;
using System.Runtime.Loader;

namespace Yggdrasilnet.Server.Plugins;

internal sealed class PluginLoadContext(string pluginAssemblyPath) : AssemblyLoadContext(isCollectible: false) {
    private readonly AssemblyDependencyResolver _resolver = new(pluginAssemblyPath);

    protected override Assembly? Load(AssemblyName assemblyName) {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath == null) {
            return null;
        }

        return LoadFromAssemblyPath(assemblyPath);
    }
}
