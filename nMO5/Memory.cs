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

    public class Memory
    {
        private bool[] _dirty;

        private int _k7Bit;
        private int _k7Byte;

        private Stream _k7Fis;
        private Stream _k7Fos;
        private Stream _fd;
        private bool IsInFileOpened => _k7Fis != null;
        private bool IsOutFileOpened => _k7Fos != null;
        private string _k7OutName;
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

        // Registres du 6821
        public int Ora;
        public int Orb;
        public int Ddra;
        public int Ddrb;
        public int Cra;
        public int Crb;

        // Cartridge
        private CartridgeType _cartype;
        private long _carsize;
        private int _carflags;
        private byte[] _car;

        private readonly byte[] _floppyRom;

        public int SoundMem { get; private set; }

        // Lightpen parameters  
        public bool LightPenClick { get; set; }
        public int LightPenX { get; set; }
        public int LightPenY { get; set; }

        public int ShowLed { get; set; }
        public int Led { get; private set; }

        public string K7Path => (_k7Fis as FileStream)?.Name;

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
            _floppyRom = File.ReadAllBytes("./bios/cd90-640.rom");
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
            bw.Write(Ora);
            bw.Write(Orb);
            bw.Write(Ddra);
            bw.Write(Ddrb);
            bw.Write(Cra);
            bw.Write(Crb);
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
            Ora = br.ReadInt32();
            Orb = br.ReadInt32();
            Ddra = br.ReadInt32();
            Ddrb = br.ReadInt32();
            Cra = br.ReadInt32();
            Crb = br.ReadInt32();
            SoundMem = br.ReadInt32();
            for (int i = 0; i < _dirty.Length; i++)
            {
                _dirty[i] = true;
            }
        }

        public void OpenDisk(string path)
        {
            _fd?.Dispose();
            _fd = File.OpenRead(path);
        }

        // read with io
        public int Read(int address)
        {
            if (address == 0xA7CB)
            {
                return (_carflags & 0x3F) | ((_carflags & 0x80) >> 1) | ((_carflags & 0x40) << 1);
            }
            if (address >= 0xA000 && address < 0xA7C0)
            {
                return _floppyRom[address & 0x7FF];
            }
            var page = (address & 0xF000) >> 12;
            if (page == 0x0B)
            {
                SwitchMemo5Bank(address);
            }
            var value = _mem[_mapper[page]][address & 0xFFF] & 0xFF;
            if (address == 0xA7C0)
            {
                const int tapeDrivePresent = 0x80;
                value |= tapeDrivePresent | (LightPenClick ? 0x20 : 0);
            }
            return value;
        }

        public int Read16(int address)
        {
            return Read(address) << 8 | Read(address + 1);
        }

        // write with io
        public void Write(int address, int value)
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

        public void Set(int address, int value)
        {
            var page = (address & 0xF000) >> 12;
            _mem[_mapper[page]][address & 0xFFF] = value & 0xFF;
        }

        public void Set16(int address, int value)
        {
            Set(address, value >> 8);
            Set(address + 1, value & 0xFF);
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

        public void SetKey(int i)
        {
            _key[i] = true;
        }

        public void RemKey(int i)
        {
            _key[i] = false;
        }

        public void SetK7File(string k7)
        {
            Console.WriteLine("opening: {0}", k7);
            try
            {
                _k7Fis?.Dispose();
                _k7Fis = File.OpenRead(k7);

                var indexMax = _k7Fis.Length >> 9;
                Console.WriteLine("Max index: {0}", indexMax);

                //K7Reader.Read(_k7Fis);
                _k7Fis.Seek(0, SeekOrigin.Begin);

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
                _carsize = Math.Min(memo.Length, 0x10000);
                _car = new byte[_carsize];
                memo.Read(_car, 0, (int)_carsize);
            }
            _cartype = _carsize > 0x4000 ? CartridgeType.SwitchBank : CartridgeType.Simple; //cartouche > 16 Ko
            _carflags = 4; //cartridge enabled, write disabled, bank 0; 

            Reset();
        }

        public void CloseMemo()
        {
            _carflags = 0;
            LoadRom();
        }

        public void Rewind()
        {
            _k7Fis?.Seek(0, SeekOrigin.Begin);
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
        }

        private void Reset(bool hard = false)
        {
            _carflags &= 0xEC;
            LoadRom();
            Cra = 0x00;
            Crb = 0x00;
            Ddra = 0x5F;
            Ddrb = 0x7F;

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
                    if ((Cra & 0x04) == 0x04)
                    // Accès à ORA
                    {
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
                        // Mise à jour de ORA selon le masque DDRA
                        op |= 0x80;
                        Ora = (Ora & (Ddra ^ 0xFF)) | (op & Ddra);
                        _mem[0xA + 2][0x7C0] = Ora;
                    }
                    else
                    {
                        Ddra = op;
                        _mem[0xA + 2][0x7C0] = op;
                    }
                    break;
                case 0xA7C1:
                    if ((Crb & 0x04) == 0x04)
                    // Accès à ORB
                    {
                        Orb = (Orb & (Ddrb ^ 0xFF)) | (op & Ddrb);

                        // GESTION HARD DU CLAVIER
                        if (_key[Orb & 0x7E])
                            Orb = Orb & 0x7F;
                        else
                            Orb = Orb | 0x80;

                        _mem[0xA + 2][0x7C1] = Orb;
                        SoundMem = (Orb & 1) << 5;
                    }
                    else
                    {
                        Ddrb = op;
                        _mem[0xA + 2][0x7C1] = op;
                    }
                    break;
                case 0xA7C2:
                    Cra = (Cra & 0xD0) | (op & 0x3F);
                    _mem[0xA + 2][0x7C2] = Cra;
                    break;
                case 0xA7C3:
                    Crb = (Crb & 0xD0) | (op & 0x3F);
                    _mem[0xA + 2][0x7C3] = Crb;
                    break;
                case 0xA7CB:
                    _carflags = op;
                    LoadRom();
                    break;
            }
        }

        private void CreateK7File()
        {
            if (_k7OutName != null)
                return;

            var today = DateTime.Now;

            _k7OutName = today.ToString("yyyy-MM-dd-HH_mm_ss") + ".k7";

            Console.WriteLine("Creating:" + _k7OutName);
            try
            {
                _k7Fos?.Dispose();
                _k7Fos = File.OpenWrite(_k7OutName);
                Console.Error.WriteLine("Information : new file {0}", _k7OutName);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error : file not created {0}", e);
                return;
            }

            _k7Bit = 0;
            _k7Byte = 0;
        }

        internal void ReadByte(M6809 machine)
        {
            if (!IsInFileOpened) return;

            int data = 0;
            _k7Byte = _k7Fis.ReadByte();

            machine.A = _k7Byte;
            Set(0x2045, data);
            _k7Bit = 0;
        }

        internal void ReadSector()
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

        internal void ReadBit(M6809 machine)
        {
            if (!IsInFileOpened) return;

            // need to read 1 byte ?
            if (_k7Bit == 0x00)
            {
                try
                {
                    _k7Byte = _k7Fis.ReadByte();
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

            Led = octet & 0xFF;
            ShowLed = 10;
            _k7Bit = _k7Bit >> 1;
        }
    }
}