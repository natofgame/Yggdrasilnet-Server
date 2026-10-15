using Yggdrasilnet.Server.Core.Command.Commands;
using Yggdrasilnet.Server.Core.Commands;
using Yggdrasilnet.Server.Core.World;

namespace Yggdrasilnet.Server.Core.Registry;

/// <summary>
///     Registry for commands.
/// </summary>
public static class CommandRegistry {
    /// <summary>
    ///     Registers all commands.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="world"></param>
    public static void Registers(CommandFactory factory, IWorld world) {
        factory.Register(new SummonCommand());
    }
}