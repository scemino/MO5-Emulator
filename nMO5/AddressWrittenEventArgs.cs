using System;

namespace nMO5
{
    public class AddressWrittenEventArgs: EventArgs
    {
        public int Address { get; }
        public int Size { get; }

        public AddressWrittenEventArgs(int address, int size)
        {
            Address = address;
            Size = size;
        }
    }
}