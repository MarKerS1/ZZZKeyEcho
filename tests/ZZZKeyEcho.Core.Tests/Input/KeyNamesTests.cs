using ZZZKeyEcho.Core.Input;

namespace ZZZKeyEcho.Core.Tests.Input;

public class KeyNamesTests
{
    [Theory]
    [InlineData(0x57u, "W")]
    [InlineData(0x41u, "A")]
    [InlineData(0x53u, "S")]
    [InlineData(0x44u, "D")]
    [InlineData(0x45u, "E")]
    [InlineData(0x51u, "Q")]
    [InlineData(0x52u, "R")]
    [InlineData(0x43u, "C")]
    [InlineData(0x31u, "1")]
    [InlineData(0x20u, "Space")]
    [InlineData(0xA0u, "LShift")]
    [InlineData(0xA1u, "RShift")]
    [InlineData(0x70u, "F1")]
    [InlineData(0x77u, "F8")]
    [InlineData(0x87u, "F24")]
    public void FromVirtualKey_maps_known_keys(uint virtualKey, string expected) =>
        Assert.Equal(expected, KeyNames.FromVirtualKey(virtualKey));

    [Theory]
    [InlineData(0x0Fu)]
    [InlineData(0xE8u)]
    public void FromVirtualKey_returns_null_for_unknown_keys(uint virtualKey) =>
        Assert.Null(KeyNames.FromVirtualKey(virtualKey));
}
