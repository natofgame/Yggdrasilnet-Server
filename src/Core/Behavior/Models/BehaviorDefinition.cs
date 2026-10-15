using Yggdrasilnet.Server.Core.Behavior.Core;

namespace Yggdrasilnet.Server.Core.Behavior.Models;

public sealed class BehaviorDefinition {
    /// <summary>
    ///     The unique identifier of the behavior.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    ///     The behaviors of the behavior.
    /// </summary>
    public List<IBehavior> Behaviors { get; init; } = [];
}