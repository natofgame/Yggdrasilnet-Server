using Xunit;
using Yggdrasilnet.Server.Core.Command;
using Yggdrasilnet.Server.Core.Command.Suggestion;

namespace Yggdrasilnet.Server.Tests;

public sealed class CommandSuggestionTests {
    private readonly CommandFactory _factory = new();
    private readonly CommandSource _player = new() { PermissionLevel = 0 };

    public CommandSuggestionTests() {
        _factory.RegisterSuggestionProvider(new FixedProvider("zombie", "skeleton", "spider"));
        _factory.Register(new TestCommands());
    }

    [Fact]
    public void SuggestsCommandNames_MatchingThePrefix() {
        var result = _factory.Suggest("s", _player);

        Assert.Equal(0, result.Start);
        Assert.Equal(["spawnpoint", "summon"], result.Suggestions);
    }

    [Fact]
    public void HidesCommands_AbovePermission() {
        Assert.DoesNotContain("op", _factory.Suggest("", _player).Suggestions);
        Assert.Contains("op", _factory.Suggest("", new CommandSource { PermissionLevel = 4 }).Suggestions);
    }

    [Fact]
    public void SuggestsProviderValues_ForFirstArgument() {
        var result = _factory.Suggest("summon s", _player);

        Assert.Equal(7, result.Start);
        Assert.Equal(["skeleton", "spider"], result.Suggestions);
        Assert.Equal("<entityType> [x] [y] [z]", result.Usage);
    }

    [Fact]
    public void GivesUsageOfRemainingArguments_WhenNoValues() {
        var result = _factory.Suggest("summon zombie ", _player);

        Assert.Empty(result.Suggestions);
        Assert.Equal("[x] [y] [z]", result.Usage);
        Assert.Equal(14, result.Start);
    }

    [Fact]
    public void SuggestsEnumAndBoolValues() {
        Assert.Equal(["easy", "hard", "normal"], _factory.Suggest("spawnpoint ", _player).Suggestions);
        Assert.Equal(["true"], _factory.Suggest("spawnpoint hard t", _player).Suggestions);
    }

    [Theory]
    [InlineData("unknown ")]
    [InlineData("summon zombie 1 2 3 ")]
    public void SuggestsNothing_ForUnknownCommandOrTooManyArgs(string input) {
        var result = _factory.Suggest(input, _player);

        Assert.Empty(result.Suggestions);
        Assert.Empty(result.Usage);
    }

    [Fact]
    public void BuildsFullUsage() {
        Assert.Equal("summon <entityType> [x] [y] [z]", _factory.GetUsage("summon"));
    }

    private enum Difficulty { Easy, Normal, Hard }

    private sealed class FixedProvider(params string[] values) : ISuggestionProvider {
        public IEnumerable<string> GetSuggestions(CommandSource source) => values;
    }

    private sealed class TestCommands : ICommand {
        [Command("summon")]
        public void Summon(CommandSource source, [Suggest(typeof(FixedProvider))] string entityType,
            float x = 0, float y = 0, float z = 0) { }

        [Command("spawnpoint")]
        public void SpawnPoint(CommandSource source, Difficulty difficulty, bool keep = false) { }

        [Command("op", Permission = 4)]
        public void Op(CommandSource source, string name) { }
    }
}
