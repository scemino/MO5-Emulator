using System;
using System.IO;
using AppKit;
using Foundation;
using MO5Emulator.Audio;
using nMO5;

namespace MO5Emulator
{
    [Register("AppDelegate")]
    public partial class AppDelegate : NSApplicationDelegate
    {
        private ISound _sound;
        private Machine _machine;

		public Machine Machine => _machine;
		public ISound Sound => _sound;

        public AppDelegate()
        {
			_sound = new DummySound();
			_machine = new Machine(_sound);
        }

        public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender)
        {
            return true;
        }

        public override bool OpenFile(NSApplication sender, string filename)
        {
            var mainWindow = NSApplication.SharedApplication.MainWindow as MainWindow;
            if (mainWindow == null) return false;
            return OpenFile(filename);
        }

		[Export("openDocument:")]
		private void OpenDialog(NSObject sender)
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

		private bool OpenFile(string path)
		{
			var ext = Path.GetExtension(path);
			if (StringComparer.OrdinalIgnoreCase.Equals(ext, ".k7"))
			{
				Machine.OpenK7(path);
				return true;
			}
			if (StringComparer.OrdinalIgnoreCase.Equals(ext, ".rom"))
			{
				Machine.OpenMemo(path);
				return true;
			}
			if (StringComparer.OrdinalIgnoreCase.Equals(ext, ".fd"))
			{
				Machine.OpenDisk(path);
				return true;
			}
			return false;
		}

		[Export("softReset:")]
		private void SoftReset(NSObject sender)
		{
			Machine.ResetSoft();
		}

		[Export("hardReset:")]
		private void HardReset(NSObject sender)
		{
			Machine.ResetHard();
		}

        [Export("saveState:")]
        private void SaveState(NSObject sender)
        {
            if (Machine.Memory.K7Path == null) return;
            var path = GetStateFilePath();
            using (var stream = File.OpenWrite(path))
            {
                Machine.SaveState(stream);
            }
        }

        [Export("restoreState:")]
        private void RestoreState(NSObject sender)
        {
            if (Machine.Memory.K7Path == null) return;
            var path = GetStateFilePath();
            if (!File.Exists(path)) return;

            using (var stream = File.OpenRead(path))
            {
                Machine.RestoreState(stream);
            }
        }

		[Action("validateMenuItem:")]
		private bool ValidateMenuItem(NSMenuItem item)
		{
			switch (item.Tag)
			{
				case 1: // save state
                    return Machine.Memory.K7Path != null;
				case 2: // restore state
                    return Machine.Memory.K7Path != null && File.Exists(GetStateFilePath());
			}

			return true;
		}

		private string GetStateFilePath()
		{
			return Path.ChangeExtension(Machine.Memory.K7Path, ".m5s");
		}
    }
}