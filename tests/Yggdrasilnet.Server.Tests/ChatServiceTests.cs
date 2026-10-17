using Xunit;
using Yggdrasilnet.Network.Chat;
using Yggdrasilnet.Server.Core.Services;

namespace Yggdrasilnet.Server.Tests;

public sealed class ChatServiceTests {
    [Fact]
    public void FormatsAsNameColonMessage() {
        Assert.Equal("~7Natof~f: salut", ChatService.FormatPlayerMessage("Natof", "  salut "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t")]
    public void ReturnsNull_ForEmptyMessage(string? message) {
        Assert.Null(ChatService.FormatPlayerMessage("Natof", message));
    }

    [Fact]
    public void PlayerCannotUseColorCodes() {
        var text = ChatService.FormatPlayerMessage("Natof", "~cje suis rouge")!;

        Assert.Equal("Natof: ~cje suis rouge", ChatFormatting.StripCodes(text));
        Assert.DoesNotContain(ChatFormatting.Parse(text), s => s.Color == ChatColor.Red);
    }

    [Fact]
    public void RemovesControlCharacters() {
        Assert.Equal("~7Natof~f: ab", ChatService.FormatPlayerMessage("Natof", "a\nb"));
    }

    [Fact]
    public void TruncatesLongMessages() {
        var text = ChatService.FormatPlayerMessage("Natof", new string('x', 1000))!;

        Assert.Equal("Natof: ".Length + ChatFormatting.MaxMessageLength, ChatFormatting.StripCodes(text).Length);
    }
}
