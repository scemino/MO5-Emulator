using System;
using AppKit;

namespace MO5Emulator.Cheats
{
	class CheatDelegate : NSTableViewDelegate
	{
        private readonly CheatSource _source;

        public CheatDelegate(CheatSource source)
		{
			_source = source;
		}

		public override NSView GetViewForItem(NSTableView tableView, NSTableColumn tableColumn, nint row)
		{
			var view = (NSTextField)tableView.MakeView("cheat", this);
			if (view == null)
			{
				view = new NSTextField
				{
					Identifier = "cheat",
					Bordered = false,
					Editable = false
				};
			}

            switch (tableColumn.Identifier)
            {
                case "description":
                    view.StringValue = _source[(int)row].Description;
                    break;
                case "address":
                    view.StringValue = _source[(int)row].Address.ToString("X");
                    break;
                case "size":
                    view.IntValue = _source[(int)row].Format == ByteFormat.Two ? 2 : 1;
                    break;
                case "value":
                    view.StringValue = _source[(int)row].Value.ToString("X");
                    break;
            }

            return view;
		}
	}
}
