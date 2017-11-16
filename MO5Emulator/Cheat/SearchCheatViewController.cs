// This file has been autogenerated from a class added in the UI designer.

using System;
using System.Collections.Generic;
using System.Linq;
using AppKit;
using Foundation;
using nMO5;

namespace MO5Emulator.Base.lproj
{
    public partial class SearchCheatViewController : NSViewController
    {
        private AppDelegate AppDelegate => (AppDelegate)NSApplication.SharedApplication.Delegate;
        private IMemory Memory => AppDelegate.Machine.Memory;

        private Dictionary<int, MemorySearchSession> _values = new Dictionary<int, MemorySearchSession>();
        private HashSet<int> _adresses = new HashSet<int>();
        private int _byteSize = 1;
        private MemoryComparisonOperator _operator = MemoryComparisonOperator.Equals;
        private MemoryComparisonMode _mode = MemoryComparisonMode.SpecificValue;

        public SearchCheatViewController(IntPtr handle) : base(handle)
        {
            Memory.Written += OnWritten;
        }

        private static bool Compare(int left, int right, MemoryComparisonOperator op)
        {
            switch (op)
            {
                case MemoryComparisonOperator.Lower:
                    return left < right;
                case MemoryComparisonOperator.Greater:
                    return left > right;
                case MemoryComparisonOperator.LowerOrEquals:
                    return left <= right;
                case MemoryComparisonOperator.GreaterOrEquals:
                    return left >= right;
                case MemoryComparisonOperator.Equals:
                    return left == right;
                case MemoryComparisonOperator.NotEquals:
                    return left != right;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op));
            }
        }

        private void OnWritten(object sender, AddressWrittenEventArgs e)
        {
            if (e.Size == 2)
            {
                _values.TryGetValue(e.Address, out MemorySearchSession value);
                _values[e.Address] = new MemorySearchSession(value.Value, e.Value, 2, value.Changes + 1);
            }
            else
            {
                _values.TryGetValue(e.Address, out MemorySearchSession value);
                _values[e.Address] = new MemorySearchSession(value.PreviousValue, e.Value, 1, value.Changes + 1);
            }
        }

        partial void ByteSize(NSButton sender)
        {
            _byteSize = (int)sender.Tag;
            ((NSNumberFormatter)ValueTextField.Formatter).Minimum = 0;
            ((NSNumberFormatter)ValueTextField.Formatter).Maximum = Math.Pow(2, _byteSize * 8) - 1;
            Reset();
        }

        partial void ChangeOperator(NSButton sender)
        {
            _operator = (MemoryComparisonOperator)(int)sender.Tag - 1;
        }

        partial void ChangeComparisonMode(NSButton sender)
        {
            _mode = (MemoryComparisonMode)(int)sender.Tag - 1;
        }

        partial void Cancel(NSButton sender)
        {
            DismissViewController(this);
        }

        partial void Restart(NSButton sender)
        {
            Reset();
        }

        partial void Search(NSButton sender)
        {
            Search();
        }

        private void Search()
        {
            var mem = Memory;
            var value = ValueTextField.IntValue;

            foreach (var addr in _adresses.ToList())
            {
                var curValue = Read(addr, _byteSize);
                if (!_values.TryGetValue(addr, out MemorySearchSession session))
                {
                    session = new MemorySearchSession(curValue, curValue, _byteSize, 0);
                }
                var relativeValue = _mode == MemoryComparisonMode.SpecificValue ? value : session.PreviousValue;
                if (!Compare(curValue, relativeValue, _operator))
                {
                    _adresses.Remove(addr);
                }
                else
                {
                    _values[addr] = new MemorySearchSession(curValue, curValue, _byteSize, session.Changes);
                }
            }

            var msg = NSBundle.MainBundle.LocalizedString("{0} results found ({1:X})", null);
            StatusTextField.StringValue = string.Format(msg, _adresses.Count, _adresses.FirstOrDefault());
        }

        private void Reset()
        {
            if (StatusTextField != null)
            {
                StatusTextField.StringValue = string.Empty;
            }
            _adresses.Clear();
            _values.Clear();

            for (int addr = 0x2200; addr <= 0x9FFF; addr++)
            {
                _adresses.Add(addr);
                var value = Read(addr, _byteSize);
                _values[addr] = new MemorySearchSession(value, value, _byteSize, 0);
            }
        }

        private int Read(int address, int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), count, "count should be positive");
            if (count > 4) throw new ArgumentOutOfRangeException(nameof(count), count, "count should be lower than 5");

            var memValue = Memory.Read(address);
            for (var i = 1; i < count; i++)
            {
                memValue <<= 8;
                memValue |= Memory.Read(address + i);
            }
            return memValue;
        }

        public override void PrepareForSegue(NSStoryboardSegue segue, NSObject sender)
        {
            base.PrepareForSegue(segue, sender);

            switch (segue.Identifier)
            {
                case "ViewAdresses":
                    if (_adresses.Count == 0)
                    {
                        Search();
                    }

                    var addressesViewController = segue.DestinationController as AddressesViewController;
                    var cheats = new NSMutableArray<CheatModel>();
                    foreach (var address in _adresses)
                    {
                        cheats.Add(new CheatModel { Address = address, Size = _byteSize });
                    }
                    addressesViewController.SetCheat(cheats);
                    break;
            }
        }
    }
}
