namespace Yggdrasilnet.Server.Core.Command;

/// <summary>
///  Marks a method as a command.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CommandAttribute : Attribute {
    public CommandAttribute(string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("Command name cannot be empty.", nameof(name));
        }

        Name = name;
    }

    /// <summary>
    ///  The name of the command.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    ///  The permission level required to execute the command.
    /// </summary>
    public int Permission { get; init; } = 0;
}
