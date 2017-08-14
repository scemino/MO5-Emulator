namespace MO5Emulator.Cheats
{
	enum ByteFormat
	{
		One,
		Two
	}

	class Cheat
	{
		public string Description { get; }
		public int Address { get; }
		public int Value { get; }
		public ByteFormat Format { get; }

		public Cheat(string description, int address, int value, ByteFormat format)
		{
			Description = description;
			Address = address;
			Value = value;
			Format = format;
		}

		public override string ToString()
		{
			return string.Format("{3}: Address={0}, Value={1}, Format={2}", Address, Value, Format, Description);
		}
	}
}
