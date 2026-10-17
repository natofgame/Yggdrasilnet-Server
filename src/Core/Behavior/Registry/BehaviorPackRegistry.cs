using System.Text.Json;
using Yggdrasilnet.Server.Core.Behavior.Models;

namespace Yggdrasilnet.Server.Core.Behavior.Registry;

/// <summary>
///     Registry for behavior packs.
/// </summary>
/// <param name="entityComponents"></param>
public sealed class BehaviorPackRegistry(EntityComponentRegistry entityComponents) {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    private static readonly EntityComponentRegistry DefaultEntityComponents = EntityComponentRegistry.CreateDefault();

    private readonly EntityComponentRegistry _entityComponents =
        entityComponents ?? throw new ArgumentNullException(nameof(entityComponents));

    private readonly Dictionary<string, BehaviorDefinition> _entityDefinitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, BehaviorDefinition> _spellDefinitions = new(StringComparer.OrdinalIgnoreCase);

    public BehaviorPackRegistry()
        : this(DefaultEntityComponents) { }

    /// <summary>
    ///     The number of behavior packs loaded.
    /// </summary>
    public int PackCount { get; private set; }

    /// <summary>
    ///     The number of behaviors loaded.
    /// </summary>
    public int EntityBehaviorCount => _entityDefinitions.Count;

    /// <summary>
    ///     The number of spell behaviors loaded.
    /// </summary>
    public int SpellBehaviorCount => _spellDefinitions.Count;

    /// <summary>
    ///     The ids of the entity definitions loaded.
    /// </summary>
    public IEnumerable<string> EntityIds => _entityDefinitions.Keys;

    /// <summary>
    ///     Tries to get a behavior definition by its ID.
    /// </summary>
    /// <param name="definitionId"></param>
    /// <param name="definition"></param>
    /// <returns></returns>
    public bool TryGetEntity(string definitionId, out BehaviorDefinition definition) {
        return _entityDefinitions.TryGetValue(definitionId, out definition!);
    }

    /// <summary>
    ///     Tries to get a spell behavior definition by its ID.
    /// </summary>
    /// <param name="spellId"></param>
    /// <param name="definition"></param>
    /// <returns></returns>
    public bool TryGetSpell(string spellId, out BehaviorDefinition definition) {
        return _spellDefinitions.TryGetValue(spellId, out definition!);
    }


    public static BehaviorPackRegistry LoadFromDirectory(string rootPath) {
        var registry = new BehaviorPackRegistry();
        registry.Load(rootPath);
        return registry;
    }

    /// <summary>
    ///     Loads behavior packs from the specified directory.
    /// </summary>
    /// <param name="rootPath"></param>
    /// <exception cref="InvalidDataException"></exception>
    public void Load(string rootPath) {
        PackCount = 0;

        _entityDefinitions.Clear();
        _spellDefinitions.Clear();

        if (!Directory.Exists(rootPath)) {
            return;
        }

        foreach (var packPath in Directory.GetDirectories(rootPath)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)) {
            var manifestPath = Path.Combine(packPath, "manifest.json");
            if (!File.Exists(manifestPath)) {
                continue;
            }

            var manifest =
                JsonSerializer.Deserialize<BehaviorPackManifest>(File.ReadAllText(manifestPath), JsonOptions);
            if (manifest is null || string.IsNullOrWhiteSpace(manifest.Id)) {
                throw new InvalidDataException($"Behavior pack manifest '{manifestPath}' is invalid.");
            }

            PackCount++;

            LoadBehavior(packPath, "Entity", _entityDefinitions);
            LoadBehavior(packPath, "Spell", _spellDefinitions);
        }
    }

    /// <summary>
    ///     Loads behaviors from the specified directory.
    /// </summary>
    /// <param name="packPath"></param>
    /// <param name="name"></param>
    /// <param name="definitions"></param>
    /// <exception cref="InvalidDataException"></exception>
    private void LoadBehavior(string packPath, string name, Dictionary<string, BehaviorDefinition> definitions) {
        var folder = Path.Combine(packPath, name);
        if (!Directory.Exists(folder)) {
            return;
        }

        foreach (var file in Directory.GetFiles(folder, "*.json")
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)) {
            var dto = JsonSerializer.Deserialize<BehaviorDefinitionDto>(File.ReadAllText(file), JsonOptions);
            if (dto is null || string.IsNullOrWhiteSpace(dto.Id)) {
                throw new InvalidDataException($"Behavior file '{file}' is invalid.");
            }

            var definition = new BehaviorDefinition {
                Id = dto.Id,
                Behaviors = dto.Components
                    .Select(pair => _entityComponents.CreateBehavior(pair.Key, pair.Value, JsonOptions))
                    .ToList()
            };
            definitions[definition.Id] = definition;
        }
    }
}