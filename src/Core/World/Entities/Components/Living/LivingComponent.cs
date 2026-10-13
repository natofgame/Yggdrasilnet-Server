using Yggdrasilnet.Server.Core.Components;

namespace Yggdrasilnet.Server.Core.World.Entities.Components.Living;

public class LivingComponent : IComponent {
    public float Current;
    public float DespawnTimer;
    public float Max;
    public LifeState State;

    public bool IsAlive => State == LifeState.Alive && float.IsFinite(Current) && Current > 0f;
}
