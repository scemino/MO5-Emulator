using System;
using Foundation;
using AppKit;

namespace MO5Emulator
{
    public partial class CheatWindowController : NSWindowController
    {
        public CheatWindowController(IntPtr handle) : base(handle)
        {
        }

        [Export("initWithCoder:")]
        public CheatWindowController(NSCoder coder) : base(coder)
        {
        }

        public CheatWindowController() : base("CheatWindow")
        {
        }
    }
}
