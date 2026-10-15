using System.Reflection;

namespace Yggdrasilnet.Server.Event;

public sealed class EventBus {
    private readonly Action<Exception> _onError;

    private readonly Dictionary<Type, List<Subscription>> _subscriptions = new();

    public EventBus(Action<Exception> onError) {
        _onError = onError;
    }

    public void Register(object listener) {
        var type = listener.GetType();
        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var touchedLists = new HashSet<List<Subscription>>();

        foreach (var method in methods) {
            var attribute = method.GetCustomAttribute<SubscribeEventAttribute>();
            if (attribute == null) {
                continue;
            }

            var parameters = method.GetParameters();
            if (parameters.Length != 1 || !typeof(IEvent).IsAssignableFrom(parameters[0].ParameterType)) {
                _onError(new InvalidOperationException(
                    $"{type.Name}.{method.Name}: invalid signature for [SubscribeEvent] " +
                    "(expected: void Method(XxxEvent e))"));
                continue;
            }

            var eventType = parameters[0].ParameterType;
            var invoker = BuildInvoker(listener, method, eventType);

            if (!_subscriptions.TryGetValue(eventType, out var list)) {
                list = new List<Subscription>();
                _subscriptions[eventType] = list;
            }

            list.Add(new Subscription(listener, attribute.Priority, invoker));
            touchedLists.Add(list);
        }

        foreach (var list in touchedLists) {
            list.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }
    }

    public void Unregister(object listener) {
        foreach (var list in _subscriptions.Values)
            list.RemoveAll(subscription => ReferenceEquals(subscription.Listener, listener));
    }

    public void Post(IEvent @event) {
        if (!_subscriptions.TryGetValue(@event.GetType(), out var list) || list.Count == 0) {
            return;
        }

        var snapshot = list.ToArray();
        foreach (var subscription in snapshot) {
            try {
                subscription.Invoke(@event);
            }
            catch (Exception exception) {
                _onError(exception);
            }
        }
    }

    private static Action<IEvent> BuildInvoker(object listener, MethodInfo method, Type eventType) {
        var factory = typeof(EventBus)
            .GetMethod(nameof(CreateTypedInvoker), BindingFlags.NonPublic | BindingFlags.Static)?
            .MakeGenericMethod(eventType);

        if (factory == null) {
            throw new InvalidOperationException("Unable to create event invoker factory.");
        }

        return (Action<IEvent>)factory.Invoke(null, [listener, method])!;
    }

    private static Action<IEvent> CreateTypedInvoker<TEvent>(object listener, MethodInfo method)
        where TEvent : IEvent {
        var typed = (Action<TEvent>)Delegate.CreateDelegate(typeof(Action<TEvent>), listener, method);
        return worldEvent => typed((TEvent)worldEvent);
    }

    private sealed class Subscription {
        public readonly Action<IEvent> Invoke;
        public readonly object Listener;
        public readonly EventPriority Priority;

        public Subscription(object listener, EventPriority priority, Action<IEvent> invoke) {
            Listener = listener;
            Priority = priority;
            Invoke = invoke;
        }
    }
}