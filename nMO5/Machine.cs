using System;
using System.IO;

namespace nMO5
{
    public enum JoystickOrientation
    {
        North = 1,
        South = 2,
        West = 4,
        East = 8,
    }

    public class Machine
    {
        private readonly Memory _mem;
        private readonly M6809 _micro;
        private readonly Keyboard _keyboard;
        private bool _irq;

        public Memory Memory => _mem;
        public Keyboard Keyboard => _keyboard;
        public M6809 Cpu => _micro;
        public int FrameCount { get; private set; }
        public bool IsScriptRunning { get; set; }

        public event EventHandler Stepping;

        public Machine(ISound sound)
        {
            _mem = new Memory();
            _micro = new M6809(_mem, sound);
            _keyboard = new Keyboard(_mem);
        }

        public void SaveState(Stream stream)
        {
            _micro.SaveState(stream);
            _mem.SaveState(stream);
        }

        public void RestoreState(Stream stream)
        {
            _micro.RestoreState(stream);
            _mem.RestoreState(stream);
        }

        public void Step()
        {
            FrameCount++;
            FullSpeed();
            Stepping?.Invoke(this, EventArgs.Empty);
        }

        public void OpenK7(string k7)
        {
            _mem.OpenK7(k7);
        }

        public void OpenMemo(string path)
        {
            _mem.OpenMemo(path);
            _micro.Reset();
        }

        public void OpenDisk(string path)
        {
            _mem.OpenDisk(path);
        }

        // soft reset method ("reinit prog" button on original MO5) 
        public void ResetSoft()
        {
            _micro.Reset();
        }

        // hard reset (switch off and on)
        public void ResetHard()
        {
            for (var i = 0x2000; i < 0x3000; i++)
            {
                _mem.Set(i, 0);
            }
            _mem.CloseMemo();
            _micro.Reset();
        }

        public void Joystick1(JoystickOrientation orientation, bool isPressed)
        {
            if (isPressed)
            {
                _mem.JoystickPosition &= ~(int)orientation;
            }
            else
            {
                _mem.JoystickPosition |= (int)orientation;
            }
        }

        public void Joystick2(JoystickOrientation orientation, bool isPressed)
        {
            if (isPressed)
            {
                _mem.JoystickPosition &= ~((int)orientation << 8);
            }
            else
            {
                _mem.JoystickPosition |= ((int)orientation << 8);
            }
        }

        public void Joystick1Button(bool isPressed)
        {
            if (isPressed)
            {
                _mem.JoystickAction &= ~0x40;
            }
            else
            {
                _mem.JoystickAction |= 0x40;
            }
        }

        public void Joystick2Button(bool isPressed)
        {
            if (isPressed)
            {
                _mem.JoystickAction &= ~0x80;
            }
            else
            {
                _mem.JoystickAction |= 0x80;
            }
        }

        // the emulator main loop
        private void FullSpeed()
        {
            _mem.Set(0xA7E7, 0x00);
            // 3.9 ms haut écran (+0.3 irq)
            if (_irq)
            {
                _irq = false;
                _micro.FetchUntil(3800);
            }
            else
            {
                _micro.FetchUntil(4100);
            }

            // 13ms fenetre
            _mem.Set(0xA7E7, 0x80);
            _micro.FetchUntil(13100);

            _mem.Set(0xA7E7, 0x00);
            _micro.FetchUntil(2800);

            if ((_mem.Crb & 0x01) == 0x01)
            {
                _irq = true;
                _mem.Crb |= 0x80;
                _mem.Set(0xA7C3, _mem.Crb);
                var cc = _micro.ReadCc();
                if ((cc & 0x10) == 0)
                    _micro.Irq();
                // 300 cycles sous interrupt
                _micro.FetchUntil(300);
                _mem.Crb &= 0x7F;
                _mem.Set(0xA7C3, _mem.Crb);
            }
        }
    }
}