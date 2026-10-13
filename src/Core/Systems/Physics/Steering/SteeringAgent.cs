using System.Numerics;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Core.World.Entities.Components.Movement;

namespace Yggdrasilnet.Server.Core.Systems.Physics.Steering;

internal readonly record struct SteeringAgent(Entity Entity, MovementComponent Movement, Vector2 Position);
