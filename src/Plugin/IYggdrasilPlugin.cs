namespace Yggdrasilnet.Server.Plugin;

public interface IYggdrasilPlugin {
    string Id { get; }
    void OnLoad(Bootstrap.Server server);
    void OnEnable(Bootstrap.Server server);
    void OnDisable(Bootstrap.Server server);
}
