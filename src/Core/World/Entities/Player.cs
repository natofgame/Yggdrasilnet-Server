using System.Numerics;
using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.World.Entities;

public class Player : Entity {
    public Player(string id, Vector3 position, IDimension? dimension = null) : base(id, position, dimension) { }
}