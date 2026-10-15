namespace Yggdrasilnet.Server.Core.Systems;

/// <summary>
///     The phase of the world system.
/// </summary>
public enum SystemPhase {
    Input = 0,
    Ai = 1,
    Physics = 2,
    Combat = 3,
    Cleanup = 4
}