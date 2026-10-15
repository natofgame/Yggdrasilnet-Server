using System.Reflection;
using Serilog;

namespace Yggdrasilnet.Server.Plugin;

/// <summary>
///  Plugin manager.
/// </summary>
/// <param name="server"></param>
public class PluginManager(Bootstrap.Server server) {
    private const string PluginsDirector = "plugins";
    
    private readonly List<Plugin> _plugins = [];
     
    /// <summary>
    ///  The loaded plugins.
    /// </summary>
    public IReadOnlyList<IYggdrasilPlugin> Plugins => _plugins.Select(p => p.Instance).ToList();
    
    /// <summary>
    ///  Load all plugins in the plugins directory.
    /// </summary>
    public void LoadAll() {
        var dir = Path.GetFullPath(PluginsDirector);
        if (!Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
            Log.Information("[Plugin] No plugin directory found, created:" + dir);
            return;
        }
 
        foreach (var dll in Directory.EnumerateFiles(dir, "*.dll")) {
            LoadFile(dll);
        }
 
        Log.Information("[Plugin] Loaded {Count} plugins", _plugins.Count);
    }
    
    /// <summary>
    ///  Load a single plugin.
    /// </summary>
    /// <param name="dllPath"></param>
    private void LoadFile(string dllPath) {
        var fullPath = Path.GetFullPath(dllPath);
        PluginContext? context = null;
 
        try {
            context = new PluginContext(fullPath);
            var assembly = context.LoadFromAssemblyPath(fullPath);
 
            var types = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false }
                            && typeof(IYggdrasilPlugin).IsAssignableFrom(t))
                .ToList();
 
            if (types.Count == 0) {
                context.Unload();
                return;
            }
 
            var loadedAny = false;
            foreach (var type in types) {
                loadedAny |= TryLoadType(type, context, fullPath);
            }
 
            if (!loadedAny) {
                context.Unload();
            }
        }
        catch (Exception ex) {
            Log.Error(ex, "Échec du chargement de {File}", Path.GetFileName(fullPath));
            context?.Unload();
        }
    }
    
    /// <summary>
    ///  Try to load a type as a plugin.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="context"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    private bool TryLoadType(Type type, PluginContext context, string path) {
        try {
            if (Activator.CreateInstance(type) is not IYggdrasilPlugin instance) {
                return false;
            }
 
            if (_plugins.Any(p => p.Instance.Id == instance.Id)) {
                Log.Error("[Plugin] Plugin already loaded : {Id}", instance.Id);
                return false;
            }
 
            var meta = type.GetCustomAttribute<PluginAttribute>();
            instance.OnLoad(server);
            _plugins.Add(new Plugin(instance, context, path));
 
            Log.Information("Plugin loaded : {Name} v{Version} by {Author}",
                meta?.Name ?? instance.Id, meta?.Version ?? "?", meta?.Author ?? "Unknown");
            return true;
        }
        catch (Exception ex) {
            Log.Error(ex, "[Plugin] Error loading plugin : {Type}", type.FullName ?? "Unknown");
            return false;
        }
    }
    
    /// <summary>
    ///  Enable all plugins.
    /// </summary>
    /// <param name="plugin"></param>
    private void Enable(Plugin plugin) {
        if (plugin.Enabled) {
            return;
        }
 
        try {
            plugin.Instance.OnEnable(server);
            plugin.Enabled = true;
            Log.Information("[Plugin] Plugin enabled : {Name}", plugin.Instance.Id);
        }
        catch (Exception ex) {
            Log.Error(ex, "[Plugin] Error activating plugin : {Name}", plugin.Instance.Id);
        }
    }

    /// <summary>
    ///  Enable all plugins.
    /// </summary>
    public void EnableAll() {
        foreach (var plugin in _plugins) {
            Enable(plugin);
        }
    }

    /// <summary>
    ///  Disable all plugins.
    /// </summary>
    public void DisableAll() {
        for (var i = _plugins.Count - 1; i >= 0; i--) {
            Disable(_plugins[i]);
        }
 
        foreach (var plugin in _plugins) {
            plugin.Context.Unload();
        }
 
        _plugins.Clear();
    }
    
    /// <summary>
    ///  Disable a plugin.
    /// </summary>
    /// <param name="plugin"></param>
    private void Disable(Plugin plugin) {
        if (!plugin.Enabled) {
            return;
        }
 
        try {
            plugin.Instance.OnDisable(server);
        }
        catch (Exception ex) {
            Log.Error(ex, "[Plugin] Error disabling plugin : {Name}", plugin.Instance.Id);
        }
        finally {
            plugin.Enabled = false;
        }
    }

}