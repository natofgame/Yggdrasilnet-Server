using Yggdrasilnet.Server.Core.Behavior.Core;

namespace Yggdrasilnet.Server.Core.Behavior.Models;

public static class BehaviorDefinitionExtensions {
    /// <summary>
    ///     Tries to get a behavior of the given type.
    /// </summary>
    /// <param name="definition"></param>
    /// <param name="behavior"></param>
    /// <typeparam name="TBehavior"></typeparam>
    /// <returns></returns>
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