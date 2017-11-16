using System;
using System.IO;

namespace nMO5
{
    [Flags]
    internal enum CartridgeType
    {
        Simple = 0,
        SwitchBank = 1,
        Os9 = 2,
    }

    public class Memory : IMemory
    {
        private bool[] _dirty;
        private readonly int[] _mapper;

        // 0 1          POINT   2
        // 2 3          COLOR   2
        // 4 5 6 7      RAM1    4
        // 8 9 10 11    RAM2    4
        // 12           LINEA   1
        // 13           LINEB   1
        // 14 15 16 17  ROM     4
        private readonly int[][] _mem;

        // I/O ports
        private readonly int[] _ports;

        // Cartridge
        private CartridgeType _cartype;
        private long _carsize;
        private int _carflags;
        private byte[] _car;

        private readonly byte[] _floppyRom;
        private IMachine _machine;
        private IInput _input;

        public int SoundMem { get; private set; }

        public event EventHandler<AddressWrittenEventArgs> Written;

        public int BorderColor
        {
            get
            {
                var value = Read(0xA7C0);
                return (value >> 1) & 0x0F;
            }
        }

        public IMachine Machine
        {
            get { return _machine; }
            set { _machine = value; }
        }

        private int JoystickAction
        {
            get
            {
                int joystickAction = 0;
                if (!_input.Joystick1ButtonPressed)
                {
                    joystickAction |= 0x40;
                }
                if (!_input.Joystick2ButtonPressed)
                {
                    joystickAction |= 0x80;
                }
                return joystickAction;
            }
        }

        private int JoystickPosition
        {
            get
            {
                int joystickPosition = 0;
                if (!_input.Joystick1Orientation.HasFlag(JoystickOrientation.North))
                {
                    joystickPosition |= (int)JoystickOrientation.North;
                }
                if (!_input.Joystick1Orientation.HasFlag(JoystickOrientation.East))
                {
                    joystickPosition |= (int)JoystickOrientation.East;
                }
                if (!_input.Joystick1Orientation.HasFlag(JoystickOrientation.West))
                {
                    joystickPosition |= (int)JoystickOrientation.West;
                }
                if (!_input.Joystick1Orientation.HasFlag(JoystickOrientation.South))
                {
                    joystickPosition |= (int)JoystickOrientation.South;
                }
                if (!_input.Joystick2Orientation.HasFlag(JoystickOrientation.North))
                {
                    joystickPosition |= ((int)JoystickOrientation.North << 8);
                }
                if (!_input.Joystick2Orientation.HasFlag(JoystickOrientation.East))
                {
                    joystickPosition |= ((int)JoystickOrientation.East << 8);
                }
                if (!_input.Joystick2Orientation.HasFlag(JoystickOrientation.West))
                {
                    joystickPosition |= ((int)JoystickOrientation.West << 8);
                }
                if (!_input.Joystick2Orientation.HasFlag(JoystickOrientation.South))
                {
                    joystickPosition |= ((int)JoystickOrientation.South << 8);
                }
                return joystickPosition;
            }
        }

        public Memory(IInput input)
        {
            _input = input;
            if (File.Exists("./bios/cd90-640.rom"))
            {
                _floppyRom = File.ReadAllBytes("./bios/cd90-640.rom");
            }
            _ports = new int[0x40];
            _mem = new int[18][];
            for (var j = 0; j < _mem.Length; j++)
            {
                _mem[j] = new int[0x1000];
            }
            _mapper = new[] {
                0,1,4,5,6,7,8,9,10,11,12,13,14,15,16,17
            };
            _dirty = new bool[200];

            Reset();
        }

        public void SaveState(Stream stream)
        {
            var bw = new BinaryWriter(stream);
            for (int i = 0; i < 18; i++)
            {
                for (int j = 0; j < 0x1000; j++)
                {
                    bw.Write(_mem[i][j]);
                }
            }
            bw.Write(_mapper[0] != 0);
            for (int i = 0; i < _ports.Length; i++)
            {
                bw.Write(_ports[i]);
            }
            bw.Write(SoundMem);
        }

