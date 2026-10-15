using Yggdrasilnet.Server.Core;
using Yggdrasilnet.Server.Core.Command;

namespace Yggdrasilnet.Server.Bootstrap;

public interface IServer {
    Simulation Simulation { get; }
}