using System.Text.Json;

namespace Yggdrasilnet.Server.Core.Behavior.Models;

public sealed class BehaviorDefinitionDto {
    /// <summary>
    ///     The unique identifier of the behavior.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     The components of the behavior.
    /// </summary>
    public Dictionary<string, JsonElement> Components { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}