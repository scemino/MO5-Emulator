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

		public static readonly Color White = FromRgb(0xFF, 0xFF, 0xFF);
		public static readonly Color Red = FromRgb(0xFF, 0, 0);
		public static readonly Color Black = FromRgb(0, 0, 0);
	}
}
