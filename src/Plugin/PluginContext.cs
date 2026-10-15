using System.Reflection;
using System.Runtime.Loader;

namespace Yggdrasilnet.Server.Plugin;

/// <summary>
///  Plugin context.
/// </summary>
/// <param name="path"></param>
public class PluginContext(string path) : AssemblyLoadContext(Path.GetFileNameWithoutExtension(path), true) {
    private readonly AssemblyDependencyResolver _resolver = new(path);

    /// <summary>
    ///  Loads an assembly from the plugin's directory.'
    /// </summary>
    /// <param name="assemblyName"></param>
    /// <returns></returns>
    protected override Assembly? Load(AssemblyName assemblyName) {
        var shared = Default.Assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
        if (shared is not null) {
            return shared;
        }
 
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is null ? null : LoadFromAssemblyPath(path);
    }
    
    /// <summary>
    ///  Loads an unmanaged DLL from the plugin's directory.'
    /// </summary>
    /// <param name="unmanagedDllName"></param>
    /// <returns></returns>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName) {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
    }
}