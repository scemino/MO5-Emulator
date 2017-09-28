using MoonSharp.Interpreter;
using nMO5;

namespace MO5Emulator.Scripting
{
    [MoonSharpUserData]
    class LuaMemory
    {
        private Memory _memory;

        public LuaMemory(Memory memory)
        {
            _memory = memory;
        }

        public int Readbyte(int address)
        {
            return _memory.Read(address);
        }

		public int Readword(int address)
		{
            return _memory.Read16(address);
		}

		public void Writebyte(int address, int value)
		{
            _memory.Write(address, value);
		}

		public void Writeword(int address, int value)
		{
			_memory.Write(address, value);
		}
    }
}
