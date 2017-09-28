using System.Text.RegularExpressions;
using MoonSharp.Interpreter;
using nMO5;

namespace MO5Emulator.Scripting
{
    [MoonSharpUserData]
    class LuaColor
    {
        private static readonly Regex _colorRegex =
            new Regex(@"\#([a-fA-F\d]{2})([a-fA-F\d]{2})([a-fA-F\d]{2})");
        private Color _color;

        public LuaColor(Color color)
        {
            _color = color;
        }

        [MoonSharpHidden]
        public Color ToClrColor()
        {
            return _color;
        }

        [MoonSharpHidden]
        public void FromClrColor(Color color)
        {
            _color = color;
        }

        [MoonSharpHidden]
        public static LuaColor Parse(string value)
        {
            var match = _colorRegex.Match(value);
            if (!match.Success) return null;

            var r = int.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
            var g = int.Parse(match.Groups[2].Value, System.Globalization.NumberStyles.HexNumber);
            var b = int.Parse(match.Groups[3].Value, System.Globalization.NumberStyles.HexNumber);
            return new LuaColor(Color.FromRgb(r, g, b));
        }
    }
}
