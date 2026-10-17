namespace Yggdrasilnet.Server.Network.Session;

/// <summary>
///     Identity of a connected player.
/// </summary>
public sealed record PlayerProfile(Guid Id, string Name);