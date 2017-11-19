using System.Collections.Generic;
using MoonSharp.Interpreter;
using nMO5;

namespace MO5Emulator.Scripting
{
    [MoonSharpUserData]
    class LuaInput
    {
        private readonly Machine _machine;

        [MoonSharpHidden]
        public LuaInput(Machine machine)
        {
            _machine = machine;
        }

        public Dictionary<string, object> Get()
        {
            var input = new Dictionary<string, object>();
            input["xmouse"] = _machine.Input.LightPenX;
            input["ymouse"] = _machine.Input.LightPenY;
            input["leftclick"] = _machine.Input.LightPenClick;
            for (int i = 0; i < 256; i++)
            {
                if (_machine.Input.IsKeyPressed((Mo5Key)i))
                {
                    input[ToKeyName((Mo5Key)i)] = true;
                }
            }
            return input;
        }

        public Dictionary<string, object> Read()
        {
            return Get();
        }

        private static string ToKeyName(Mo5Key key)
        {
            if (key >= Mo5Key.D0 && key <= Mo5Key.D9)
            {
                return string.Format("numpad{0}", key - Mo5Key.D0);
            }
            return key.ToString().ToLowerInvariant();
        }
    }
}
