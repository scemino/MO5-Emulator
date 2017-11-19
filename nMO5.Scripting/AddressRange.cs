namespace MO5Emulator.Scripting
{
	struct AddressRange
	{
		public AddressRange(int address, int length)
		{
			Address = address;
			EndAddress = address + length;
			Length = length;
		}

		public int Address { get; }
		public int EndAddress { get; }
		public int Length { get; }

        public bool ConstainsAddress(int address)
        {
            return address >= Address && address <= EndAddress;    
        }
	}
}
