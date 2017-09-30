namespace MO5Emulator
{
	enum MemoryComparisonOperator
	{
		Lower,
		Greater,
		LowerOrEquals,
		GreaterOrEquals,
		Equals,
		NotEquals
	}

	enum MemoryComparisonMode
	{
		PreviousValue,
		SpecificValue,
	}

    struct MemorySearchSession
    {
        public int Value { get; }
        public int PreviousValue { get; }
        public int Changes { get; }
        public int Size { get; }

        public MemorySearchSession(int previousValue, int value, int size, int changes)
        {
            PreviousValue = previousValue;
            Value = value;
            Changes = changes;
            Size = size;
        }

        public override string ToString()
        {
            return string.Format("[Value={0}, PreviousValue={1}, Changes={2}]", Value, PreviousValue, Changes);
        }
    }
}