        public void RestoreState(Stream stream)
        {
            var br = new BinaryReader(stream);
            for (int i = 0; i < 18; i++)
            {
                for (int j = 0; j < 0x1000; j++)
                {
                    _mem[i][j] = br.ReadInt32();
                }
            }
            var isMemSwap = br.ReadBoolean();
            if (isMemSwap)
            {
                _mapper[0] = 2;
                _mapper[1] = 3;
            }
            else
            {
                _mapper[0] = 0;
                _mapper[1] = 1;
            }
            for (int i = 0; i < _ports.Length; i++)
            {
                _ports[i] = br.ReadInt32();
            }
            SoundMem = br.ReadInt32();
            for (int i = 0; i < _dirty.Length; i++)
            {
                _dirty[i] = true;
            }
        }

        // read with io
        public int Read(int address)
        {
            var page = (address & 0xF000) >> 12;
            switch (page)
            {
                case 0x0A:
                    if (address >= 0xA000 && address < 0xA7C0)
                    {
                        return _floppyRom[address & 0x7FF];
                    }
                    switch (address)
                    {
                        case 0xA7C0:
                            const int tapeDrivePresent = 0x80;
                            return _ports[0] | tapeDrivePresent | (_input.LightPenClick ? 0x20 : 0);
                        case 0xA7C1:
                            return _ports[1] | (_input.IsKeyPressed((Mo5Key)(_ports[1] & 0xFE)) ? 0 : 0x80);
                        case 0xA7C2:
                            return _ports[2];
                        case 0xA7C3:
                            return _ports[3] | ~_machine.Initn();
                        case 0xA7CB:
                            return (_carflags & 0x3F) | ((_carflags & 0x80) >> 1) | ((_carflags & 0x40) << 1);
                        case 0xA7CC:
                            return ((_ports[0x0E] & 4) != 0) ? JoystickPosition : _ports[0x0C];
                        case 0xA7CD:
                            return ((_ports[0x0F] & 4) != 0) ? JoystickAction | SoundMem : _ports[0x0D];
                        case 0xA7CE:
                            return 4;
                        case 0xA7D8:
                            return ~_machine.Initn();
                        case 0xA7E1: return 0xFF; //0 means printer error #53
                        case 0xA7E6: return _machine.Iniln() << 1;
                        case 0xA7E7: return _machine.Initn();
                        default:
                            if (address < 0xA800)
                            {
                                return (_ports[address & 0x3F]);
                            }
                            return 0;
                    }
                case 0x0B:
                    SwitchMemo5Bank(address);
                    return _mem[_mapper[page]][address & 0xFFF] & 0xFF;
                default:
                    return _mem[_mapper[page]][address & 0xFFF] & 0xFF;
            }
        }

        public int Read16(int address)
        {
            return Read(address) << 8 | Read(address + 1);
        }

        // write with io
        public void Write(int address, int value)
        {
            WriteCore(address, value);
            Written?.Invoke(this, new AddressWrittenEventArgs(address, 1, value & 0xFF));
        }

        public void Write16(int address, int value)
        {
            WriteCore(address, value >> 8);
            WriteCore(address + 1, value & 0xFF);
            Written?.Invoke(this, new AddressWrittenEventArgs(address, 2, value & 0xFFFF));
        }

        public void Set(int address, int value)
        {
            var page = (address & 0xF000) >> 12;
            _mem[_mapper[page]][address & 0xFFF] = value & 0xFF;
            Written?.Invoke(this, new AddressWrittenEventArgs(address, 1, value & 0xFF));
        }

        public int Point(int address)
        {
            var page = (address & 0xF000) >> 12;
            return _mem[page][address & 0xFFF];
        }

