using Yggdrasilnet.Server.Core.Behavior.Core;

namespace Yggdrasilnet.Server.Core.Behavior.Models;

public sealed class BehaviorDefinition {
    public string Id { get; set; } = string.Empty;
    public List<IBehavior> Behaviors { get; set; } = [];
}
