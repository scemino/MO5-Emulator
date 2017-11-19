using System;
using System.Threading;
using MO5Emulator.Scripting;
using MoonSharp.Interpreter;
using nMO5;

namespace MO5Emulator
{
    public class ScriptErrorEventArgs: EventArgs
    {
        public Exception Exception { get; }

        public ScriptErrorEventArgs(Exception e)
        {
            Exception = e;
        }
    }

    public class LUAManager
    {
        private Thread _scriptThread;
        private readonly Script _script;
        private readonly Machine _machine;

        public EventHandler<ScriptErrorEventArgs> ScriptError;

        public LUAManager(Machine machine)
        {
            _machine = machine;
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
            _script.Globals["memory"] = new LuaMemory(machine);
            _script.Globals["gui"] = new LuaGui(machine.Screen);
            _script.Globals["emu"] = new LuaEmu(machine);
            _script.Globals["savestate"] = new LuaSaveState(machine);
            _script.Globals["input"] = new LuaInput(machine);
            _script.Globals["debugger"] = new LuaDebugger(machine);
        }

        public void LoadLUAScript(string file)
        {
            if (_scriptThread != null)
            {
                _scriptThread.Abort();
                _machine.IsScriptRunning = false;
            }
            _scriptThread = new Thread(OnScriptRun)
            {
                IsBackground = true
            };
            _machine.IsScriptRunning = true;
            _scriptThread.Start(file);
        }

        private void OnScriptRun(object parameter)
        {
            var file = (string)parameter;
            try
            {
                _script.DoFile(file);
            }
            catch (Exception e)
            {
                ScriptError?.Invoke(this, new ScriptErrorEventArgs(e));
            }
        }
    }
}