using AppKit;
using Foundation;

namespace MO5Emulator
{
    [Register("AppDelegate")]
    public partial class AppDelegate : NSApplicationDelegate
    {
        private GameView Game => ((MainWindow)NSApplication.SharedApplication.MainWindow).Game;

        public AppDelegate()
        {
        }

        partial void HardReset(NSMenuItem sender)
        {
            Game.HardReset();
        }

        partial void SoftReset(NSMenuItem sender)
        {
            Game.SoftReset();
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
			}
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