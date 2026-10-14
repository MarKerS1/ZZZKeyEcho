namespace ZZZKeyEcho.Core.Input;

public readonly record struct GamepadReading(
    bool Square,
    bool Cross,
    bool Circle,
    bool Triangle,
    bool L1,
    bool R1,
    bool L2,
    bool R2,
    bool L3,
    bool R3,
    bool DPadUp,
    bool DPadDown,
    bool DPadLeft,
    bool DPadRight,
    double LeftStickX,
    double LeftStickY,
    double LeftTrigger,
    double RightTrigger);

public enum GamepadNaming
{
    Xbox,
    PlayStation,
}

public static class GamepadCodes
{
    public const string Up = "Up";
    public const string Down = "Down";
    public const string Left = "Left";
    public const string Right = "Right";

    public static string FaceDown(GamepadNaming naming) => naming == GamepadNaming.Xbox ? "AButton" : "Cross";

    public static string FaceRight(GamepadNaming naming) => naming == GamepadNaming.Xbox ? "BButton" : "Circle";

    public static string FaceLeft(GamepadNaming naming) => naming == GamepadNaming.Xbox ? "XButton" : "Square";

    public static string FaceUp(GamepadNaming naming) => naming == GamepadNaming.Xbox ? "YButton" : "Triangle";

    public static string ShoulderLeft(GamepadNaming naming) => naming == GamepadNaming.Xbox ? "LB" : "L1";

    public static string ShoulderRight(GamepadNaming naming) => naming == GamepadNaming.Xbox ? "RB" : "R1";

    public static string TriggerLeft(GamepadNaming naming) => naming == GamepadNaming.Xbox ? "LT" : "L2";

    public static string TriggerRight(GamepadNaming naming) => naming == GamepadNaming.Xbox ? "RT" : "R2";

    public static string StickLeft(GamepadNaming naming) => naming == GamepadNaming.Xbox ? "L3" : "L3";

    public static string StickRight(GamepadNaming naming) => naming == GamepadNaming.Xbox ? "R3" : "R3";

    public static string DPad(GamepadNaming naming, string direction) => "DPad" + direction;
}
