namespace Yggdrasilnet.Server.Plugin;

public interface IYggdrasilPlugin {
    /// <summary>
    ///  The unique identifier of the plugin.
    /// </summary>
    string Id { get; }
    
    void OnLoad(Bootstrap.Server server);
    
    /// <summary>
    ///  Called when the plugin is enabled.
    /// </summary>
    /// <param name="server"></param>
    void OnEnable(Bootstrap.Server server);
    
    /// <summary>
    ///  Called when the plugin is disabled.
    /// </summary>
    /// <param name="server"></param>
    void OnDisable(Bootstrap.Server server);
}
