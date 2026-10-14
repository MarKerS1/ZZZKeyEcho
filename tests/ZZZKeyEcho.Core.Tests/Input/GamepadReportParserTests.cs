using ZZZKeyEcho.Core.Input;

namespace ZZZKeyEcho.Core.Tests.Input;

public class GamepadReportParserTests
{
    [Fact]
    public void DualSense_usb_report_is_parsed()
    {
        var report = new byte[64];
        report[0] = GamepadReportParser.DualSenseUsbReportId;
        report[1] = 255;
        report[2] = 128;
        report[5] = 255;
        report[6] = 0;
        report[8] = 0x38;
        report[9] = 0x81;

        Assert.True(GamepadReportParser.TryParseDualSense(report, out var reading));

        Assert.True(reading.Cross);
        Assert.True(reading.Square);
        Assert.True(reading.L1);
        Assert.True(reading.R3);
        Assert.Equal(0.99, reading.LeftStickX, 2);
        Assert.Equal(0, reading.LeftStickY, 2);
        Assert.Equal(1, reading.LeftTrigger, 2);
        Assert.Equal(0, reading.RightTrigger, 2);
    }

    [Fact]
    public void DualSense_bluetooth_report_applies_offset()
    {
        var report = new byte[78];
        report[0] = GamepadReportParser.DualSenseBluetoothReportId;
        report[2] = 255;
        report[9] = 0x20;

        Assert.True(GamepadReportParser.TryParseDualSense(report, out var reading));

        Assert.True(reading.Cross);
        Assert.Equal(0.99, reading.LeftStickX, 2);
    }

    [Fact]
    public void DPad_encoding_maps_diagonals_to_two_directions()
    {
        var report = new byte[64];
        report[0] = GamepadReportParser.DualSenseUsbReportId;
        report[8] = 0x01;

        Assert.True(GamepadReportParser.TryParseDualSense(report, out var reading));
        Assert.True(reading.DPadUp);
        Assert.True(reading.DPadRight);
        Assert.False(reading.DPadLeft);
        Assert.False(reading.DPadDown);
    }

    [Fact]
    public void Ds4_usb_report_is_parsed()
    {
        var report = new byte[64];
        report[0] = GamepadReportParser.Ds4UsbReportId;
        report[1] = 128;
        report[2] = 255;
        report[5] = 0x01;
        report[6] = 0x03;
        report[8] = 200;

        Assert.True(GamepadReportParser.TryParseDs4(report, out var reading));

        Assert.True(reading.DPadUp);
        Assert.True(reading.L1);
        Assert.True(reading.R1);
        Assert.Equal(0, reading.LeftStickX, 2);
        Assert.Equal(-0.99, reading.LeftStickY, 2);
        Assert.True(reading.LeftTrigger > 0.7);
    }

    [Fact]
    public void Short_reports_are_rejected()
    {
        Assert.False(GamepadReportParser.TryParseDualSense(new byte[4], out _));
        Assert.False(GamepadReportParser.TryParseDs4(new byte[4], out _));
    }
}
