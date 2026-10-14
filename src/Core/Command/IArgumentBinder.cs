namespace Yggdrasilnet.Server.Core.Command;

/// <summary>
///  Binds arguments to a type.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IArgumentBinder<T> {
    /// <summary>
    ///  Tries to bind the argument to the type.
    /// </summary>
    /// <param name="token"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    bool TryBind(string token, out T value);
}
