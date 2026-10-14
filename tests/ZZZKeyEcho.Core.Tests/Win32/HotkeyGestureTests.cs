using ZZZKeyEcho.Core.Win32;

namespace ZZZKeyEcho.Core.Tests.Win32;

public class HotkeyGestureTests
{
    [Theory]
    [InlineData("F1", 0u, 0x70u)]
    [InlineData("F8", 0u, 0x77u)]
    [InlineData("f11", 0u, 0x7Au)]
    [InlineData("F24", 0u, 0x87u)]
    [InlineData("Ctrl+F8", 0x0002u, 0x77u)]
    [InlineData("ctrl + shift + alt + win + F9", 0x000Fu, 0x78u)]
    [InlineData("A", 0u, 0x41u)]
    [InlineData("9", 0u, 0x39u)]
    [InlineData("Space", 0u, 0x20u)]
    [InlineData("Shift+Escape", 0x0004u, 0x1Bu)]
    public void TryParse_accepts_valid_gestures(string text, uint modifiers, uint virtualKey)
    {
        Assert.True(HotkeyGesture.TryParse(text, out var gesture));
        Assert.Equal(modifiers, gesture.Modifiers);
        Assert.Equal(virtualKey, gesture.VirtualKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ctrl")]
    [InlineData("F0")]
    [InlineData("F25")]
    [InlineData("F8+F9")]
    [InlineData("Nope")]
    [InlineData("Ctrl+Nope")]
    public void TryParse_rejects_invalid_gestures(string? text) =>
        Assert.False(HotkeyGesture.TryParse(text, out _));
}
