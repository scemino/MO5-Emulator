using System;

namespace nMO5
{
    public class Screen
    {
        public const int Width = 320;
        public const int Height = 200;

        private static readonly Color[] Palette =
        {
            Color.FromArgb(0x00, 0x00, 0x00),
            Color.FromArgb(0xF0, 0x00, 0x00),
            Color.FromArgb(0x00, 0xF0, 0x00),
            Color.FromArgb(0xF0, 0xF0, 0x00),

            Color.FromArgb(0x00, 0x00, 0xF0),
            Color.FromArgb(0xF0, 0x00, 0xF0),
            Color.FromArgb(0x00, 0xF0, 0xF0),
            Color.FromArgb(0xF0, 0xF0, 0xF0),

            Color.FromArgb(0x63, 0x63, 0x63),
            Color.FromArgb(0xF0, 0x63, 0x63),
            Color.FromArgb(0x63, 0xF0, 0x63),
            Color.FromArgb(0xF0, 0xF0, 0x63),

            Color.FromArgb(0x00, 0x63, 0xF0),
            Color.FromArgb(0xF0, 0x63, 0xF0),
            Color.FromArgb(0x63, 0xF0, 0xF0),
            Color.FromArgb(0xF0, 0x63, 0x00)
        };

        private readonly Color[] _pixels;

        private Memory _mem;

        public bool MouseClick { get; set; }
        public int MouseX { get; private set; }
        public int MouseY { get; private set; }

        public Screen()
        {
            _pixels = new Color[Width * Height];
            MouseX = -1;
            MouseY = -1;
        }

        // Mouse Event use for Lightpen emulation
        public void SetMousePosition(int x, int y)
        {
            MouseX = x;
            MouseY = y;
        }

        internal void Init(Memory memory)
        {
            _mem = memory;
        }

        public void Update(Color[] dest)
        {
            Dopaint();
            DrawLed();
            Array.Copy(_pixels, dest, _pixels.Length);
        }

        private void DrawLed()
        {
            if (_mem.ShowLed <= 0) return;
            
            _mem.ShowLed--;
            var c = _mem.Led != 0 ? Color.Red : Color.Black;
            for (var i = 0; i < 16; i++)
            {
                var offs = i * Width + Width - 16;
                for (var j = 0; j < 16; j++)
                {
                    _pixels[offs + j] = c;
                }
            }
        }

        private void Dopaint()
        {
            var i = 0;

            for (var y = 0; y < Height; y++)
            {
                var offset = y * Width;
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
    }
}