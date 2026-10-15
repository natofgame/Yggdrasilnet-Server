using System.Reflection;
using System.Runtime.Loader;

namespace Yggdrasilnet.Server.Plugin;

public class PluginContext(string path) : AssemblyLoadContext(Path.GetFileNameWithoutExtension(path), true) {
    private readonly AssemblyDependencyResolver _resolver = new(path);

    protected override Assembly? Load(AssemblyName assemblyName) {
        var shared = Default.Assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
        if (shared is not null) {
            return shared;
        }
 
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is null ? null : LoadFromAssemblyPath(path);
    }
    
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName) {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
    }
}