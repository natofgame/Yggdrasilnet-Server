using System.Numerics;

namespace Yggdrasilnet.Server.Core.Command.Commands;

public class SummonCommand : ICommand {
    /// <summary>
    ///     Summons an entity.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="entityType"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    [Command("summon", Permission = 2)]
    public void Summon(CommandSource source, string entityType, float x = 0, float y = 0, float z = 0) {
        var handle = source.Dimension.Spawn(entityType, new Vector3(x, y, z));
        source.SendFeedback($"Summoned {entityType} at ({x}, {y}, {z}) with id {handle.UniqueId}.");
    }
}