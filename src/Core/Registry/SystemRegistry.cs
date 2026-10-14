using Yggdrasilnet.Server.Core.Behavior.Registry;
using Yggdrasilnet.Server.Core.Events;
using Yggdrasilnet.Server.Core.Systems;
using Yggdrasilnet.Server.Core.Systems.Ai;
using Yggdrasilnet.Server.Core.Systems.Combat;
using Yggdrasilnet.Server.Core.Systems.Physics;
using Yggdrasilnet.Server.Core.Systems.Physics.Collision;

namespace Yggdrasilnet.Server.Core.Registry;

/// <summary>
///  Registry for systems.
/// </summary>
public static class SystemRegistry {
    /// <summary>
    ///  Registers all systems.
    /// </summary>
    /// <param name="systems"></param>
    /// <param name="behaviors"></param>
    /// <param name="events"></param>
    public static void Registers(ref SystemPipeline systems, BehaviorPackRegistry behaviors, IWorldEventBus events) {
        systems.Add(new AiSystem());
        systems.Add(new MovementSystem());
        systems.Add(new CollisionSystem());

        var spellCastSystem = new SpellCastSystem(behaviors);
        events.Register(spellCastSystem);
        systems.Add(new TargetingSystem());
        systems.Add(spellCastSystem);

        systems.Add(new ProjectileSystem(behaviors));
    }
}
