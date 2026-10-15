using System.Collections.Concurrent;
using Serilog;

namespace Yggdrasilnet.Server.Event;

public sealed class EventManager {
    private readonly EventBus _eventBus;
    private readonly ConcurrentQueue<IEvent> _queue = new();
    private int _flushing;

    public EventManager() {
        _eventBus = new(eventException => Log.Error(eventException, "Event handler threw an exception"));
    }

    /// <summary>
    /// Registers all event handlers declared on the given listener.
    /// </summary>
    /// <param name="listener"></param>
    public void Register(object listener) {
        _eventBus.Register(listener);
    }

    /// <summary>
    /// Unregisters all event handlers declared on the given listener.
    /// </summary>
    /// <param name="listener"></param>
    public void Unregister(object listener) {
        _eventBus.Unregister(listener);
    }

    /// <summary>
    /// Publishes an event.
    /// </summary>
    /// <param name="event"></param>
    public void Publish(IEvent @event) {
        ArgumentNullException.ThrowIfNull(@event);
        _queue.Enqueue(@event);
        Flush();
    }

    /// <summary>
    /// Flushes the event queue.
    /// </summary>
    private void Flush() {
        if (Interlocked.Exchange(ref _flushing, 1) == 1) {
            return;
        }

        try {
            while (_queue.TryDequeue(out var @event))
                _eventBus.Post(@event);
        }
        finally {
            Interlocked.Exchange(ref _flushing, 0);
        }
    }
}
