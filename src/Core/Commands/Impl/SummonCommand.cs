using System.Numerics;
using Yggdrasilnet.Server.Core.World;

namespace Yggdrasilnet.Server.Core.Commands.Impl;

public class SummonCommand : ICommand {
    private readonly IWorld _world;

    public SummonCommand(IWorld world) {
        _world = world ?? throw new ArgumentNullException(nameof(world));
    }

    [Command("summon", Permission = 2)]
    public void Summon(CommandSource source, string entityType, float x = 0, float y = 0, float z = 0) {
        var handle = source.Dimension.Spawn(entityType, new Vector3(x, y, z));
        source.SendFeedback($"Summoned {entityType} at ({x}, {y}, {z}) with id {handle.UniqueId}.");
    }
}
