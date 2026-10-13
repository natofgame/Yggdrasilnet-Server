namespace Yggdrasilnet.Server.Core.Commands;

internal sealed class ArgumentBinderRegistry {
    private readonly Dictionary<Type, Func<string, (bool Ok, object? Value)>> _binders = new();

    public ArgumentBinderRegistry() {
        AddParser<int>(int.TryParse);
        AddParser<float>(float.TryParse);
        AddParser<double>(double.TryParse);
        AddParser<bool>(bool.TryParse);
        AddParser((string s, out string v) => {
            v = s;
            return true;
        });
    }

    public void Add<T>(IArgumentBinder<T> binder) {
        _binders[typeof(T)] = token => {
            var ok = binder.TryBind(token, out var value);
            return (ok, value);
        };
    }

    private void AddParser<T>(TryParse<T> parse) {
        _binders[typeof(T)] = token => {
            var ok = parse(token, out var value);
            return (ok, value);
        };
    }

    public bool TryBind(Type type, string token, out object? value) {
        if (type.IsEnum) {
            var ok = Enum.TryParse(type, token, true, out var enumValue);
            value = enumValue;
            return ok;
        }

        if (_binders.TryGetValue(type, out var fn)) {
            var (ok, result) = fn(token);
            value = result;
            return ok;
        }

        value = null;
        return false;
    }

    private delegate bool TryParse<T>(string token, out T value);
}
