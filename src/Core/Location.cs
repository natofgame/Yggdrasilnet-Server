using System.Numerics;
using Yggdrasilnet.Server.Core.World.Dimensions;

namespace Yggdrasilnet.Server.Core;

public readonly record struct Location(IDimension Dimension, Vector3 Position);
