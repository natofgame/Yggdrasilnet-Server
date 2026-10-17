using System.Reflection;
using Yggdrasilnet.Network.Chat;
using Yggdrasilnet.Server.Core.Command.Suggestion;

namespace Yggdrasilnet.Server.Core.Command;

public class CommandFactory {
    private const int MaxSuggestions = 50;

    private readonly ArgumentBinderRegistry _binders = new();

    private readonly Dictionary<string, Entry> _commands = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<Type, ISuggestionProvider> _suggestionProviders = new();

    public void RegisterBinder<T>(IArgumentBinder<T> binder) {
        _binders.Add(binder);
    }

    /// <summary>
    ///     Registers a provider used by the parameters marked with <see cref="SuggestAttribute" />.
    /// </summary>
    /// <param name="provider"></param>
    public void RegisterSuggestionProvider(ISuggestionProvider provider) {
        _suggestionProviders[provider.GetType()] = provider;
    }

    public void UnregisterSuggestionProvider(ISuggestionProvider provider) {
        if (_suggestionProviders.TryGetValue(provider.GetType(), out var registered) && ReferenceEquals(registered, provider)) {
            _suggestionProviders.Remove(provider.GetType());
        }
    }

    public void Register(ICommand command) {
        foreach (var method in command.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
            var attr = method.GetCustomAttribute<CommandAttribute>();
            if (attr is null) {
                continue;
            }

            _commands[attr.Name] = new Entry(command, method, attr);
        }
    }

    public void Unregister(string name) {
        _commands.Remove(name);
    }

    public void UnregisterAll(ICommand command) {
        foreach (var key in _commands.Where(kv => ReferenceEquals(kv.Value.Instance, command)).Select(kv => kv.Key)
                     .ToList())
            _commands.Remove(key);
    }

    public void Execute(string input, CommandSource source) {
        var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) {
            return;
        }

        if (!_commands.TryGetValue(tokens[0], out var entry)) {
            source.SendFeedback($"{ChatFormatting.Red}Unknown command: {tokens[0]}");
            return;
        }

        if (source.PermissionLevel < entry.Attr.Permission) {
            source.SendFeedback($"{ChatFormatting.Red}Insufficient permissions.");
            return;
        }

        if (!TryBindArguments(entry.Method, tokens, source, out var args, out var error)) {
            source.SendFeedback($"{ChatFormatting.Red}{error ?? "Invalid arguments."}");
            return;
        }

        entry.Method.Invoke(entry.Instance, args);
    }

    /// <summary>
    ///     Completions for a command line being typed (without its leading "/"): the command names first,
    ///     then the values of the argument under the cursor (end of the input).
    /// </summary>
    /// <param name="input"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    public CommandSuggestions Suggest(string input, CommandSource source) {
        var start = input.LastIndexOf(' ') + 1;
        var prefix = input[start..];
        var typedArgs = input[..start].Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (typedArgs.Length == 0) {
            var names = _commands
                .Where(kv => source.PermissionLevel >= kv.Value.Attr.Permission)
                .Select(kv => kv.Key);
            return new CommandSuggestions(start, Filter(names, prefix), string.Empty);
        }

        if (!_commands.TryGetValue(typedArgs[0], out var entry) || source.PermissionLevel < entry.Attr.Permission) {
            return CommandSuggestions.Empty with { Start = start };
        }

        var parameters = entry.Method.GetParameters()
            .Where(p => p.ParameterType != typeof(CommandSource))
            .ToArray();

        var argIndex = typedArgs.Length - 1;
        if (argIndex >= parameters.Length) {
            return CommandSuggestions.Empty with { Start = start };
        }

        var usage = string.Join(' ', parameters.Skip(argIndex).Select(FormatUsage));
        return new CommandSuggestions(start, Filter(GetValues(parameters[argIndex], source), prefix), usage);
    }

    /// <summary>
    ///     The full usage of a command, e.g. <c>summon &lt;entityType&gt; [x] [y] [z]</c>, or null if unknown.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public string? GetUsage(string name) {
        if (!_commands.TryGetValue(name, out var entry)) {
            return null;
        }

        var args = entry.Method.GetParameters()
            .Where(p => p.ParameterType != typeof(CommandSource))
            .Select(FormatUsage);
        return string.Join(' ', args.Prepend(entry.Attr.Name));
    }

    private IEnumerable<string> GetValues(ParameterInfo parameter, CommandSource source) {
        var suggest = parameter.GetCustomAttribute<SuggestAttribute>();
        if (suggest is not null) {
            return _suggestionProviders.TryGetValue(suggest.ProviderType, out var provider)
                ? provider.GetSuggestions(source)
                : [];
        }

        var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
        if (type.IsEnum) {
            return Enum.GetNames(type).Select(n => n.ToLowerInvariant());
        }

        return type == typeof(bool) ? ["true", "false"] : [];
    }

    private static List<string> Filter(IEnumerable<string> values, string prefix) {
        return values
            .Where(v => v.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Take(MaxSuggestions)
            .ToList();
    }

    private static string FormatUsage(ParameterInfo parameter) {
        return parameter.HasDefaultValue ? $"[{parameter.Name}]" : $"<{parameter.Name}>";
    }

    private bool TryBindArguments(MethodInfo method, string[] tokens, CommandSource source, out object?[] args,
        out string? error) {
        var parameters = method.GetParameters();
        args = new object?[parameters.Length];
        error = null;
        var tokenIndex = 1;

        for (var i = 0; i < parameters.Length; i++) {
            var parameter = parameters[i];

            if (parameter.ParameterType == typeof(CommandSource)) {
                args[i] = source;
                continue;
            }

            if (tokenIndex >= tokens.Length) {
                if (parameter.HasDefaultValue) {
                    args[i] = parameter.DefaultValue;
                    continue;
                }

                error = $"Missing argument: {parameter.Name}";
                return false;
            }

            if (!_binders.TryBind(parameter.ParameterType, tokens[tokenIndex], out var value)) {
                error = $"Invalid value for '{parameter.Name}': {tokens[tokenIndex]}";
                return false;
            }

            args[i] = value;
            tokenIndex++;
        }

        return true;
    }

    private sealed record Entry(object Instance, MethodInfo Method, CommandAttribute Attr);
}