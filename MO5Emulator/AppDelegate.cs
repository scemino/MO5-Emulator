using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using AppKit;
using Foundation;
using MO5Emulator.Audio;
using MO5Emulator.Scripting;
using MoonSharp.Interpreter;
using nMO5;

namespace MO5Emulator
{
    [Register("AppDelegate")]
    public partial class AppDelegate : NSApplicationDelegate
    {
        private ISound _sound;
        private Machine _machine;
        private Thread _scriptThread;
        private Script _script;
        private Screen _screen;

        public Machine Machine => _machine;
        public ISound Sound => _sound;
        public Screen Screen => _screen;

        public AppDelegate()
        {
            _sound = new DummySound();
            _machine = new Machine(_sound);
            _screen = new Screen(Machine.Memory);

            UserData.RegisterType<LuaMemory>();
            UserData.RegisterType<LuaGui>();
            UserData.RegisterType<LuaColor>();
            UserData.RegisterType<LuaEmu>();
            UserData.RegisterType<LuaSaveSlot>();
            UserData.RegisterType<LuaSaveState>();
            UserData.RegisterType<LuaInput>();
            UserData.RegisterType<LuaDebugger>();

            Script.GlobalOptions.CustomConverters
                  .SetScriptToClrCustomConversion(DataType.String,
                                                  typeof(LuaColor),
                                                  (DynValue arg) => LuaColor.Parse(arg.String));
            Script.GlobalOptions.CustomConverters
                  .SetScriptToClrCustomConversion(DataType.Table,
                                                  typeof(LuaColor),
                                                  (DynValue arg) => LuaColor.Parse(arg.Table));
            Script.GlobalOptions.CustomConverters
                  .SetScriptToClrCustomConversion(DataType.Number,
                                                  typeof(LuaColor),
                                                  (DynValue arg) => LuaColor.Parse((int)arg.Number));

            _script = new Script();
            _script.Globals["memory"] = new LuaMemory(Machine);
            _script.Globals["gui"] = new LuaGui(Screen);
            _script.Globals["emu"] = new LuaEmu(Machine);
            _script.Globals["savestate"] = new LuaSaveState(Machine);
            _script.Globals["input"] = new LuaInput(Machine);
            _script.Globals["debugger"] = new LuaDebugger(Machine);
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
                LoadLUAScript(dlg.Url.Path);
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
            var pixels = _screen.Pixels;
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

        private void LoadLUAScript(string file)
        {
            if (_scriptThread != null)
            {
                _scriptThread.Abort();
                Machine.IsScriptRunning = false;
            }
            _scriptThread = new Thread(OnScriptRun)
            {
                IsBackground = true
            };
            Machine.IsScriptRunning = true;
            _scriptThread.Start(file);
        }

        private void OnScriptRun(object parameter)
        {
            var file = (string)parameter;
            try
            {
                _script.DoFile(file);
            }
            catch (InterpreterException e)
            {
                InvokeOnMainThread(() =>
                {
                    var msgFormat = NSBundle.MainBundle.LocalizedString("An error occured in the LUA script!\n{0}.", null);
                    var message = string.Format(msgFormat, e.DecoratedMessage);
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
            catch (Exception e)
            {
                InvokeOnMainThread(() =>
                {
                    var msgFormat = NSBundle.MainBundle.LocalizedString("An unknown error occured in the LUA script!\n{0}.", null);
                    var message = string.Format(msgFormat, e.Message);
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
}