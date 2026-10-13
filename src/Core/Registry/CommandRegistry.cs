using Yggdrasilnet.Server.Core.Commands;
using Yggdrasilnet.Server.Core.Commands.Impl;
using Yggdrasilnet.Server.Core.World;

namespace Yggdrasilnet.Server.Core.Registry;

public static class CommandRegistry {
    public static void Registers(CommandFactory factory, IWorld world) {
        factory.Register(new SummonCommand(world));
    }
}
