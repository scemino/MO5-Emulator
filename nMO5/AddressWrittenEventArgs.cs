using System;

namespace nMO5
{
    public class AddressWrittenEventArgs: EventArgs
    {
        public int Address { get; }
		public int Size { get; }
		public int Value { get; }

        public AddressWrittenEventArgs(int address, int size, int value)
        {
            Address = address;
            Size = size;
            Value = value;
        }
    }
}