using System.Numerics;
using Yggdrasilnet.Server.Core.World.Entities;

namespace Yggdrasilnet.Server.Core.Systems.Physics.Steering;

internal readonly record struct PlayerTarget(Entity Entity, Vector2 Position);