using System;

using Foundation;
using AppKit;
using System.Linq;
using System.Collections.Generic;
using MO5Emulator.Cheats;

namespace MO5Emulator
{
    internal partial class CheatWindow : NSWindow
    {
        private HashSet<int> _adresses = new HashSet<int>();
        private List<Cheat> _cheats = new List<Cheat>();

        private MainWindow MainWindow => NSApplication.SharedApplication.Windows.OfType<MainWindow>().First();

        public CheatWindow(IntPtr handle) : base(handle)
        {
        }

        [Export("initWithCoder:")]
        public CheatWindow(NSCoder coder) : base(coder)
        {
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            var source = new CheatSource(_cheats);
            TableValues.Source = source;
            TableValues.Delegate = new CheatDelegate(source);
        }

        partial void FindValue(NSObject sender)
        {
            var mem = MainWindow.Game.Machine.Memory;
            var value = TextValue.IntValue;
			Func<int, List<int>> find;
			Func<int, int> read;
            if(RadioFormat.State == NSCellStateValue.On){
				find = mem.Find16;
                read = mem.Read16;
            }else{
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
                TextAdressToWrite.IntValue = _adresses.First();
            }
            LabelStatus.StringValue = string.Format("{0} occurrences found ({1})", _adresses.Count, _adresses.FirstOrDefault());
        }

        partial void WriteValue(NSObject sender)
        {
            var mem = MainWindow.Game.Machine.Memory;
            var address = TextAdressToWrite.IntValue;
            var value = TextValueToWrite.IntValue;
            if (RadioFormat.State == NSCellStateValue.On)
            {
                mem.Set16(address, value);
            } 
            else 
            {
                mem.Set(address, value);
            }
        }

        partial void Add(NSObject sender)
        {
            var address = TextAdressToWrite.IntValue;
            var value = TextValueToWrite.IntValue;
            var format = RadioFormat.State == NSCellStateValue.On ?
                                    ByteFormat.Two : ByteFormat.One;
            _cheats.Add(new Cheat(TextDescription.StringValue, address, value, format));
            TableValues.ReloadData();
        }

        partial void Load(NSObject sender)
        {
			var dlg = NSOpenPanel.OpenPanel;
			dlg.CanChooseFiles = true;
			dlg.CanChooseDirectories = false;

			if (dlg.RunModal() == 1)
			{
                _cheats.Clear();
                var serializer = new CheatSerializer();
                _cheats.AddRange(serializer.Load(dlg.Url.Path));
                TableValues.ReloadData();
			}
        }

        partial void Save(NSObject sender)
        {
            var dlg = NSOpenPanel.SavePanel;

			if (dlg.RunModal() == 1)
			{
                var serializer = new CheatSerializer();
                serializer.Save(dlg.Url.Path, _cheats);
			}
        }

        public void UpdateValues()
        {
            var mem = MainWindow.Game.Machine.Memory;
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
