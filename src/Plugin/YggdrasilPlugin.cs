using System.Reflection;

namespace Yggdrasilnet.Server.Plugin;

public abstract class YggdrasilPlugin : IYggdrasilPlugin {
    private PluginAttribute? _metadata;

    public PluginAttribute Metadata => _metadata ??=
        GetType().GetCustomAttribute<PluginAttribute>()
        ?? throw new InvalidOperationException(
            $"Le plugin {GetType().FullName} doit être décoré avec [Plugin(\"id\")].");

    public string Id => Metadata.Id;

    public virtual void OnLoad(Bootstrap.Server server) { }
    public virtual void OnEnable(Bootstrap.Server server) { }
    public virtual void OnDisable(Bootstrap.Server server) { }
}