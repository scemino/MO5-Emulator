namespace nMO5
{
    public enum Mo5Key
    {
        N = 0x00, Delete = 0x02, J = 0x04, H = 0x06, U = 0x08, Y = 0x0A, D7 = 0x0C, D6 = 0x0E,
        Comma = 0x10, Insert = 0x12, K = 0x14, G = 0x16, I = 0x18, T = 0x1A, D8 = 0x1C, D5 = 0x1E,
        Dot = 0x20, Back = 0x22, L = 0x24, F = 0x26, O = 0x28, R = 0x2A, D9 = 0x2C, D4 = 0x2E,
        At = 0x30, Right = 0x32, M = 0x34, D = 0x36, P = 0x38, E = 0x3A, D0 = 0x3C, D3 = 0x3E,
        Space = 0x40, Down = 0x42, B = 0x44, S = 0x46, Slash = 0x48, Z = 0x4A, Minus = 0x4C, D2 = 0X4E,
        X = 0x50, Left = 0x52, V = 0x54, Q = 0x56, Multiply = 0x58, A = 0x5A, Plus = 0x5C, D1 = 0x5E,
        W = 0x60, Up = 0x62, C = 0x64, Reset = 0x66, Enter = 0x68, Control = 0x6A, Backspace = 0x6C, Stop = 0x6E,
        Shift = 0x70, Basic = 0x72
    }

    public interface IInput
    {
        bool LightPenClick { get; }
        int LightPenX { get; }
        int LightPenY { get; }

        bool IsKeyPressed(Mo5Key key);

        JoystickOrientation Joystick1Orientation { get; }
        JoystickOrientation Joystick2Orientation { get; }
        bool Joystick1ButtonPressed { get; }
        bool Joystick2ButtonPressed { get; }
    }
}