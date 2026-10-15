namespace Yggdrasilnet.Server.Plugin;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PluginAttribute : Attribute {
    public string Id { get; }
    
    public string Name { get; set; }
    
    public string Version { get; set; }
    
    public string Author { get; set; }

    public PluginAttribute(string id, string version, string author = "Unknown") {
        Id = id;
        Author = author;
        Version = version;
        Name = id;
    }
}