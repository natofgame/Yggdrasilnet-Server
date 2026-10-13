using System.Reflection;
using Serilog;

namespace Yggdrasilnet.Server.Core.Events;

public sealed class WorldEventRuntime {
    private readonly WorldEventBus _eventBus;
    private readonly WorldEventQueue _queue = new();

    public WorldEventRuntime(EntityResolver entityResolver) {
        ArgumentNullException.ThrowIfNull(entityResolver);

        WorldEventContext? context = null;
        _eventBus = new WorldEventBus(
            () => context ?? throw new InvalidOperationException("World event context is not initialized."),
            exception => Log.Error(exception, "Unhandled world event exception"));

        context = new WorldEventContext(
            "Yggdrasilnet.Server",
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0",
            _eventBus,
            entityResolver,
            message => Log.Information("[WorldEvent] {Message}", message));

        Context = context;
    }

    public WorldEventContext Context { get; }
    public IWorldEventBus Events => _eventBus;

    public void Publish(WorldEvent worldEvent) {
        ArgumentNullException.ThrowIfNull(worldEvent);
        _queue.Enqueue(worldEvent);
        _queue.Flush(_eventBus);
    }
}
