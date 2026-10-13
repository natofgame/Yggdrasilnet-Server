using System.Text.Json;

namespace Yggdrasilnet.Server.Core.Behavior.Models;

public sealed class BehaviorDefinitionDto {
    public string Id { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> Components { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
