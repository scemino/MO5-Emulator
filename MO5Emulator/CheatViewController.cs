using System;
using AppKit;
using nMO5;
using System.Linq;
using System.Collections.Generic;
using MO5Emulator.Cheats;

namespace MO5Emulator
{
    public partial class CheatViewController : NSViewController
    {
        AppDelegate AppDelegate => (AppDelegate)NSApplication.SharedApplication.Delegate;
        private Memory Memory => AppDelegate.Machine.Memory;

        private HashSet<int> _adresses = new HashSet<int>();
        private List<Cheat> _cheats = new List<Cheat>();

        public CheatViewController(IntPtr handle) : base(handle)
        {
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

			var source = new CheatSource(_cheats);
            CheatTableView.Source = source;
			CheatTableView.Delegate = new CheatDelegate(source);
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

        partial void FindValue(NSButton sender)
        {
            var mem = Memory;
            var value = ValueTextField.IntValue;
            Func<int, List<int>> find;
            Func<int, int> read;
            if (RadioFormat.State == NSCellStateValue.On)
            {
                find = mem.Find16;
                read = mem.Read16;
            }
            else
            {
                find = mem.Find8;
                read = mem.Read;
            }
            if (_adresses.Count == 0)
            {
                var newValues = find(value);
                foreach (var addr in newValues)
                {
                    _adresses.Add(addr);
                }
            }
            else
            {
                foreach (var addr in _adresses.ToList())
                {
                    if (read(addr) != value)
                    {
                        _adresses.Remove(addr);
                    }
                }
            }
            if (_adresses.Count == 1)
            {
                AdressTextField.IntValue = _adresses.First();
            }
            StatusTextField.StringValue = string.Format("{0} occurrences found ({1})", _adresses.Count, _adresses.FirstOrDefault());
        }

        partial void WriteValue(NSButton sender)
        {
            var mem = Memory;
            var address = AdressTextField.IntValue;
            var value = ValueTextField.IntValue;
            if (RadioFormat.State == NSCellStateValue.On)
            {
                mem.Set16(address, value);
            }
            else
            {
                mem.Set(address, value);
            }
        }

        partial void AddCheat(NSButton sender)
        {
            var address = AdressTextField.IntValue;
            var value = ValueTextField.IntValue;
            var format = RadioFormat.State == NSCellStateValue.On ?
                                    ByteFormat.Two : ByteFormat.One;
            _cheats.Add(new Cheat(DescriptionTextField.StringValue, address, value, format));
            CheatTableView.ReloadData();
        }

        partial void LoadCheats(NSButton sender)
        {
            var dlg = NSOpenPanel.OpenPanel;
            dlg.CanChooseFiles = true;
            dlg.CanChooseDirectories = false;

            if (dlg.RunModal() == 1)
            {
                _cheats.Clear();
                var serializer = new CheatSerializer();
                _cheats.AddRange(serializer.Load(dlg.Url.Path));
                CheatTableView.ReloadData();
            }
        }

        partial void SaveCheats(NSButton sender)
        {
            var dlg = NSSavePanel.SavePanel;

            if (dlg.RunModal() == 1)
            {
                var serializer = new CheatSerializer();
                serializer.Save(dlg.Url.Path, _cheats);
            }
        }

        private void UpdateValues()
        {
            var mem = Memory;
            foreach (var cheat in _cheats)
            {
                if (cheat.Format == ByteFormat.One)
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
