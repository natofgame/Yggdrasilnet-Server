namespace Yggdrasilnet.Server.Plugin;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PluginAttribute : Attribute {
    /// <summary>
    ///  The unique identifier of the plugin.
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    ///  The name of the plugin.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    ///  The version of the plugin.
    /// </summary>
    public string Version { get; set; }
    
    /// <summary>
    ///  The author of the plugin.
    /// </summary>
    public string Author { get; set; }

    /// <summary>
    ///  The minimum Yggdrasil server version required by the plugin (e.g. "0.0.1").
    ///  Null means the plugin does not declare any requirement.
    /// </summary>
    public string? YggdrasilVersion { get; set; }

    /// <summary>
    ///  Creates a new plugin attribute.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="version"></param>
    /// <param name="author"></param>
    /// <param name="yggdrasilVersion">The minimum Yggdrasil server version required.</param>
    public PluginAttribute(string id, string version, string author = "Unknown", string? yggdrasilVersion = null) {
        Id = id;
        Author = author;
        Version = version;
        Name = id;
        YggdrasilVersion = yggdrasilVersion;
    }
}