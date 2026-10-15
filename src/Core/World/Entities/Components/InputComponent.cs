using System.Numerics;
using Yggdrasilnet.Server.Core.Components;

namespace Yggdrasilnet.Server.Core.World.Entities.Components;

public class InputComponent : IComponent {
    public Vector2 Direction;
    public float X;
    public float Z;
}