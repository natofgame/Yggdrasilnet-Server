namespace Yggdrasilnet.Server.Core.Commands;

/// <summary>
///     Marker interface for command classes. Instances must be explicitly passed to
///     CommandRegistry.Register(...) � nothing is auto-discovered, so plugins fully
///     control what they expose and when (e.g. unregister on unload).
/// </summary>
public interface ICommand { }
