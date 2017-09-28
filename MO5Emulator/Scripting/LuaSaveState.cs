using System.IO;
using MoonSharp.Interpreter;
using nMO5;

namespace MO5Emulator.Scripting
{
    [MoonSharpUserData]
    class LuaSaveSlot
    {
        private readonly int _slot;

        [MoonSharpHidden]
        public Stream Stream { get; set; }

        public bool IsPersistent { get; set; }

        public bool IsAnonymous => _slot < 0 || _slot > 9;

        [MoonSharpHidden]
        public LuaSaveSlot(int slot)
        {
            _slot = slot;
        }
    }

    [MoonSharpUserData]
    internal class LuaSaveState
    {
        private readonly Machine _machine;

        [MoonSharpHidden]
        public LuaSaveState(Machine machine)
        {
            _machine = machine;
        }

		/// <summary>
		/// Create a new savestate object.
		/// </summary>
		/// <returns>The object.</returns>
		/// <param name="slot">Slot.</param>
		/// <remarks>
		/// Optionally you can save the current state to one of the predefined slots(1-10) using the range 1-9 for slots 1-9, and 10 for 0, QWERTY style. Using no number will create an "anonymous" savestate.
		/// Note that this does not actually save the current state! You need to create this value and pass it on to the load and save functions in order to save it.
		/// 
		/// Anonymous savestates are temporary, memory only states.You can make them persistent by calling memory.persistent(state). Persistent anonymous states are deleted from disk once the script exits.
		/// </remarks>
		public LuaSaveSlot Object(int? slot = null)
        {
            var curSlot = slot.HasValue ? slot.Value : -1;
            if (curSlot == 10)
                curSlot = 0;
            return new LuaSaveSlot(curSlot);
        }

        public void Save(LuaSaveSlot saveState)
        {
            var slot = saveState as LuaSaveSlot;
            if (slot == null)
                return;
            var stream = new MemoryStream();
            slot.Stream = stream;
            _machine.SaveState(stream);
        }

		public void Load(LuaSaveSlot saveState)
		{
			var slot = saveState as LuaSaveSlot;
            if (slot == null)
                return;
            if (slot.Stream == null && _machine.Memory.K7Path != null)
            {
                var path = GetStateFilePath();
                if (File.Exists(path))
                {
                    slot.Stream = File.OpenRead(path);
                }
            }
            if (slot.Stream == null)
                return;
			_machine.RestoreState(slot.Stream);
            if(!slot.IsPersistent)
            {
                slot.Stream.Dispose();
                slot.Stream = null;
            }
		}

		private string GetStateFilePath()
		{
			return Path.ChangeExtension(_machine.Memory.K7Path, ".m5s");
		}
    }
}
