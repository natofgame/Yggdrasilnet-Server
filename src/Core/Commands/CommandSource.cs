using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.Commands;

public class CommandSource {
    public int ExecutorEntityId { get; init; } = -1;
    public IDimension Dimension { get; init; } = null!;
    public int PermissionLevel { get; init; }
    public Action<string> SendFeedback { get; init; } = _ => { };
}
