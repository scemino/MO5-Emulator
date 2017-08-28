using System;
using System.IO;
using System.Linq;
using AppKit;
using Foundation;

namespace MO5Emulator
{
    [Register("AppDelegate")]
    public partial class AppDelegate : NSApplicationDelegate
    {
        private GameView Game => NSApplication.SharedApplication.Windows.OfType<MainWindow>().First().Game;
        private CheatWindowController _cheatController;

        public AppDelegate()
        {
            _cheatController = new CheatWindowController();
        }

        public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender)
        {
            return true;
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
                OpenFile(dlg.Url.Path);
                NSDocumentController.SharedDocumentController.NoteNewRecentDocumentURL(dlg.Url);
            }
        }

        private void OpenFile(string path)
        {
            var ext = Path.GetExtension(path);
            if (StringComparer.OrdinalIgnoreCase.Equals(ext, ".k7"))
            {
                Game.OpenK7(path);
            }
            else if (StringComparer.OrdinalIgnoreCase.Equals(ext, ".rom"))
            {
                Game.OpenMemo(path);
            }
			else if (StringComparer.OrdinalIgnoreCase.Equals(ext, ".fd"))
			{
				Game.OpenDisk(path);
			}
        }

        public override bool OpenFile(NSApplication sender, string filename)
        {
            OpenFile(filename);
            return true;
        }
    }
}