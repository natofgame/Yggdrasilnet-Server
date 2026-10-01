using System.Reflection;
using Serilog;
using Yggdrasil.Plugin.Event;
using Yggdrasil.Plugin.Event.Events;
using Yggdrasil.Plugin.Plugin;

namespace Yggdrasilnet.Server.Plugins;

public sealed class PluginManager {
    private sealed record LoadedPlugin(IPlugin Instance, string AssemblyPath);

    private readonly List<LoadedPlugin> _plugins = [];
    private readonly HashSet<string> _loadedPluginIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly EventBus _eventBus;
    private readonly PluginContext _context;

    public int LoadedCount => _plugins.Count;

    public PluginManager() {
        PluginContext? context = null;
        _eventBus = new EventBus(
            () => context ?? throw new InvalidOperationException("Plugin context is not initialized."),
            ex => Log.Error(ex, "Unhandled plugin event exception"));
        context = new PluginContext(
            "Yggdrasilnet.Server",
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0",
            _eventBus,
            message => Log.Information("[Plugin] {Message}", message));
        _context = context;
    }

    public void DiscoverAndLoad(string pluginsDirectory) {
        if (!Directory.Exists(pluginsDirectory)) {
            Directory.CreateDirectory(pluginsDirectory);
            Log.Information("Plugin directory created at {Path}.", pluginsDirectory);
        }

        var pluginDirectories = ResolvePluginDirectories(pluginsDirectory);
        var loadedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pluginDirectory in pluginDirectories) {
            var pluginAssemblies = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories);
            foreach (var assemblyPath in pluginAssemblies) {
                var fullPath = Path.GetFullPath(assemblyPath);
                if (!IsLoadCandidate(fullPath)) {
                    continue;
                }

                if (!loadedAssemblies.Add(fullPath)) {
                    continue;
                }

                TryLoadAssembly(fullPath);
            }
        }

        Log.Information("Loaded {Count} plugin(s).", _plugins.Count);
    }

    public void RaiseServerStarting(int port, int tickRate) {
        _eventBus.Post(new ServerStartingEvent(port, tickRate));
    }

    public void Shutdown() {
        _eventBus.Post(new ServerStoppingEvent());

        for (var i = _plugins.Count - 1; i >= 0; i--) {
            var plugin = _plugins[i];
            try {
                plugin.Instance.OnDisable();
            } catch (Exception ex) {
                Log.Error(ex, "Plugin {PluginId} failed during OnDisable", plugin.Instance.Id);
            }
        }
    }

    private void TryLoadAssembly(string assemblyPath) {
        try {
            var loadContext = new PluginLoadContext(assemblyPath);
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
            var pluginTypes = ResolvePluginTypes(assembly);
            if (pluginTypes.Count == 0) {
                return;
            }

            foreach (var pluginType in pluginTypes) {
                var instance = (IPlugin?)Activator.CreateInstance(pluginType);
                if (instance == null) {
                    throw new InvalidOperationException($"Unable to instantiate plugin type '{pluginType.FullName}'.");
                }

                if (!_loadedPluginIds.Add(instance.Id)) {
                    Log.Warning(
                        "Skipping plugin {PluginId} from {AssemblyPath}: id already loaded.",
                        instance.Id,
                        assemblyPath);
                    continue;
                }

                instance.OnLoad(_context);
                instance.OnEnable();
                _plugins.Add(new LoadedPlugin(instance, assemblyPath));

                Log.Information("Plugin {PluginId} loaded", instance.Id);
            }
        } catch (Exception ex) {
            Log.Error(ex, "Failed to load plugin assembly {AssemblyPath}", assemblyPath);
        }
    }

    private static List<Type> ResolvePluginTypes(Assembly assembly) {
        IEnumerable<Type> types;
        try {
            types = assembly.GetTypes();
        } catch (ReflectionTypeLoadException ex) {
            types = ex.Types.Where(t => t != null)!;
        }

        return types
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => typeof(IPlugin).IsAssignableFrom(t))
            .ToList()!;
    }

    private static List<string> ResolvePluginDirectories(string primaryPluginsDirectory) {
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            Path.GetFullPath(primaryPluginsDirectory)
        };

        AddParentPluginDirectories(directories, AppContext.BaseDirectory);
        AddParentPluginDirectories(directories, Directory.GetCurrentDirectory());

        return directories.Where(Directory.Exists).ToList();
    }

    private static void AddParentPluginDirectories(HashSet<string> directories, string startDirectory) {
        var current = new DirectoryInfo(startDirectory);
        while (current != null) {
            var candidate = Path.Combine(current.FullName, "Plugins");
            directories.Add(Path.GetFullPath(candidate));
            current = current.Parent;
        }
    }
    
    private static bool IsLoadCandidate(string assemblyPath) {
        var normalized = Path.GetFullPath(assemblyPath)
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        var objSegment = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        var refSegment = $"{Path.DirectorySeparatorChar}ref{Path.DirectorySeparatorChar}";
        var refIntSegment = $"{Path.DirectorySeparatorChar}refint{Path.DirectorySeparatorChar}";

        return !normalized.Contains(objSegment, StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains(refSegment, StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains(refIntSegment, StringComparison.OrdinalIgnoreCase);
    }
}
