using System;
using AppKit;
using nMO5;

namespace MO5Emulator.Input
{
    internal class CocoaInput : IInput
    {
        private readonly bool[] _key;

        public JoystickOrientation Joystick1Orientation { get; private set; }
        public JoystickOrientation Joystick2Orientation { get; private set; }
        public bool Joystick1ButtonPressed { get; private set; }
        public bool Joystick2ButtonPressed { get; private set; }

        public int LightPenX { get; private set; }
        public int LightPenY { get; private set; }
        public bool LightPenClick { get; private set; }

        public CocoaInput()
        {
            _key = new bool[256];
        }

        public bool IsKeyPressed(Mo5Key key)
        {
            return _key[(int)key];
        }

        public void FlagsChanged(NSEvent theEvent)
        {
            if (theEvent.ModifierFlags.HasFlag(NSEventModifierMask.ShiftKeyMask))
            {
                KeyPressed(Mo5Key.Shift);
            }
            else
            {
                ClearKeys();
            }

            if (theEvent.ModifierFlags.HasFlag(NSEventModifierMask.ControlKeyMask))
            {
                KeyPressed(Mo5Key.Control);
            }
            else
            {
                KeyReleased(Mo5Key.Control);
            }

            if (theEvent.ModifierFlags.HasFlag(NSEventModifierMask.CommandKeyMask))
            {
                KeyPressed(Mo5Key.Basic);
            }
            else
            {
                KeyReleased(Mo5Key.Basic);
            }
        }

        public void KeyDown(NSEvent theEvent)
        {
            if (theEvent.CharactersIgnoringModifiers.Length == 0) return;
            var c = theEvent.CharactersIgnoringModifiers[0];
            Do(c, true);

            if (KeyMappings.Joystick1Orientations.TryGetValue(c, out JoystickOrientation orientation))
            {
                Joystick1Orientation |= orientation;
            }

            if (c == KeyMappings.Joystick1Button)
            {
                Joystick1ButtonPressed = true;
            }

            if (KeyMappings.Joystick2Orientations.TryGetValue(c, out orientation))
            {
                Joystick2Orientation |= orientation;
            }

            if (c == KeyMappings.Joystick2Button)
            {
                Joystick2ButtonPressed = true;
            }
        }

        public void KeyUp(NSEvent theEvent)
        {
            if (theEvent.CharactersIgnoringModifiers.Length == 0) return;
            var c = theEvent.CharactersIgnoringModifiers[0];
            Do(c, false);

            if (KeyMappings.Joystick1Orientations.TryGetValue(c, out JoystickOrientation orientation))
            {
                Joystick1Orientation &= ~orientation;
            }

            if (c == KeyMappings.Joystick1Button)
            {
                Joystick1ButtonPressed = false;
            }

            if (KeyMappings.Joystick2Orientations.TryGetValue(c, out orientation))
            {
                Joystick2Orientation &= ~orientation;
            }

            if (c == KeyMappings.Joystick2Button)
            {
                Joystick2ButtonPressed = false;
            }
        }

        public void MouseDown() => LightPenClick = true;

        public void MouseUp() => LightPenClick = false;

        public void MouseMoved(int x, int y)
        {
            LightPenX = x;
            LightPenY = y;
        }

        private void Do(char c, bool keyPressed)
        {
            if (!KeyMappings.Keys.TryGetValue(c, out VirtualKey key))
                return;

            if (key.ShiftKey && keyPressed)
            {
                KeyPressed(Mo5Key.Shift);
            }
            else
            {
                KeyReleased(Mo5Key.Shift);
            }

            if (keyPressed)
            {
                KeyPressed(key.Key);
            }
            else
            {
                KeyReleased(key.Key);
            }
        }

        private void KeyPressed(Mo5Key key)
        {
            _key[(int)key] = true;
        }

        private void KeyReleased(Mo5Key key)
        {
            _key[(int)key] = false;
        }

        private void ClearKeys()
        {
            Array.Clear(_key, 0, _key.Length);
        }
    }
}
