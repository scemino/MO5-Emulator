namespace nMO5
{
	public struct Color
	{
		public byte R, G, B, A;

		public static Color FromRgb(int r, int g, int b)
		{
			return new Color { R = (byte)r, G = (byte)g, B = (byte)b, A = 0xFF };
		}

		public static Color FromArgb(int a, int r, int g, int b)
		{
			return new Color { R = (byte)r, G = (byte)g, B = (byte)b, A = (byte)a };
		}

		public static readonly Color Red = FromRgb(255, 0, 0);
		public static readonly Color Green = FromRgb(0, 255, 0);
		public static readonly Color Blue = FromRgb(0, 0, 255);
		public static readonly Color White = FromRgb(255, 255, 255);
		public static readonly Color Black = FromRgb(0, 0, 0);
		public static readonly Color Gray = FromRgb(128, 128, 128);
        public static readonly Color Grey = Gray;
		public static readonly Color Orange = FromRgb(255, 165, 0);
		public static readonly Color Yellow = FromRgb(255, 255, 0);
		public static readonly Color Teal = FromRgb(0, 128, 128);
		public static readonly Color Cyan = FromRgb(0, 255, 255);
		public static readonly Color Purple = FromRgb(128, 0, 128);
		public static readonly Color Margenta = FromRgb(255, 0, 255);
        public static readonly Color Transparent = FromArgb(255, 255, 255, 255);
    }
}
