namespace Yggdrasilnet.Server.Core.Commands;

[AttributeUsage(AttributeTargets.Method)]
public sealed class CommandAttribute : Attribute {
    public CommandAttribute(string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("Command name cannot be empty.", nameof(name));
        }

        Name = name;
    }

    public string Name { get; }
    public int Permission { get; init; } = 0;
}
