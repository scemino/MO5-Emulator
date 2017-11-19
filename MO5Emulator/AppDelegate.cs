using System;
using System.IO;
using System.Runtime.InteropServices;
using AppKit;
using Foundation;
using MO5Emulator.Audio;
using MO5Emulator.Input;
using nMO5;

namespace MO5Emulator
{
    [Register("AppDelegate")]
    public partial class AppDelegate : NSApplicationDelegate
    {
        private Machine _machine;
        private LUAManager _luaManager;

        public Machine Machine => _machine;

        public AppDelegate()
        {
            var sound = new DummySound();
            var input = new CocoaInput();
            var mem = new Memory(input);
            var cpu = new M6809(mem, sound);
            _machine = new Machine(sound, cpu, input, mem);
            mem.Machine = _machine;
            _luaManager = new LUAManager(_machine);
            _luaManager.ScriptError += OnScriptError;
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

        [Export("openLUAScript:")]
        private void OpenLUAScript(NSObject sender)
        {
            var dlg = NSOpenPanel.OpenPanel;
            dlg.CanChooseFiles = true;
            dlg.CanChooseDirectories = false;

            if (dlg.RunModal() == 1)
            {
                _luaManager.LoadLUAScript(dlg.Url.Path);
            }
        }

        [Export("saveScreenshot:")]
        private void SaveScreenshot(NSObject sender)
        {
            var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var path = Path.Combine(desktopDir, string.Format("MO5 Emulator Screenshot {0:u}.png", DateTime.Now));
            var rep = CreateImageRep();
            var pngData = rep.RepresentationUsingTypeProperties(NSBitmapImageFileType.Png, new NSDictionary());
            pngData.Save(path, false);
        }

        private NSBitmapImageRep CreateImageRep()
        {
            var pixels = Machine.Screen.Pixels;
            var pitch = Screen.Width * 4;
            var data = new byte[Screen.Width * Screen.Height * 4];
            for (int x = 0; x < Screen.Width; x++)
            {
                for (int y = 0; y < Screen.Height; y++)
                {
                    var c = pixels[x + y * Screen.Width];
                    data[x * 4 + y * pitch] = c.R;
                    data[x * 4 + 1 + y * pitch] = c.G;
                    data[x * 4 + 2 + y * pitch] = c.B;
                    data[x * 4 + 3 + y * pitch] = c.A;
                }
            }
            var rep = new NSBitmapImageRep(IntPtr.Zero, Screen.Width, Screen.Height,
                                          bps: 8, spp: 4, alpha: true, isPlanar: false,
                                          colorSpaceName: NSColorSpace.CalibratedRGB,
                                          bitmapFormat: NSBitmapFormat.AlphaNonpremultiplied,
                                          rBytes: 4 * Screen.Width,
                                          pBits: 0);
            Marshal.Copy(data, 0, rep.BitmapData, data.Length);
            return rep;
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
            Machine.SotReset();
        }

        [Export("hardReset:")]
        private void HardReset(NSObject sender)
        {
            Machine.HardReset();
        }

        [Export("saveState:")]
        private void SaveState(NSObject sender)
        {
            if (Machine.K7Path == null) return;
            var path = GetStateFilePath();
            using (var stream = File.OpenWrite(path))
            {
                Machine.SaveState(stream);
            }
        }

        [Export("restoreState:")]
        private void RestoreState(NSObject sender)
        {
            if (Machine.K7Path == null) return;
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
                    return Machine.K7Path != null;
                case 2: // restore state
                    return Machine.K7Path != null && File.Exists(GetStateFilePath());
            }

            return true;
        }

        private string GetStateFilePath()
        {
            return Path.ChangeExtension(Machine.K7Path, ".m5s");
        }

        private void OnScriptError(object sender, ScriptErrorEventArgs e)
        {
            InvokeOnMainThread(() =>
            {
                var msgFormat = NSBundle.MainBundle.LocalizedString("An unknown error occured in the LUA script!\n{0}.", null);
                var message = string.Format(msgFormat, e.Exception.Message);
                var alert = new NSAlert
                {
                    AlertStyle = NSAlertStyle.Critical,
                    InformativeText = message,
                    MessageText = NSBundle.MainBundle.LocalizedString("Oops", null),
                };
                alert.RunModal();
                Machine.IsScriptRunning = false;
            });
        }
    }
}