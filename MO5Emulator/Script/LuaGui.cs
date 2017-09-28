namespace MO5Emulator.Scripting
{
    [MoonSharpUserData]
    class LuaGui
    {
        Screen _screen;
        public LuaGui(Screen screen)
        {
            _screen = screen;
        }

        public void Pixel(int x, int y, Color color)
        {
            _screen.SetPixel(x, y, color);
        }

        public void Box(int x1, int y1, int x2, int y2, Color? fillColor = null, Color? outlineColor = null)
        {
            _screen.DrawBox(x1, y1, x2, y2, fillColor ?? Color.White, outlineColor ?? Color.Black);
        }

        public Color GetPixel(int x, int y)
        {
            return _screen.GetPixel(x, y);
        }

        public void Line(int x1, int y1, int x2, int y2, Color color)
        {
            var dx = x1 - x1;
            var dy = y2 - y1;
            var D = 2 * dy - dx;
            var y = y1;

            for (var x = x1; x <= x2; x++)
            {
                Pixel(x, y, color);
                if (D > 0)
                {
                    y = y + 1;
                    D = D - 2 * dx;
                }
                D = D + 2 * dy;
            }
        }
    }
}
