namespace Yggdrasilnet.Server.Core.Command;

/// <summary>
///  Registry for argument binders.
/// </summary>
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

    /// <summary>
    /// Add a binder for a specific type.
    /// </summary>
    /// <param name="binder"></param>
    /// <typeparam name="T"></typeparam>
    public void Add<T>(IArgumentBinder<T> binder) {
        _binders[typeof(T)] = token => {
            var ok = binder.TryBind(token, out var value);
            return (ok, value);
        };
    }

    /// <summary>
    ///  Add a binder for a specific type.
    /// </summary>
    /// <param name="parse"></param>
    /// <typeparam name="T"></typeparam>
    private void AddParser<T>(TryParse<T> parse) {
        _binders[typeof(T)] = token => {
            var ok = parse(token, out var value);
            return (ok, value);
        };
    }

    /// <summary>
    ///  Try to bind a token to a value.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="token"></param>
    /// <param name="value"></param>
    /// <returns></returns>
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

    /// <summary>
    ///  Delegate for parsing a token to a value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private delegate bool TryParse<T>(string token, out T value);
}
