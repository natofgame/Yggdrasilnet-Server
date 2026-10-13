using Yggdrasilnet.Server.Core.Behavior.Core;

namespace Yggdrasilnet.Server.Core.Behavior.Models;

public static class BehaviorDefinitionExtensions {
    public static bool TryGetBehavior<TBehavior>(this BehaviorDefinition definition, out TBehavior behavior)
        where TBehavior : class, IBehavior {
        foreach (var entry in definition.Behaviors)
            if (entry is TBehavior typed) {
                behavior = typed;
                return true;
            }

        behavior = null!;
        return false;
    }
}
