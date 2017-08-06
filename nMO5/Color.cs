namespace nMO5
{
	public struct Color
	{
		public byte R, G, B, A;

		public static Color FromArgb(int r, int g, int b)
		{
			return new Color { R = (byte)r, G = (byte)g, B = (byte)b, A = 0xFF };
		}

		public static readonly Color Red = FromArgb(0xFF, 0, 0);
		public static readonly Color Black = FromArgb(0, 0, 0);
	}
}
