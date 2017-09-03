using System;
using AppKit;
using nMO5;
using System.Collections.Generic;
using Foundation;
using System.Linq;

namespace MO5Emulator
{
    public partial class CheatViewController : NSViewController
    {
        AppDelegate AppDelegate => (AppDelegate)NSApplication.SharedApplication.Delegate;
        private Memory Memory => AppDelegate.Machine.Memory;

        private HashSet<int> _adresses = new HashSet<int>();
		private NSMutableArray _cheats = new NSMutableArray();

		public CheatModel SelectedCheat { get; private set; }

		[Export("cheatModelArray")]
		public NSArray Cheats => _cheats;

		public CheatViewController(IntPtr handle) : base(handle)
		{
		}

        public override void ViewWillAppear()
        {
			AppDelegate.Machine.Stepping += OnStepping;
			base.ViewWillAppear();
        }

        public override void ViewWillDisappear()
        {
            AppDelegate.Machine.Stepping -= OnStepping;
            base.ViewWillDisappear();
        }

        private void OnStepping(object sender, EventArgs e)
        {
            UpdateValues();
        }

		public override void PrepareForSegue(NSStoryboardSegue segue, NSObject sender)
		{
			base.PrepareForSegue(segue, sender);

			// Take action based on type
			switch (segue.Identifier)
			{
				case "EditorSegue":
                    var editor = segue.DestinationController as AddCheatViewController;
					editor.Presentor = this;
					editor.Cheat = SelectedCheat;
					break;
			}
		}

		public void DeleteCheat(NSWindow window)
		{
			if (CheatTableView.SelectedRow == -1)
			{
				var alert = new NSAlert
				{
					AlertStyle = NSAlertStyle.Critical,
                    InformativeText = NSBundle.MainBundle.LocalizedString("Please select the cheat to remove from the list of cheats.", null),
                    MessageText = NSBundle.MainBundle.LocalizedString("Delete Cheat", null),
				};
				alert.BeginSheet(window);
			}
			else
			{
				SelectedCheat = _cheats.GetItem<CheatModel>((nuint)CheatTableView.SelectedRow);
                var message = NSBundle.MainBundle.LocalizedString("Are you sure you want to delete cheat `{0}` from the table?", null);
				// Confirm delete
				var alert = new NSAlert
				{
					AlertStyle = NSAlertStyle.Critical,
					InformativeText = string.Format(message, SelectedCheat.Address),
                    MessageText = NSBundle.MainBundle.LocalizedString("Delete Cheat", null),
				};
                alert.AddButton(NSBundle.MainBundle.LocalizedString("OK", null));
                alert.AddButton(NSBundle.MainBundle.LocalizedString("Cancel", null));
				alert.BeginSheetForResponse(window, (result) =>
				{
					// Delete?
					if (result == 1000)
					{
						RemoveCheat(CheatTableView.SelectedRow);
					}
				});
			}
		}

		public void EditCheat(NSWindow window)
		{
            if (CheatTableView.SelectedRow == -1)
			{
				var alert = new NSAlert
				{
					AlertStyle = NSAlertStyle.Informational,
                    InformativeText = NSBundle.MainBundle.LocalizedString("Please select the cheat to edit from the list of cheats.", null),
                    MessageText = NSBundle.MainBundle.LocalizedString("Edit Cheat", null),
				};
				alert.BeginSheet(window);
			}
			else
			{
                SelectedCheat = _cheats.GetItem<CheatModel>((nuint)CheatTableView.SelectedRow);
				PerformSegue("EditorSegue", this);
			}
		}

        public void LoadCheats(NSWindow window)
        {
			var dlg = NSOpenPanel.OpenPanel;
			dlg.CanChooseFiles = true;
			dlg.CanChooseDirectories = false;

			if (dlg.RunModal() == 1)
			{
				var serializer = new CheatSerializer();
                var items = new NSMutableArray();
                items.AddObjects(serializer.Load(dlg.Url.Path).ToArray());
                SetCheat(items);
			}
        }

        public void SaveCheats(NSWindow window)
		{
		    var dlg = NSSavePanel.SavePanel;

		    if (dlg.RunModal() == 1)
            {
                var serializer = new CheatSerializer();
                var cheats = GetCheats().ToList();
                serializer.Save(dlg.Url.Path, cheats);
            }
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

		// TODO
		//partial void FindValue(NSButton sender)
		//{
		//    var mem = Memory;
		//    var value = ValueTextField.IntValue;
		//    Func<int, List<int>> find;
		//    Func<int, int> read;
		//    if (RadioFormat.State == NSCellStateValue.On)
		//    {
		//        find = mem.Find16;
		//        read = mem.Read16;
		//    }
		//    else
		//    {
		//        find = mem.Find8;
		//        read = mem.Read;
		//    }
		//    if (_adresses.Count == 0)
		//    {
		//        var newValues = find(value);
		//        foreach (var addr in newValues)
		//        {
		//            _adresses.Add(addr);
		//        }
		//    }
		//    else
		//    {
		//        foreach (var addr in _adresses.ToList())
		//        {
		//            if (read(addr) != value)
		//            {
		//                _adresses.Remove(addr);
		//            }
		//        }
		//    }
		//    if (_adresses.Count == 1)
		//    {
		//        AdressTextField.IntValue = _adresses.First();
		//    }
		//    StatusTextField.StringValue = string.Format("{0} occurrences found ({1})", _adresses.Count, _adresses.FirstOrDefault());
		//}

		// TODO
		//partial void WriteValue(NSButton sender)
		//{
		//    var mem = Memory;
		//    var address = AdressTextField.IntValue;
		//    var value = ValueTextField.IntValue;
		//    if (RadioFormat.State == NSCellStateValue.On)
		//    {
		//        mem.Set16(address, value);
		//    }
		//    else
		//    {
		//        mem.Set(address, value);
		//    }
		//}

		private IEnumerable<CheatModel> GetCheats()
		{
			for (nuint i = 0; i < _cheats.Count; i++)
			{
				yield return _cheats.GetItem<CheatModel>(i);
			}
		}

		private void UpdateValues()
        {
            var mem = Memory;
            foreach (var cheat in GetCheats())
            {
                if (cheat.Size == 1)
                {
                    mem.Set(cheat.Address, cheat.Value);
                }
                else
                {
                    mem.Set16(cheat.Address, cheat.Value);
                }
            }
        }
    }
}
