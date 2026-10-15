namespace Yggdrasilnet.Server.Event;

/// <summary>
///  Marks a method as a subscriber to an event.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SubscribeEventAttribute(EventPriority priority = EventPriority.Normal) : Attribute {
    /// <summary>
    ///  The priority of this event.
    /// </summary>
    public EventPriority Priority { get; } = priority;
}