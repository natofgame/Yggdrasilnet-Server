namespace Yggdrasilnet.Server.Event;

public sealed class EventManager {
    private readonly EventBus _eventBus = new(_ => { });
    private readonly Queue<IEvent> _queue = new();
    private bool _flushing;

    /// <summary>
    ///  Publishes an event.
    /// </summary>
    /// <param name="event"></param>
    public void Publish(IEvent @event) {
        ArgumentNullException.ThrowIfNull(@event);
        _queue.Enqueue(@event);
        Flush(_eventBus);
    }

    /// <summary>
    /// Flushes the event queue.
    /// </summary>
    /// <param name="bus"></param>
    private void Flush(EventBus bus) {
        if (_flushing) {
            return;
        }

        _flushing = true;
        try {
            while (_queue.Count > 0)
                bus.Post(_queue.Dequeue());
        }
        finally {
            _flushing = false;
        }
    }
}