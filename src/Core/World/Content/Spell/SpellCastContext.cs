using System.Numerics;
using Yggdrasilnet.NetworkV4.Enums;

namespace Yggdrasilnet.Server.Core.World.Content.Spell;

public sealed record SpellCastContext(
    int OwnerId,
    string SpellId,
    SpellType SpellType,
    Vector3 Direction,
    Vector3 Origin);
