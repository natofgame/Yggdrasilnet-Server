namespace Yggdrasilnet.Server.Plugin;

public sealed class Plugin(IYggdrasilPlugin plugin, PluginContext context, string path)  {
    /// <summary>
    ///  The instance of the plugin.
    /// </summary>
    public IYggdrasilPlugin Instance { get; } = plugin;
    
    /// <summary>
    ///  The context of the plugin.
    /// </summary>
    public PluginContext Context { get; } = context;
    
    /// <summary>
    ///  The path to the plugin.
    /// </summary>
    public string Path { get; } = path;
    
    /// <summary>
    ///  Whether the plugin is enabled.
    /// </summary>
    public bool Enabled { get; set; }
}