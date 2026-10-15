namespace Yggdrasilnet.Server.Core.Events;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SubscribeWorldEventAttribute : Attribute {
    public SubscribeWorldEventAttribute(EventPriority priority = EventPriority.Normal) {
        Priority = priority;
    }

    public EventPriority Priority { get; }
}