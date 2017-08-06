using System;
using Foundation;
using AppKit;

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

            // Run the game at 60 updates per second
            Game.Run(60.0);
        }

        public override void KeyDown(NSEvent theEvent)
        {
            Game.KeyDown(theEvent);
        }

		public override void KeyUp(NSEvent theEvent)
		{
			Game.KeyUp(theEvent);
		}
    }
}
