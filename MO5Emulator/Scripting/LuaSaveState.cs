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

		public int Number => _slot;

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

        public void Save(LuaSaveSlot slot)
        {
            if (slot == null)
                return;
            var stream = new MemoryStream();
            slot.Stream = stream;
            _machine.SaveState(stream);
			if (slot.IsPersistent && !slot.IsAnonymous)
			{
                File.WriteAllBytes(GetStateFilePath(slot.Number), stream.ToArray());
			}
        }

		public void Load(LuaSaveSlot slot)
		{
            if (slot == null)
                return;
            if (slot.Stream == null && _machine.K7Path != null)
            {
                var path = GetStateFilePath(slot.Number);
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

        public void Persist(LuaSaveSlot saveState)
        {
            saveState.IsPersistent = true;
        }

		private string GetStateFilePath(int slot)
		{
            string path;
            if (slot == 0)
            {
                path = _machine.K7Path;
            }
            else
            {
                path = string.Format("{0}{1}", _machine.K7Path, slot);
            }
			return Path.ChangeExtension(path, ".m5s");
		}
    }
}
