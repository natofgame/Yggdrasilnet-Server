namespace Yggdrasilnet.Server.Plugin;

public sealed class Plugin(IYggdrasilPlugin plugin, PluginContext context, string path)  {
    public IYggdrasilPlugin Instance { get; } = plugin;
    public PluginContext Context { get; } = context;
    public string Path { get; } = path;
    public bool Enabled { get; set; }
}