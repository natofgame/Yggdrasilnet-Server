namespace Yggdrasilnet.Server.Core.Command.Suggestion;

/// <summary>
///     Completions for a command line.
/// </summary>
/// <param name="Start">Index in the input where the completed word starts.</param>
/// <param name="Suggestions">Words that can replace the input from <paramref name="Start" />.</param>
/// <param name="Usage">Remaining arguments from the current one, e.g. <c>&lt;entityType&gt; [x]</c>.</param>
public sealed record CommandSuggestions(int Start, IReadOnlyList<string> Suggestions, string Usage) {
    public static readonly CommandSuggestions Empty = new(0, [], string.Empty);
}
