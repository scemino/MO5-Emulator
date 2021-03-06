using System;
using Foundation;
using AppKit;
using System.IO;

namespace MO5Emulator
{
    public partial class MainWindow : NSWindow
    {
        public GameView Game { get; set; }

        public MainWindow(IntPtr handle) : base(handle)
        {
        }

        [Export("initWithCoder:")]
        public MainWindow(NSCoder coder) : base(coder)
        {
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            // Create new Game View and replace the window content with it
            Game = new GameView(ContentView.Frame);
            ContentView = Game;

            UpdateTitle();
            Game.Machine.IndexChanged += OnIndexChanged;

            // Run the game at 60 updates per second
            Game.Run(60.0);
        }

        private void OnIndexChanged(object sender, EventArgs e)
        {
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            InvokeOnMainThread(() =>
            {
                // K7 ?
                if (!string.IsNullOrEmpty(Game.Machine.K7Path))
                {
                    Title = string.Format("{0} [{1}/{2}]",
                                          Path.GetFileNameWithoutExtension(Game.Machine.K7Path),
                                          Game.Machine.Index, Game.Machine.IndexMax);
                    return;
                }
                // Memo ?
                if (!string.IsNullOrEmpty(Game.Machine.MemoPath))
                {
                    Title = Path.GetFileNameWithoutExtension(Game.Machine.MemoPath);
                    return;
                }
                // Disk ?
                if (!string.IsNullOrEmpty(Game.Machine.DiskPath))
                {
                    Title = Path.GetFileNameWithoutExtension(Game.Machine.DiskPath);
                }
            });
        }

        public override void FlagsChanged(NSEvent theEvent)
        {
            Game.FlagsChanged(theEvent);
        }

        public override void KeyDown(NSEvent theEvent)
        {
            Game.KeyDown(theEvent);
        }

		public override void KeyUp(NSEvent theEvent)
		{
			Game.KeyUp(theEvent);
		}

        public override void PerformClose(NSObject sender)
        {
            base.PerformClose(sender);
            NSApplication.SharedApplication.Terminate(NSApplication.SharedApplication);
        }
    }
}
