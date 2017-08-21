namespace nMO5
{
    internal static class Util
    {
		// force sign extension in a portable but ugly maneer
        public static int SignedChar(int v)
		{
			if ((v & 0x80) == 0) return v & 0xFF;
			int delta = -1; // delta is 0xFFFF.... independently of 32/64bits
			delta = (delta >> 8) << 8; // force last 8bits to 0
			return (v & 0xFF) | delta; // result is now signed
		}

		// force sign extension in a portable but ugly maneer
		public static int Signed16Bits(int v)
		{
			if ((v & 0x8000) == 0) return v & 0xFFFF;
			int delta = -1; // delta is 0xFFFF.... independently of 32/64bits
			delta = (delta >> 16) << 16; // force last 16bits to 0
			return (v & 0xFFFF) | delta; // result is now signed
		}
    }
}
