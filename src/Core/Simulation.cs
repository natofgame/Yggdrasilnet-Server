using System.Collections.Concurrent;
using LiteNetLib;
using Serilog;
using Yggdrasilnet.NetworkV4.Protocol;
using Yggdrasilnet.Server.Core.Behavior.Registry;
using Yggdrasilnet.Server.Core.Commands;
using Yggdrasilnet.Server.Core.Events;
using Yggdrasilnet.Server.Core.Events.Events;
using Yggdrasilnet.Server.Core.Registry;
using Yggdrasilnet.Server.Core.Services;
using Yggdrasilnet.Server.Core.Systems;
using Yggdrasilnet.Server.Core.World;
using Yggdrasilnet.Server.Core.World.Dimensions;
using Yggdrasilnet.Server.Core.World.Entities;
using Yggdrasilnet.Server.Network;
using Yggdrasilnet.Server.Network.Handler;
using Yggdrasilnet.Server.Network.Services;
using Yggdrasilnet.Server.Network.Session;
using Yggdrasilnet.Server.Network.Snapshot;

namespace Yggdrasilnet.Server.Core;

public sealed class Simulation {
    private readonly ConcurrentQueue<IServerEvent> _events;

    private readonly PlayerService _players;
    private readonly ServerTransport _server;

    private readonly SessionRegistry _sessions = new();

    private readonly SnapshotBroadcaster _snapshotBroadcaster;
    private readonly WorldEventRuntime _worldEvents;

    public Simulation(ConcurrentQueue<IServerEvent> events, ServerTransport server, int tickRate) {
        _events = events;
        _server = server;

        _worldEvents = new WorldEventRuntime(ResolveEntity);
        World = CreateWorld(_worldEvents.Events, _worldEvents.Publish);

        Network = new NetworkService(_server);
        EntityRegistry = new EntityRegistry(this);
        _players = new PlayerService(World, _sessions, Network, EntityRegistry);

        _snapshotBroadcaster = new SnapshotBroadcaster(_sessions, World, this, tickRate);
        InitRegistry();
    }


    public CommandFactory CommandFactory { get; } = new();

    public EntityRegistry EntityRegistry { get; }

    public NetworkService Network { get; }

    public long Tick { get; private set; }
    public IWorld World { get; }

    public IWorldEventBus Events => _worldEvents.Events;

    public bool TryGetWorld(string name, out IDimension? world) {
        return World.TryGetDimension(name, out world);
    }

    public void Publish(WorldEvent worldEvent) {
        _worldEvents.Publish(worldEvent);
    }

    private bool ResolveEntity(int entityId, out Entity? entity) {
        return World.TryResolveEntity(entityId, out entity);
    }

    public void Update(float dt) {
        Tick++;

        DrainEvents();
        World.Update(dt);
        _snapshotBroadcaster.BroadcastSnapshots(Tick);
        _players.SyncSessionsFromWorld();
    }

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

    private void OnConsoleCommand(string input) {
        var source = new CommandSource {
            Dimension = World.MainDimension,
            PermissionLevel = int.MaxValue,
            SendFeedback = message => Log.Information("{Message}", message)
        };

        CommandFactory.Execute(input, source);
    }

    private void OnPeerConnected(NetPeer peer) {
        var protocol = ProtocolRegistry.GetHighestProtocol();
        var session = new GameSession(peer, protocol);
        session.SetHandler(new HandshakeHandler(this, session, Network, _players));
        _sessions.Add(session);

        Log.Information("Peer connected: {EndPoint}", peer.Address);
    }

    private void OnPeerDisconnected(NetPeer peer, DisconnectInfo info) {
        if (!_sessions.Remove(peer, out var session) || session is null) {
            return;
        }

        if (session.TryGetEntityBinding(out var entityId, out var dimensionName)) {
            _worldEvents.Publish(new PlayerDeconnexionEvent(
                peer.Address.ToString(),
                entityId,
                info.Reason.ToString()));

            World.TryDespawn(dimensionName, entityId);
            session.ClearEntityBinding();
        }

        Log.Information("Peer disconnected: {EndPoint} ({Reason})", peer.Address, info.Reason);
    }

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

    private void InitRegistry() {
        CommandRegistry.Registers(CommandFactory, World);
    }

    private static World.World CreateWorld(IWorldEventBus events, Action<WorldEvent> publishEvent) {
        var entityComponents = EntityComponentRegistry.CreateDefault();
        var behaviorRegistry = new BehaviorPackRegistry(entityComponents);
        var behaviorRoot = Path.Combine(AppContext.BaseDirectory, "BehaviorPack");
        behaviorRegistry.Load(behaviorRoot);
        Log.Information(
            "Behavior packs loaded: {PackCount} ({EntityBehaviorCount} entity, {SpellBehaviorCount} spells)",
            behaviorRegistry.PackCount,
            behaviorRegistry.EntityBehaviorCount,
            behaviorRegistry.SpellBehaviorCount);

        var systems = new SystemPipeline();
        SystemRegistry.Registers(ref systems, behaviorRegistry, events);

        var world = new World.World(events, behaviorRegistry);
        world.CreateDimension("overworld", systems, true);

        return world;
    }
}
