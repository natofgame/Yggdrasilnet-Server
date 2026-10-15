namespace Yggdrasilnet.Server.Core.Events;

internal sealed class WorldEventQueue {
    private readonly Queue<WorldEvent> _queue = new();
    private bool _flushing;

    public void Enqueue(WorldEvent worldEvent) {
        _queue.Enqueue(worldEvent);
    }

    public void Flush(WorldEventBus bus) {
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