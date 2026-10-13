using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.Systems;

public interface IWorldSystem {
    string Id { get; }
    WorldSystemPhase Phase { get; }
    void Update(Dimension dimension, float dt);
}
