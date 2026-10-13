using System.Numerics;
using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.Behavior.Core;

public interface IBehavior {
    void Apply(Dimension dimension, int entityId, Vector3 spawnPosition);
}