        public int Color(int address)
        {
            var page = (address & 0xF000) >> 12;
            return _mem[page + 2][address & 0xFFF];
        }

        public bool IsDirty(int line)
        {
            var ret = _dirty[line];
            _dirty[line] = false;
            return ret;
        }

        public void OpenMemo(Stream memo)
        {
            _carsize = Math.Min(memo.Length, 0x10000);
            _car = new byte[_carsize];
            memo.Read(_car, 0, (int)_carsize);

            _cartype = _carsize > 0x4000 ? CartridgeType.SwitchBank : CartridgeType.Simple; //cartouche > 16 Ko
            _carflags = 4; //cartridge enabled, write disabled, bank 0; 

            Reset();
        }

        public void CloseMemo()
        {
            _carflags = 0;
            LoadRom();
        }

        private void WriteCore(int address, int value)
        {
            var page = (address & 0xF000) >> 12;
            if (address < 0x1F40)
            {
                _dirty[address / 40] = true;
            }
            switch (page)
            {
                case 0x0A:
                    Hardware(address, value);
                    break;
                case 0x0B:
                case 0x0C:
                case 0x0D:
                case 0x0E:
                    if (((_carflags & 8) != 0) && (_cartype == 0))
                    {
                        _mem[_mapper[page]][address & 0xFFF] = value & 0xFF;
                    }
                    break;
                case 0x0F:
                    break;
                default:
                    _mem[_mapper[page]][address & 0xFFF] = value & 0xFF;
                    break;
            }
        }

        private void SwitchMemo5Bank(int address)
        {
            if (_cartype != CartridgeType.SwitchBank) return;
            if ((address & 0xFFFC) != 0xBFFC) return;
            _carflags = (_carflags & 0xFC) | (address & 3);
            LoadRom();
        }

        private void Reset()
        {
            _carflags &= 0xEC;
            LoadRom();
        }

        private void LoadRom()
        {
            if ((_carflags & 4) != 0)
            {
                LoadMemo();
                return;
            }

            LoadMo5Rom();
        }

        private void LoadMemo()
        {
            var offset = ((_carflags & 0x03) << 14);
            var maxSize = Math.Min(_car.Length - offset, 0x4000); // max 16 Kb
            for (var i = 0; i < maxSize; i++)
            {
                Set(0xB000 + i, _car[i + offset]);
            }
        }

        private void LoadMo5Rom()
        {
            const int startingAddress = 0xC000;
            try
            {
                using (var fis = File.OpenRead("./bios/mo5.rom"))
                {
                    for (var i = startingAddress; i < 0x10000; i++)
                    {
                        Set(i, fis.ReadByte());
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error : mo5.rom file is missing {0}", e);
            }
        }

        private void Hardware(int adr, int op)
        {
            // 6821 système
            // acces à ORA ou DDRA
            switch (adr)
            {
                case 0xA7C0:
                    _ports[0] = op & 0x5F;
                    if ((op & 0x01) != 0)
                    {
                        _mapper[0] = 0;
                        _mapper[1] = 1;
                    }
                    else
                    {
                        _mapper[0] = 2;
                        _mapper[1] = 3;
                    }
                    break;
                case 0xA7C1:
                    _ports[1] = op & 0x7F;
                    SoundMem = (op & 1) << 5;
                    break;
                case 0xA7C2:
                    _ports[2] = op & 0x3F;
                    break;
                case 0xA7C3:
                    _ports[3] = op & 0x3F;
                    break;
                case 0xA7CB:
                    _carflags = op;
                    LoadRom();
                    break;
                case 0xA7CC:
                    _ports[0x0C] = op;
                    break;
                case 0xA7CD:
                    _ports[0x0D] = op;
                    SoundMem = op & 0x3F;
                    break;
                case 0xA7CE:
                    _ports[0x0E] = op;
                    break;
                case 0xA7CF:
                    _ports[0x0F] = op;
                    break;
            }
        }
    }
}