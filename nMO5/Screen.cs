using System;

namespace nMO5
{
    public class Screen
    {
        public const int Width = 336;  // screen width = 320 + 2 borders of 8 pixels
		public const int Height = 216; // screen height = 200 + 2 boarders of 8 pixels

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

		private Color BorderColor
		{
			get
			{
                return Palette[_mem.BorderColor];
			}
		}

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
            DrawScreen();
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