using System.Numerics;
using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core.World.Entities;

public class Player : Entity {
    public Player(Entity entity) : base(entity.Id, entity.Position, entity.Dimension, entity.UniqueId) { }
}