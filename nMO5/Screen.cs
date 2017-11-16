using System;

namespace nMO5
{
    public class Screen
    {
        public const int Width = 336;  // screen width = 320 + 2 borders of 8 pixels
        public const int Height = 216; // screen height = 200 + 2 boarders of 8 pixels

        private static readonly Color[] Palette =
        {
            Color.FromRgb(0x00, 0x00, 0x00),
            Color.FromRgb(0xF0, 0x00, 0x00),
            Color.FromRgb(0x00, 0xF0, 0x00),
            Color.FromRgb(0xF0, 0xF0, 0x00),

            Color.FromRgb(0x00, 0x00, 0xF0),
            Color.FromRgb(0xF0, 0x00, 0xF0),
            Color.FromRgb(0x00, 0xF0, 0xF0),
            Color.FromRgb(0xF0, 0xF0, 0xF0),

            Color.FromRgb(0x63, 0x63, 0x63),
            Color.FromRgb(0xF0, 0x63, 0x63),
            Color.FromRgb(0x63, 0xF0, 0x63),
            Color.FromRgb(0xF0, 0xF0, 0x63),

            Color.FromRgb(0x00, 0x63, 0xF0),
            Color.FromRgb(0xF0, 0x63, 0xF0),
            Color.FromRgb(0x63, 0xF0, 0xF0),
            Color.FromRgb(0xF0, 0x63, 0x00)
        };

        private Color BorderColor => Palette[_mem.BorderColor];
        public Color[] Pixels => _pixels;

        private readonly Color[] _pixels;

        private IMemory _mem;

        public Screen(IMemory memory)
        {
            _pixels = new Color[Width * Height];
            _mem = memory;
        }

        public void Update(Color[] dest)
        {
            DrawScreen();
            Array.Copy(_pixels, dest, _pixels.Length);
        }

        public void SetPixel(int x, int y, Color color)
        {
            _pixels[y * Width + x] = color;
        }

        public Color GetPixel(int x, int y)
        {
            return _pixels[y * Width + x];
        }

        public void DrawBox(int x1, int y1, int x2, int y2, Color fillColor, Color outlinecolor)
        {
            int width = x2 - x1;
            int height = y2 - y1;
            var offs = x1 + y1 * Width;
            for (var y = 0; y <= height; y++)
            {
                for (var x = 0; x <= width; x++)
                {
                    _pixels[offs++] = fillColor;
                }
                offs += Width - width - 1;
            }
            DrawLine(x1, y1, x2, y1, outlinecolor);
            DrawLine(x1, y1, x1, y2, outlinecolor);
            DrawLine(x2, y1, x2, y2, outlinecolor);
            DrawLine(x1, y2, x2, y2, outlinecolor);
        }

        public void DrawLine(int x1, int y1, int x2, int y2, Color color)
        {
            int w = x2 - x1;
            int h = y2 - y1;
            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
            if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
            if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
            if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
            int longest = Math.Abs(w);
            int shortest = Math.Abs(h);
            if (!(longest > shortest))
            {
                longest = Math.Abs(h);
                shortest = Math.Abs(w);
                if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
                dx2 = 0;
            }
            int numerator = longest >> 1;
            for (int i = 0; i <= longest; i++)
            {
                SetPixel(x1, y1, color);
                numerator += shortest;
                if (!(numerator < longest))
                {
                    numerator -= longest;
                    x1 += dx1;
                    y1 += dy1;
                }
                else
                {
                    x1 += dx2;
                    y1 += dy2;
                }
            }
        }

        private void DrawScreen()
        {
            var i = 0;

            DrawBorder();

            for (var y = 0; y < 200; y++)
            {
                var offset = (y + 8) * Width + 8;
                var x = 0;
                if (!_mem.IsDirty(y))
                {
                    i += 40;
                }
                else
                {
                    for (var xx = 0; xx < 40; xx++)
                    {
                        var col = _mem.Color(i);
                        var c2 = col & 0x0F;
                        var c1 = col >> 4;
                        var cc2 = Palette[c1];
                        var cc1 = Palette[c2];

                        var pt = _mem.Point(i);
                        if ((0x80 & pt) != 0)
                            _pixels[x + offset] = cc2;
                        else
                            _pixels[x + offset] = cc1;
                        x++;
                        if ((0x40 & pt) != 0)
                            _pixels[x + offset] = cc2;
                        else
                            _pixels[x + offset] = cc1;
                        x++;
                        if ((0x20 & pt) != 0)
                            _pixels[x + offset] = cc2;
                        else
                            _pixels[x + offset] = cc1;
                        x++;
                        if ((0x10 & pt) != 0)
                            _pixels[x + offset] = cc2;
                        else
                            _pixels[x + offset] = cc1;
                        x++;
                        if ((0x08 & pt) != 0)
                            _pixels[x + offset] = cc2;
                        else
                            _pixels[x + offset] = cc1;
                        x++;
                        if ((0x04 & pt) != 0)
                            _pixels[x + offset] = cc2;
                        else
                            _pixels[x + offset] = cc1;
                        x++;
                        if ((0x02 & pt) != 0)
                            _pixels[x + offset] = cc2;
                        else
                            _pixels[x + offset] = cc1;
                        x++;
                        if ((0x01 & pt) != 0)
                            _pixels[x + offset] = cc2;
                        else
                            _pixels[x + offset] = cc1;
                        x++;
                        i++;
                    }
                }
            }
        }

        private void DrawBorder()
        {
            var bc = BorderColor;

            // draw top/bottom borders
            for (var y = 0; y < 8; y++)
            {
                var offset = y * Width;
                var offset2 = (Height - 8 + y) * Width;
                for (var x = 0; x < Width; x++)
                {
                    _pixels[x + offset] = bc;
                    _pixels[x + offset2] = bc;
                }
            }

            // draw left/right borders
            for (var y = 0; y < Height - 16; y++)
            {
                var offset = (y + 8) * Width;
                var offset2 = (y + 9) * Width - 8;
                for (var x = 0; x < 8; x++)
                {
                    _pixels[x + offset] = bc;
                    _pixels[x + offset2] = bc;
                }
            }
        }
    }
}