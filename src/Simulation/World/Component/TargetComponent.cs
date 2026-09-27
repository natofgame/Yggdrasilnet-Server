using System.Numerics;

namespace Yggdrasilnet.Server.Simulation.World.Component;

public class TargetComponent : Network.Packet.Snapshot.Components.TargetComponent, IComponent {
    public float Range = 20f;
    public float RefreshInterval = 0.2f;
    public float RefreshTimer;
}