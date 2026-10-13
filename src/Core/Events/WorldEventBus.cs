using System.Reflection;

namespace Yggdrasilnet.Server.Core.Events;

public sealed class WorldEventBus : IWorldEventBus {
    private readonly Func<WorldEventContext> _contextProvider;
    private readonly Action<Exception> _onError;

    private readonly Dictionary<Type, List<Subscription>> _subscriptions = new();

    public WorldEventBus(Func<WorldEventContext> contextProvider, Action<Exception> onError) {
        _contextProvider = contextProvider;
        _onError = onError;
    }

    public void Register(object listener) {
        var type = listener.GetType();
        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var method in methods) {
            var attribute = method.GetCustomAttribute<SubscribeWorldEventAttribute>();
            if (attribute == null) {
                continue;
            }

            var parameters = method.GetParameters();
            if (parameters.Length != 1 || !typeof(WorldEvent).IsAssignableFrom(parameters[0].ParameterType)) {
                _onError(new InvalidOperationException(
                    $"{type.Name}.{method.Name}: invalid signature for [SubscribeWorldEvent] " +
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
            list.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }
    }

    public void Unregister(object listener) {
        foreach (var list in _subscriptions.Values)
            list.RemoveAll(subscription => ReferenceEquals(subscription.Listener, listener));
    }

    public void Post(WorldEvent worldEvent) {
        worldEvent.Context = _contextProvider();

        if (worldEvent is EntityEvent entityEvent && !entityEvent.TryResolve()) {
            _onError(new InvalidOperationException(
                $"Unable to resolve entity {entityEvent.EntityId} for event {worldEvent.GetType().Name}."));
            return;
        }

        if (!_subscriptions.TryGetValue(worldEvent.GetType(), out var list) || list.Count == 0) {
            return;
        }

        var snapshot = list.ToArray();
        for (var i = 0; i < snapshot.Length; i++)
            try {
                snapshot[i].Invoke(worldEvent);
            }
            catch (Exception exception) {
                _onError(exception);
            }
    }

    private static Action<WorldEvent> BuildInvoker(object listener, MethodInfo method, Type eventType) {
        var factory = typeof(WorldEventBus)
            .GetMethod(nameof(CreateTypedInvoker), BindingFlags.NonPublic | BindingFlags.Static)?
            .MakeGenericMethod(eventType);

        if (factory == null) {
            throw new InvalidOperationException("Unable to create event invoker factory.");
        }

        return (Action<WorldEvent>)factory.Invoke(null, [listener, method])!;
    }

    private static Action<WorldEvent> CreateTypedInvoker<TEvent>(object listener, MethodInfo method)
        where TEvent : WorldEvent {
        var typed = (Action<TEvent>)Delegate.CreateDelegate(typeof(Action<TEvent>), listener, method);
        return worldEvent => typed((TEvent)worldEvent);
    }

    private sealed class Subscription {
        public readonly Action<WorldEvent> Invoke;
        public readonly object Listener;
        public readonly EventPriority Priority;

        public Subscription(object listener, EventPriority priority, Action<WorldEvent> invoke) {
            Listener = listener;
            Priority = priority;
            Invoke = invoke;
        }
    }
}
