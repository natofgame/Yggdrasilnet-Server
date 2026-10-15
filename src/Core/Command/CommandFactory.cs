using System.Reflection;
using Yggdrasilnet.Server.Core.Command;

namespace Yggdrasilnet.Server.Core.Commands;

public class CommandFactory {
    private readonly ArgumentBinderRegistry _binders = new();

    private readonly Dictionary<string, Entry> _commands = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterBinder<T>(IArgumentBinder<T> binder) {
        _binders.Add(binder);
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
            source.SendFeedback($"Unknown command: {tokens[0]}");
            return;
        }

        if (source.PermissionLevel < entry.Attr.Permission) {
            source.SendFeedback("Insufficient permissions.");
            return;
        }

        if (!TryBindArguments(entry.Method, tokens, source, out var args, out var error)) {
            source.SendFeedback(error ?? "Invalid arguments.");
            return;
        }

        entry.Method.Invoke(entry.Instance, args);
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