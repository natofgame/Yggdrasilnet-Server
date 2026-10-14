namespace Yggdrasilnet.Server.Core.Commands;

public interface IArgumentBinder<T> {
    bool TryBind(string token, out T value);
}
