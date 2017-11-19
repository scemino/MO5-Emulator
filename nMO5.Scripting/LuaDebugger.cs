using MoonSharp.Interpreter;
using nMO5;

namespace MO5Emulator.Scripting
{
    [MoonSharpUserData]
    internal class LuaDebugger
    {
        private readonly Machine _machine;

        [MoonSharpHidden]
        public LuaDebugger(Machine machine)
        {
            _machine = machine;
        }

        public int Getcyclescount()
        {
            return _machine.Cpu.CyclesCount;
        }

        public int Getinstructionscount()
        {
            return _machine.Cpu.InstructionsCount;
        }

		public void Resetcyclescount()
		{
            _machine.Cpu.ResetClock();
		}

		public void Resetinstructionscount()
		{
			_machine.Cpu.ResetInstructionsCount();
		}
    }
}
