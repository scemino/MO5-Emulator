using System;
using System.Collections;
using System.Collections.Generic;
using AppKit;

namespace MO5Emulator.Cheats
{
	class CheatSource : NSTableViewSource, IEnumerable<Cheat>
	{
        private readonly List<Cheat> _cheats;

        public CheatSource(List<Cheat> cheats)
        {
            _cheats = cheats;
        }

        public Cheat this[int index] => _cheats[index];

		public IEnumerator<Cheat> GetEnumerator()
		{
			return _cheats.GetEnumerator();
		}

		public override nint GetRowCount(NSTableView tableView)
		{
			return _cheats.Count;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _cheats.GetEnumerator();
		}
	}
}
