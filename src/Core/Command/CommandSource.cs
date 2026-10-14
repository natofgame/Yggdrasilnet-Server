using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.Command;

/// <summary>
///  Represents the source of a command.
/// </summary>
public class CommandSource {
    /// <summary>
    ///  The entity ID of the executor.
    /// </summary>
    public int ExecutorEntityId { get; init; } = -1;
    
    /// <summary>
    ///  The dimension the executor is in.
    /// </summary>
    public IDimension Dimension { get; init; } = null!;
    
    /// <summary>
    ///  The permission level of the executor.
    /// </summary>
    public int PermissionLevel { get; init; }
    
    /// <summary>
    ///  The action to send feedback to the executor.
    /// </summary>
    public Action<string> SendFeedback { get; init; } = _ => { };
}
