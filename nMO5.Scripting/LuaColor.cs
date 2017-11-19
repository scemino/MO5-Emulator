using System;
using System.Linq;
using System.Text.RegularExpressions;
using MoonSharp.Interpreter;
using nMO5;

namespace MO5Emulator.Scripting
{
    [MoonSharpUserData]
    class LuaColor
    {
        private static readonly Regex _rgbColorRegex =
            new Regex(@"\#([a-fA-F\d]{2})([a-fA-F\d]{2})([a-fA-F\d]{2})");
		private static readonly Regex _rgbaColorRegex =
			new Regex(@"\#([a-fA-F\d]{2})([a-fA-F\d]{2})([a-fA-F\d]{2})([a-fA-F\d]{2})");
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
            // rgba
            var match = _rgbaColorRegex.Match(value);
            if (match.Success)
            {
                var r = int.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
                var g = int.Parse(match.Groups[2].Value, System.Globalization.NumberStyles.HexNumber);
                var b = int.Parse(match.Groups[3].Value, System.Globalization.NumberStyles.HexNumber);
                var a = int.Parse(match.Groups[4].Value, System.Globalization.NumberStyles.HexNumber);
                return new LuaColor(Color.FromArgb(a, r, g, b));
            }

			// rgb
			match = _rgbColorRegex.Match(value);
            if (match.Success)
            {
                var r = int.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
                var g = int.Parse(match.Groups[2].Value, System.Globalization.NumberStyles.HexNumber);
                var b = int.Parse(match.Groups[3].Value, System.Globalization.NumberStyles.HexNumber);
                return new LuaColor(Color.FromRgb(r, g, b));
            }

            // color names
            var fields = typeof(Color).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var field = fields.FirstOrDefault(f => string.Equals(f.Name, value, StringComparison.OrdinalIgnoreCase));
            if (field != null)
            {
                return new LuaColor((Color)field.GetValue(null));
            }
			if (value == "clear")
			{
				return new LuaColor(Color.Transparent);
			}

            return null;
        }

        [MoonSharpHidden]
        public static LuaColor Parse(Table value)
        {
            if (value.Get("r").Type != DataType.Nil)
            {
                var a = (int)value.Get("a").Number;
                var r = (int)value.Get("r").Number;
                var g = (int)value.Get("g").Number;
                var b = (int)value.Get("b").Number;
                return new LuaColor(Color.FromArgb(a, r, g, b));
            }

            if (value.Get(1).Type != DataType.Nil)
			{
                var a = (int)value.Get(4).Number;
                var r = (int)value.Get(1).Number;
                var g = (int)value.Get(2).Number;
				var b = (int)value.Get(3).Number;
				return new LuaColor(Color.FromArgb(a, r, g, b));
			}

            return null;
        }

		[MoonSharpHidden]
        public static LuaColor Parse(int value)
        {
            var r = (int)(value & 0xFF000000) >> 24;
            var g = (value & 0xFF0000) >> 16;
            var b = (value & 0xFF00) >> 8;
            var a = value & 0xFF;
            return new LuaColor(Color.FromArgb(a, r, g, b));
        }
    }
}
