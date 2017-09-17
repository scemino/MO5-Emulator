using System;
using Foundation;

namespace MO5Emulator
{
    [Register("CheatModel")]
    public partial class CheatModel: NSObject
	{
        private string _details;
		private int _address;
		private int _value;
        private int _size = 1;
        private NSMutableArray _cheats = new NSMutableArray();

		[Export("cheatModelArray")]
		public NSArray Cheats
		{
			get { return _cheats; }
		}

        [Export("Details")]
		public string Details
		{
			get { return _details; }
			set
			{
				WillChangeValue("Details");
				_details = value;
				DidChangeValue("Details");
			} 
        }

        [Export("Address")]
        public int Address
        {
            get { return _address; }
            set
            {
                WillChangeValue("Address");
                _address = value;
                DidChangeValue("Address");
            }
        }

        [Export("Value")]
		public int Value
        {
            get { return _value; }
            set
            {
                WillChangeValue("Value");
                _value = value;
                DidChangeValue("Value");
            }
        }

		[Export("Size")]
        public int Size
        {
            get { return _size; }
            set
            {
                WillChangeValue("Size");
                _size = value;
                DidChangeValue("Size");
            }
        }

        public CheatModel()
        {
        }

        public CheatModel(string details, int address, int value, int size)
		{
            Details = details;
			Address = address;
			Value = value;
			Size = size;
		}

		[Export("addObject:")]
		public void AddCheat(CheatModel cheat)
		{
			WillChangeValue("cheatModelArray");
			_cheats.Add(cheat);
			DidChangeValue("cheatModelArray");
		}

		[Export("insertObject:inCheatModelArrayAtIndex:")]
		public void InsertCheat(CheatModel cheat, nint index)
		{
			WillChangeValue("cheatModelArray");
			_cheats.Insert(cheat, index);
			DidChangeValue("cheatModelArray");
		}

		[Export("removeObjectFromCheatModelArrayAtIndex:")]
		public void RemoveCheat(nint index)
		{
			WillChangeValue("cheatModelArray");
			_cheats.RemoveObject(index);
			DidChangeValue("cheatModelArray");
		}

		[Export("setCheatModelArray:")]
		public void SetCheat(NSMutableArray array)
		{
			WillChangeValue("cheatModelArray");
			_cheats = array;
			DidChangeValue("cheatModelArray");
		}

		public override string ToString()
		{
			return string.Format("{3}: Address={0}, Value={1}, Size={2}", Address, Value, Size, Details);
		}
	}
}
