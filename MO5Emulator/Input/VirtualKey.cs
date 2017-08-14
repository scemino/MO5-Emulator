using nMO5;

namespace MO5Emulator.Input
{
	class VirtualKey
	{
		public Mo5Key Key { get; }
		public bool? ShiftKey { get; }

		public VirtualKey(Mo5Key key, bool? shiftKey = null)
		{
			Key = key;
			ShiftKey = shiftKey;
		}
	}
}
