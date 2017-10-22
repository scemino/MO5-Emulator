using System;
using System.IO;

namespace nMO5
{
    [Flags]
    enum CartridgeType
    {
        Simple = 0,
        SwitchBank = 1,
        Os9 = 2,
    }

    public class Memory : IMemory
    {
        private bool[] _dirty;

        private int _k7Bit;
        private int _k7Byte;

        private Stream _k7FileStream;
        private Stream _fd;
        private bool IsFileOpened => _k7FileStream != null;
        private bool[] _key;
        private int[] _mapper;

        // 0 1          POINT   2
        // 2 3          COLOR   2
        // 4 5 6 7      RAM1    4
        // 8 9 10 11    RAM2    4
        // 12           LINEA   1
        // 13           LINEB   1
        // 14 15 16 17  ROM     4
        private int[][] _mem;

        // I/O ports
        public int Crb;
        private int[] _ports;

        // Cartridge
        private CartridgeType _cartype;
        private long _carsize;
        private int _carflags;
        private byte[] _car;

        private readonly byte[] _floppyRom;
        private long _indexMax;
        private long _index;

        public int SoundMem { get; private set; }

        // Lightpen parameters  
        public bool LightPenClick { get; set; }
        public int LightPenX { get; set; }
        public int LightPenY { get; set; }

        // Joystick parameters
        public int JoystickPosition { get; set; }
        public int JoystickAction { get; set; }

        public bool[] Key => _key;

        public string DiskPath { get; private set; }
        public string MemoPath { get; private set; }
        public string K7Path => (_k7FileStream as FileStream)?.Name;
        public long Index => _index;
        public long IndexMax => _indexMax;

        public event EventHandler<AddressWrittenEventArgs> Written;
        public event EventHandler IndexChanged;

        public int BorderColor
        {
            get
            {
                var value = Read(0xA7C0);
                return (value >> 1) & 0x0F;
            }
        }


