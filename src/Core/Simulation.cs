using System.Collections.Concurrent;
using LiteNetLib;
using Serilog;
using Yggdrasilnet.Network.Chat;
using Yggdrasilnet.Network.Protocol;
using Yggdrasilnet.Server.Configuration;
using Yggdrasilnet.Server.Core.Behavior.Registry;
using Yggdrasilnet.Server.Core.Command;
using Yggdrasilnet.Server.Core.Registry;
using Yggdrasilnet.Server.Core.Services;
using Yggdrasilnet.Server.Core.Systems;
using Yggdrasilnet.Server.Core.World;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Event.Events.Player;
using Yggdrasilnet.Server.Network;
using Yggdrasilnet.Server.Network.Handler;
using Yggdrasilnet.Server.Network.Services;
using Yggdrasilnet.Server.Network.Session;
using Yggdrasilnet.Server.Network.Snapshot;

namespace Yggdrasilnet.Server.Core;

/// <summary>
///     The simulation.
/// </summary>
public sealed class Simulation {
    private readonly ConcurrentQueue<IServerEvent> _events;
    private readonly PlayerService _players;
    private readonly SessionRegistry _sessions = new();

    private readonly SnapshotBroadcaster _snapshotBroadcaster;
 
    public Simulation(Bootstrap.Server server, ConcurrentQueue<IServerEvent> events, ServerTransport transport, int tickRate,
        SnapshotConfiguration snapshotConfiguration) {
        _events = events;

        World = CreateWorld();

        Server = server;
        Network = new NetworkService(transport);
        EntityRegistry = new EntityRegistry(this);
        _players = new PlayerService(Server, World, Network, EntityRegistry);
        Chat = new ChatService(Network, _sessions);

        _snapshotBroadcaster = new SnapshotBroadcaster(_sessions, World, this, new SnapshotBuilder(EntityRegistry),
            snapshotConfiguration, tickRate);
        CommandRegistry.Registers(CommandFactory, World);
    }

    /// <summary>
    ///     The command factory.
    /// </summary>
    public CommandFactory CommandFactory { get; } = new();

    /// <summary>
    ///     The chat service.
    /// </summary>
    public ChatService Chat { get; }

    /// <summary>
    ///     The entity registry.
    /// </summary>
    public EntityRegistry EntityRegistry { get; }

    /// <summary>
    ///     The network service.
    /// </summary>
    public NetworkService Network { get; }

    /// <summary>
    ///     The current tick.
    /// </summary>
    public long Tick { get; private set; }
    
    /// <summary>
    ///  The server.
    /// </summary>
    public Bootstrap.Server Server { get; }

    /// <summary>
    ///     The world the simulation is running in.
    /// </summary>
    public IWorld World { get; }

    /// <summary>
    ///     Updates the simulation.
    /// </summary>
    /// <param name="dt"></param>
    public void Update(float dt) {
        Tick++;

        DrainEvents();
        World.Update(dt);
        _snapshotBroadcaster.BroadcastSnapshots(Tick);
    }

    /// <summary>
    ///     Drains all events from the queue.
    /// </summary>
    private void DrainEvents() {
        while (_events.TryDequeue(out var serverEvent))
            switch (serverEvent) {
                case PeerConnectedEvent connected:
                    OnPeerConnected(connected.Peer);
                    break;
                case PeerDisconnectedEvent disconnected:
                    OnPeerDisconnected(disconnected.Peer, disconnected.Info);
                    break;
                case PacketReceivedEvent packet:
                    OnPacketReceived(packet.Peer, packet.Payload);
                    break;
                case ConsoleCommandEvent consoleCommand:
                    OnConsoleCommand(consoleCommand.Input);
                    break;
            }
    }

    /// <summary>
    ///     Executes the given command.
    /// </summary>
    /// <param name="input"></param>
    private void OnConsoleCommand(string input) {
        var source = new CommandSource {
            Dimension = World.MainDimension,
            PermissionLevel = int.MaxValue,
            SendFeedback = message => Log.Information("{Message}", ChatFormatting.StripCodes(message))
        };

        CommandFactory.Execute(input, source);
    }

    /// <summary>
    ///     Creates a new session for the given peer.
    /// </summary>
    /// <param name="peer"></param>
    private void OnPeerConnected(NetPeer peer) {
        var protocol = ProtocolRegistry.GetHighestProtocol();
        var session = new GameSession(peer, protocol);
        session.SetHandler(new HandshakeHandler(this, session, Network, _players));
        _sessions.Add(session);

        Log.Information("Peer connected: {EndPoint}", peer.Address);
    }

    /// <summary>
    ///     Disconnects the given peer.
    /// </summary>
    /// <param name="peer"></param>
    /// <param name="info"></param>
    private void OnPeerDisconnected(NetPeer peer, DisconnectInfo info) {
        if (!_sessions.Remove(peer, out var session) || session is null) {
            return;
        }

        _players.LeaveGame(session);

        Log.Information("Peer disconnected: {EndPoint} ({Reason})", peer.Address, info.Reason);
    }

    /// <summary>
    ///     Dispatches a packet to the appropriate handler.
    /// </summary>
    /// <param name="peer"></param>
    /// <param name="payload"></param>
    private void OnPacketReceived(NetPeer peer, byte[] payload) {
        if (!_sessions.TryGet(peer, out var session) || session?.Handler is null) {
            return;
        }

        if (!session.Codec.TryDecode(payload, out var packet) || packet is null) {
            Log.Warning("Malformed packet from {EndPoint} ({Bytes} bytes)", peer.Address, payload.Length);
            return;
        }

        try {
            Network.DispatchPacket(session.Handler, packet);
        }
        catch (Exception exception) {
            Log.Error(exception, "Packet handling error for {EndPoint}", peer.Address);
            Network.Disconnect(session);
        }
    }
    
    private World.World CreateWorld() {
        var entityComponents = EntityComponentRegistry.CreateDefault();
        var behaviorRegistry = new BehaviorPackRegistry(entityComponents);
        var behaviorRoot = Path.Combine(AppContext.BaseDirectory, "BehaviorPack");
        behaviorRegistry.Load(behaviorRoot);
        Log.Information(
            "[Behavior] Behavior packs loaded:{PackCount} ({EntityBehaviorCount} entity, {SpellBehaviorCount} spells)",
            behaviorRegistry.PackCount,
            behaviorRegistry.EntityBehaviorCount,
            behaviorRegistry.SpellBehaviorCount);

        var systems = new SystemPipeline();
        SystemRegistry.Registers(ref systems, behaviorRegistry);

        var world = new World.World(this, behaviorRegistry);
        world.CreateDimension("overworld", systems, true);

        return world;
    }
}