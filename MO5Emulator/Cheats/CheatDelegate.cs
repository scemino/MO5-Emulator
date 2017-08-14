using System;
using AppKit;

namespace MO5Emulator.Cheats
{
	class CheatDelegate : NSTableViewDelegate
	{
		CheatSource _source;

		public CheatDelegate(CheatSource source)
		{
			_source = source;
		}

		public override NSView GetViewForItem(NSTableView tableView, NSTableColumn tableColumn, nint row)
		{
			var view = (NSTextField)tableView.MakeView("cheat", this);
			if (view == null)
			{
				view = new NSTextField()
				{
					Identifier = "cheat",
					Bordered = false,
					Editable = false
				};
			}
			if (tableColumn.Identifier == "description")
			{
				view.StringValue = _source[(int)row].Description;
			}
			else if (tableColumn.Identifier == "address")
			{
				view.StringValue = _source[(int)row].Address.ToString("X");
			}
			else if (tableColumn.Identifier == "size")
			{
				view.IntValue = _source[(int)row].Format == ByteFormat.Two ? 2 : 1;
			}
			else if (tableColumn.Identifier == "value")
			{
				view.StringValue = _source[(int)row].Value.ToString("X");
			}
			return view;
		}
	}
}
