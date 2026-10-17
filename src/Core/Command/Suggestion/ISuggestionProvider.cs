namespace Yggdrasilnet.Server.Core.Command.Suggestion;

/// <summary>
///     Provides the possible values of a command argument, for auto-completion.
///     Registered with <see cref="CommandFactory.RegisterSuggestionProvider" /> and used through <see cref="SuggestAttribute" />.
/// </summary>
public interface ISuggestionProvider {
    /// <summary>
    ///     All the values the argument can take. The factory filters them with what the player typed.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    IEnumerable<string> GetSuggestions(CommandSource source);
}
