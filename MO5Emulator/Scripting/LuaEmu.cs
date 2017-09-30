using System;
using System.IO;
using MoonSharp.Interpreter;
using nMO5;

namespace MO5Emulator.Scripting
{
    [MoonSharpUserData]
    internal class LuaEmu
    {
        private readonly Machine _machine;

        [MoonSharpHidden]
        public LuaEmu(Machine machine)
        {
            _machine = machine;
        }

        public void Poweron()
        {
            _machine.ResetHard();
        }

		public void Softreset()
		{
            _machine.ResetSoft();
		}

		public void Frameadvance()
		{
			_machine.Step();
			System.Threading.Thread.Sleep(20);
		}

        public int Framecount()
        {
            return _machine.FrameCount;
        }

		public string Getdir()
		{
            return AppDomain.CurrentDomain.BaseDirectory;
		}

		public void Loadrom(string path)
        {
            if (!Path.IsPathRooted(path))
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

            if (!File.Exists(path))
                return;

			var ext = Path.GetExtension(path);
			if (StringComparer.OrdinalIgnoreCase.Equals(ext, ".k7"))
			{
				_machine.OpenK7(path);
				return;
			}
			if (StringComparer.OrdinalIgnoreCase.Equals(ext, ".rom"))
			{
				_machine.OpenMemo(path);
				return;
			}
			if (StringComparer.OrdinalIgnoreCase.Equals(ext, ".fd"))
			{
				_machine.OpenDisk(path);
				return;
			}
            return;
        }

		public void Print(string message)
		{
			System.Console.WriteLine(message);
		}
    }
}
