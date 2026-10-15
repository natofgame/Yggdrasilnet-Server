using System.Reflection;

namespace Yggdrasilnet.Server.Plugin;

public abstract class YggdrasilPlugin : IYggdrasilPlugin {
    /// <summary>
    ///  The metadata of the plugin.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    private PluginAttribute Metadata => field ??=
        GetType().GetCustomAttribute<PluginAttribute>()
        ?? throw new InvalidOperationException("Plugin metadata not found.");

    /// <inheritdoc/>
    public string Id => Metadata.Id;

    /// <inheritdoc/>
    public virtual void OnLoad(Bootstrap.Server server) { }
    
    /// <inheritdoc/>
    public virtual void OnEnable(Bootstrap.Server server) { }
    
    public virtual void OnDisable(Bootstrap.Server server) { }
}