using System.Collections.Generic;
using MoonSharp.Interpreter;
using nMO5;

namespace MO5Emulator.Scripting
{
    [MoonSharpUserData]
    class LuaMemory
    {
        private Machine _machine;
        private IMemory _memory;
        private readonly Dictionary<AddressRange, Closure> _registerExecActions;
        private readonly Dictionary<AddressRange, Closure> _registerWrittenActions;

        [MoonSharpHidden]
        public LuaMemory(Machine machine)
        {
            _machine = machine;
            _memory = _machine.Memory;
            _registerExecActions = new Dictionary<AddressRange, Closure>();
            _registerWrittenActions = new Dictionary<AddressRange, Closure>();
            _machine.Cpu.OpcodeExecuted += OnOpcodeExecuted;
            _machine.Memory.Written += OnMemoryWritten;
        }

        public int Readbyte(int address)
        {
            return _memory.Read(address);
        }

        public int Readbyteunsigned(int address)
        {
            return Readbyte(address);
        }

        public byte[] Readbyterange(int address, int length)
        {
            var data = new byte[length];
            for (int i = 0; i < length; i++)
            {
                data[i] = (byte)_memory.Read(address);
            }
            return data;
        }

        public int Readbytesigned(int address)
        {
            var value = Readbyte(address);
            return (sbyte)value;
        }

        public int Readword(int address)
        {
            return _memory.Read16(address);
        }

        public int Readwordsigned(int address)
        {
            return (short)_memory.Read16(address);
        }

        public int Readwordunsigned(int address)
        {
            return Readword(address);
        }

        public int Getregister(string registerName)
        {
            switch (registerName)
            {
                case "pc":
                    return _machine.Cpu.RegPc;
				case "a":
                    return _machine.Cpu.RegA;
				case "b":
                    return _machine.Cpu.RegB;
				case "cc":
                    return _machine.Cpu.RegCc;
				case "d":
                    return _machine.Cpu.RegD;
				case "dp":
                    return _machine.Cpu.RegDp;
				case "s":
                    return _machine.Cpu.RegS;
				case "u":
                    return _machine.Cpu.RegU;
				case "x":
                    return _machine.Cpu.RegX;
				case "y":
                    return _machine.Cpu.RegY;
            }
            return 0;
        }

		public int Setregister(string registerName, int value)
		{
			switch (registerName)
			{
				case "pc":
                    return _machine.Cpu.RegPc = value;
				case "a":
                    return _machine.Cpu.RegA = value;
				case "b":
                    return _machine.Cpu.RegB = value;
				case "cc":
                    return _machine.Cpu.RegCc = value;
				case "d":
                    return _machine.Cpu.RegD = value;
				case "dp":
                    return _machine.Cpu.RegDp = value;
				case "s":
                    return _machine.Cpu.RegS = value;
				case "u":
                    return _machine.Cpu.RegU = value;
				case "x":
                    return _machine.Cpu.RegX = value;
				case "y":
                    return _machine.Cpu.RegY = value;
			}
			return 0;
		}

        public void Writebyte(int address, int value)
        {
            _memory.Write(address, value % 256);
        }

        public void Writeword(int address, int value)
        {
            _memory.Write(address, value % 65536);
        }


        public void Registerexec(int address, int size, Closure func)
        {
            _registerExecActions[new AddressRange(address, size)] = func;
        }

		public void Registerrun(int address, int size, Closure func)
		{
			Registerexec(address, size, func);
		}

		public void Registerexecute(int address, int size, Closure func)
		{
			Registerexec(address, size, func);
		}


		public void Register(int address, Closure func)
		{
            Register(address, 1, func);
		}

		public void Register(int address, int size, Closure func)
		{
            _registerWrittenActions[new AddressRange(address, size)] = func;
		}

		public void Registerwrite(int address, Closure func)
		{
			Register(address, func);
		}

		public void Registerwrite(int address, int size, Closure func)
		{
            Register(address, size, func);
		}

        private void OnOpcodeExecuted(object sender, OpcodeExecutedEventArgs e)
        {
            foreach (var action in _registerExecActions)
            {
                if (action.Key.ConstainsAddress(e.Pc))
                {
                    action.Value.Call(e.Pc, e.Opcode);
                }
            }
        }

		private void OnMemoryWritten(object sender, AddressWrittenEventArgs e)
		{
			foreach (var action in _registerExecActions)
			{
                if (action.Key.ConstainsAddress(e.Address))
				{
                    action.Value.Call(e.Address);
				}
			}
		}
	}
}
