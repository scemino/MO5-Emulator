using System.Linq;
using AppKit;
using Foundation;

namespace MO5Emulator
{
    [Register("AppDelegate")]
    public partial class AppDelegate : NSApplicationDelegate
    {
        private GameView Game => NSApplication.SharedApplication.Windows.OfType<MainWindow>().First().Game;
        CheatWindowController _cheatController;

        public AppDelegate()
        {
            _cheatController = new CheatWindowController();
        }

        partial void HardReset(NSMenuItem sender)
        {
            Game.HardReset();
        }

        partial void SoftReset(NSMenuItem sender)
        {
            Game.SoftReset();
        }

        partial void Debug(NSMenuItem sender)
        {
			_cheatController.Window.MakeKeyAndOrderFront(this);
        }

        [Export("openDocument:")]
        void OpenDialog(NSObject sender)
        {
			var dlg = NSOpenPanel.OpenPanel;
			dlg.CanChooseFiles = true;
			dlg.CanChooseDirectories = false;

			if (dlg.RunModal() == 1)
			{
                Game.OpenK7(dlg.Url.Path);
                NSDocumentController.SharedDocumentController.NoteNewRecentDocumentURL(dlg.Url);
            }
        }

        public override bool OpenFile(NSApplication sender, string filename)
        {
            Game.OpenK7(filename);
            return true;
        }

        public override void DidFinishLaunching(NSNotification notification)
        {
            // Insert code here to initialize your application
        }

        public override void WillTerminate(NSNotification notification)
        {
            // Insert code here to tear down your application
        }
    }
}