using Yggdrasilnet.Network.Enums;

namespace Yggdrasilnet.Server.Core.World.Entities.Components.Collision;

public static class CollisionLayerParser {
    public static CollisionLayer Parse(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return CollisionLayer.None;
        }

        var result = CollisionLayer.None;
        foreach (var part in value.Split('|'))
            result |= part.Trim().ToLowerInvariant() switch {
                "world" => CollisionLayer.World,
                "player" => CollisionLayer.Player,
                "monster" => CollisionLayer.Monster,
                "spell" => CollisionLayer.Spell,
                "all" => CollisionLayer.All,
                "none" => CollisionLayer.None,
                _ => throw new InvalidDataException($"Unknown collision layer '{part}'.")
            };

        return result;
    }
}