        public Memory()
        {
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
            _mapper = new int[] {
                0,1,4,5,6,7,8,9,10,11,12,13,14,15,16,17
            };
            _key = new bool[256];
            _dirty = new bool[200];
            JoystickPosition = 0xFF; // center of joystick 
            JoystickAction = 0xC0;   // button released

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
            bw.Write(Crb);
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
            Crb = br.ReadInt32();
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
                            return _ports[0] | tapeDrivePresent | (LightPenClick ? 0x20 : 0);
                        case 0xA7C1:
                            return _ports[1] | (_key[_ports[1] & 0xFE] ? 0 : 0x80);
                        case 0xA7C2:
                            return _ports[2];
                        case 0xA7C3:
                            return _mem[_mapper[page]][address & 0xFFF] & 0xFF;
                        case 0xA7CB:
                            return (_carflags & 0x3F) | ((_carflags & 0x80) >> 1) | ((_carflags & 0x40) << 1);
                        case 0xA7CC:
                            return ((_ports[0x0E] & 4) != 0) ? JoystickPosition : _ports[0x0C];
                        case 0xA7CD:
                            return ((_ports[0x0F] & 4) != 0) ? JoystickAction : _ports[0x0D];
                        case 0xA7CE:
                            return 4;
                        case 0xA7D8:
                            return _mem[_mapper[page]][address & 0xFFF] & 0xFF;
                        case 0xA7E1: return 0xFF; //0 means printer error #53
                        case 0xA7E6: return _mem[_mapper[page]][address & 0xFFF] & 0xFF;
                        case 0xA7E7: return _mem[_mapper[page]][address & 0xFFF] & 0xFF;
                        default:
                            if (address >= 0xA7CF && address < 0xA800)
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

        public void SetKey(int i) => _key[i] = true;

        public void RemKey(int i) => _key[i] = false;

        public void OpenK7(string k7)
        {
            try
            {
                EjectAll();
                _k7FileStream = File.Open(k7, FileMode.OpenOrCreate, FileAccess.ReadWrite);

                _indexMax = _k7FileStream.Length >> 9;
                _index = 0;
                IndexChanged?.Invoke(this, EventArgs.Empty);

                _k7FileStream.Seek(0, SeekOrigin.Begin);

            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error : file is missing " + e);
                return;
            }

            _k7Bit = 0;
            _k7Byte = 0;
        }

        public void OpenMemo(string path)
        {
            using (var memo = File.OpenRead(path))
            {
                EjectAll();
                MemoPath = path;
                IndexChanged?.Invoke(this, EventArgs.Empty);
                _carsize = Math.Min(memo.Length, 0x10000);
                _car = new byte[_carsize];
                memo.Read(_car, 0, (int)_carsize);
            }
            _cartype = _carsize > 0x4000 ? CartridgeType.SwitchBank : CartridgeType.Simple; //cartouche > 16 Ko
            _carflags = 4; //cartridge enabled, write disabled, bank 0; 

            Reset();
        }

        public void OpenDisk(string path)
        {
            EjectAll();
            _fd = File.OpenRead(path);
            DiskPath = path;
            IndexChanged?.Invoke(this, EventArgs.Empty);
        }

        private void EjectAll()
        {
            _k7FileStream?.Dispose();
            _k7FileStream = null;
            MemoPath = null;
            _fd?.Dispose();
            _fd = null;
            DiskPath = null;
        }

        public void CloseMemo()
        {
            _carflags = 0;
            LoadRom();
        }

        public void Rewind()
        {
            _k7FileStream?.Seek(0, SeekOrigin.Begin);
        }

        private void WriteCore(int address, int value)
        {
            var page = (address & 0xF000) >> 12;

            if (_mapper[page] >= 14 && _mapper[page] <= 17)
                return; // Protection en écriture de la ROM

            if (address < 0x1F40)
            {
                _dirty[address / 40] = true;
            }
            if (page == 0x0A)
            {
                Hardware(address, value);
            }
            else if (page == 0x0B || page == 0x0C || page == 0x0D
                     || page == 0x0E)
            {
                if (((_carflags & 8) != 0) && (_cartype == 0))
                {
                    _mem[_mapper[page]][address & 0xFFF] = value & 0xFF;
                    return;
                }
            }
            else
            {
                _mem[_mapper[page]][address & 0xFFF] = value & 0xFF;
            }
        }

        private void SwitchMemo5Bank(int address)
        {
            if (_cartype != CartridgeType.SwitchBank) return;
            if ((address & 0xFFFC) != 0xBFFC) return;
            _carflags = (_carflags & 0xFC) | (address & 3);
            LoadRom();
        }

        // write with io without Protection
        private void WriteP(int address, int value)
        {
            var page = (address & 0xF000) >> 12;
            if (address < 0x1F40)
            {
                _dirty[address / 40] = true;
            }
            if (page == 0x0A)
            {
                Hardware(address, value);
                return;
            }
            if (page == 0x0B || page == 0x0C || page == 0x0D || page == 0x0E)
            {
                if (((_carflags & 8) != 0) && (_cartype == 0))
                {
                    _mem[_mapper[page]][address & 0xFFF] = value & 0xFF;
                    return;
                }
            }
            _mem[_mapper[page]][address & 0xFFF] = value & 0xFF;
            Written?.Invoke(this, new AddressWrittenEventArgs(address, 1, value & 0xFF));
        }

        private void Reset()
        {
            _carflags &= 0xEC;
            LoadRom();
            Crb = 0x00;

            _mem[0xA + 2][0x7CC] = 0xFF;
            _mem[0xA + 2][0x7CD] = 0xFF;
            _mem[0xA + 2][0x7CE] = 0xFF;
            _mem[0xA + 2][0x7CF] = 0xFF;
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
                        WriteP(i, fis.ReadByte());
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
                    if ((op & 0x01) == 0x01)
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
                    Crb = (Crb & 0xD0) | (op & 0x3F);
                    _mem[0xA + 2][0x7C3] = Crb;
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

        public void ReadK7Byte(M6809 machine)
        {
            if (!IsFileOpened) return;

            _k7Byte = _k7FileStream.ReadByte();
            if ((_k7FileStream.Position & 511) == 0)
            {
                _index = _k7FileStream.Position >> 9;
                IndexChanged?.Invoke(this, EventArgs.Empty);
            }

            machine.A = _k7Byte;
            Set(0x2045, 0);
            _k7Bit = 0;
        }

        public void WriteK7Byte(M6809 machine)
        {
            if (!IsFileOpened) return;
            _k7FileStream.WriteByte((byte)machine.A);
            Set(0x2045, 0);
        }

        public void ReadSector()
        {
            if (_fd == null)
            {
                //TODO: Diskerror(71); 
                return;
            }
            var u = Read(0x2049);
            if (u > 03)
            {
                //Diskerror(53); 
                return;
            }
            var p = Read(0x204A);
            if (p != 0)
            {
                //Diskerror(53); 
                return;
            }
            p = Read(0x204B);
            if (p > 79)
            {
                //Diskerror(53); 
                return;
            }
            var s = Read(0x204C);
            if ((s == 0) || (s > 16))
            {
                //Diskerror(53); 
                return;
            }
            s += 16 * p + 1280 * u;
            //fseek(ffd, 0, SEEK_END);
            if ((s << 8) > _fd.Length)
            {
                //Diskerror(53); 
                return;
            }
            var buffer = new byte[256];
            for (var j = 0; j < 256; j++) buffer[j] = 0xe5;
            _fd.Position = (s - 1) << 8;
            var i = (Read(0x204F) << 8) + Read(0x2050);
            if (_fd.Read(buffer, 0, 256) == 0)
            {
                //Diskerror(53); 
                return;
            }
            for (var j = 0; j < 256; j++)
            {
                Write(i++, buffer[j]);
            }
        }

        public void ReadBit(M6809 machine)
        {
            if (!IsFileOpened) return;

            // need to read 1 byte ?
            if (_k7Bit == 0x00)
            {
                try
                {
                    _k7Byte = _k7FileStream.ReadByte();
                }
                catch (Exception)
                {
                }

                _k7Bit = 0x80;
            }

            var octet = Read(0x2045) << 1;
            if ((_k7Byte & _k7Bit) == 0)
            {
                machine.A = 0;
            }
            else
            {
                octet |= 0x01;
                machine.A = 0xFF;

            }
            // positionne l'octet dans la page 0 du moniteur
            Set(0x2045, octet & 0xFF);

            _k7Bit = _k7Bit >> 1;
        }
    }
}