using Yggdrasilnet.Server.Core.World;

namespace Yggdrasilnet.Server.Core.Command.Suggestion;

/// <summary>
///     Suggests the entity types loaded from the behavior packs.
/// </summary>
/// <param name="world"></param>
public sealed class EntityTypeSuggestionProvider(IWorld world) : ISuggestionProvider {
    /// <inheritdoc />
    public IEnumerable<string> GetSuggestions(CommandSource source) {
        return world.Behaviors.EntityIds;
    }
}
