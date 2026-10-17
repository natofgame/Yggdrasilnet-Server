using System.Reflection;

namespace Yggdrasilnet.Server.Plugin;

public abstract class YggdrasilPlugin : IYggdrasilPlugin {
    /// <summary>
    ///  The metadata of the plugin.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public PluginAttribute Metadata => field ??=
        GetType().GetCustomAttribute<PluginAttribute>()
        ?? throw new InvalidOperationException(
            $"Le plugin {GetType().FullName} doit être décoré avec [Plugin(\"id\")].");

    /// <inheritdoc/>
    public string Id => Metadata.Id;

    /// <inheritdoc/>
    public virtual void OnLoad(Bootstrap.Server server) { }
    
    /// <inheritdoc/>
    public virtual void OnEnable(Bootstrap.Server server) { }
    
    /// <inheritdoc/>
    public virtual void OnDisable(Bootstrap.Server server) { }
}