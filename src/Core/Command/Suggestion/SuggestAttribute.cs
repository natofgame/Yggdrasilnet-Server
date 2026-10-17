namespace Yggdrasilnet.Server.Core.Command.Suggestion;

/// <summary>
///     Marks a command parameter whose values come from an <see cref="ISuggestionProvider" />.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class SuggestAttribute : Attribute {
    public SuggestAttribute(Type providerType) {
        if (!typeof(ISuggestionProvider).IsAssignableFrom(providerType)) {
            throw new ArgumentException($"{providerType.Name} must implement {nameof(ISuggestionProvider)}.",
                nameof(providerType));
        }

        ProviderType = providerType;
    }

    /// <summary>
    ///     The provider type, as registered in the <see cref="CommandFactory" />.
    /// </summary>
    public Type ProviderType { get; }
}
