using System;
using System.IO;

namespace nMO5
{
    [Flags]
    public enum JoystickOrientation
    {
        North = 1,
        South = 2,
        West = 4,
        East = 8,
    }

    public interface IMachine
    {
        int Iniln();
        int Initn();
    }

    public class Machine: IMachine
    {
        private readonly IMemory _mem;
        private readonly IM6809 _cpu;
        private FileStream _fPrinter;
        private int _k7Bit;
        private int _k7Byte;
        private long _indexMax;
        private long _index;
        private Stream _k7FileStream;
        private Stream _fd;
        private int _videoLineCycle;
        private int _videoLineNumber;

        private bool IsFileOpened => _k7FileStream != null;
        public string K7Path => (_k7FileStream as FileStream)?.Name;
        public long Index => _index;
        public long IndexMax => _indexMax;
        public string DiskPath { get; private set; }
        public string MemoPath { get; private set; }

        public IMemory Memory => _mem;
        public IM6809 Cpu => _cpu;
        public Screen Screen { get; }
        public ISound Sound { get; }
        public IInput Input { get; }

        public int FrameCount { get; private set; }
        public bool IsScriptRunning { get; set; }

        public event EventHandler IndexChanged;
        public event EventHandler Stepping;

        public Machine(ISound sound, IM6809 cpu, IInput input, IMemory memory)
        {
            _mem = memory;
            Sound = sound;
            Input = input;
            cpu.Memory = _mem;
            _cpu = cpu;
            cpu.Reset();
            Screen = new Screen(_mem);
        }

        public void SaveState(Stream stream)
        {
            _cpu.SaveState(stream);
            _mem.SaveState(stream);
        }

        public void RestoreState(Stream stream)
        {
            _cpu.RestoreState(stream);
            _mem.RestoreState(stream);
        }

        public void Step()
        {
            FrameCount++;
            FetchUntil(20000);
            Stepping?.Invoke(this, EventArgs.Empty);
        }

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
                _mem.OpenMemo(memo);
            }
            _cpu.Reset();
        }

        public void OpenDisk(string path)
        {
            EjectAll();
            _fd = File.OpenRead(path);
            DiskPath = path;
            IndexChanged?.Invoke(this, EventArgs.Empty);
        }

        // soft reset method ("reinit prog" button on original MO5) 
        public void SotReset()
        {
            _cpu.Reset();
        }

        // hard reset (switch off and on)
        public void HardReset()
        {
            _videoLineCycle = 0;
            _videoLineNumber = 0;
            //for (var i = 0x2000; i < 0x3000; i++)
            //{
            //    _mem.Set(i, 0);
            //}
            _mem.CloseMemo();
            _cpu.Reset();
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

        private void ReadK7Byte()
        {
            if (!IsFileOpened) return;

            _k7Byte = _k7FileStream.ReadByte();
            if ((_k7FileStream.Position & 511) == 0)
            {
                _index = _k7FileStream.Position >> 9;
                IndexChanged?.Invoke(this, EventArgs.Empty);
            }

            Cpu.RegA = _k7Byte;
            _mem.Write(0x2045, 0);
            _k7Bit = 0;
        }

        private void WriteK7Byte()
        {
            if (!IsFileOpened) return;
            _k7FileStream.WriteByte((byte)Cpu.RegA);
            _mem.Write(0x2045, 0);
        }

        private void ReadSector()
        {
            if (_fd == null)
            {
                //TODO: Diskerror(71); 
                return;
            }
            var u = _mem.Read(0x2049);
            if (u > 03)
            {
                //Diskerror(53); 
                return;
            }
            var p = _mem.Read(0x204A);
            if (p != 0)
            {
                //Diskerror(53); 
                return;
            }
            p = _mem.Read(0x204B);
            if (p > 79)
            {
                //Diskerror(53); 
                return;
            }
            var s = _mem.Read(0x204C);
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
            var i = (_mem.Read(0x204F) << 8) + _mem.Read(0x2050);
            if (_fd.Read(buffer, 0, 256) == 0)
            {
                //Diskerror(53); 
                return;
            }
            for (var j = 0; j < 256; j++)
            {
                _mem.Write(i++, buffer[j]);
            }
        }

        private void ReadBit(IM6809 machine)
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

            var octet = _mem.Read(0x2045) << 1;
            if ((_k7Byte & _k7Bit) == 0)
            {
                machine.RegA = 0;
            }
            else
            {
                octet |= 0x01;
                machine.RegA = 0xFF;

            }
            // positionne l'octet dans la page 0 du moniteur
            _mem.Write(0x2045, octet & 0xFF);

            _k7Bit = _k7Bit >> 1;
        }

        private void FetchUntil(int clock)
        {
            var c = 0;
            while (c < clock)
            {
                var result = _cpu.Fetch();
                if (result < 0)
                {
                    FetchUnknownOpCode(-result);
                    result = 64;
                }
                c += result;
                _videoLineCycle += result;
                // wait for end of line
                if (_videoLineCycle < 64) continue;
                _videoLineCycle -= 64;
                _videoLineNumber++;
                // wait end of frame
                if (_videoLineNumber < 312) continue;
                _videoLineNumber -= 312;
                _cpu.Irq();
            }
        }

        public int Iniln()
        {
            if (_videoLineCycle < 23) return 0; 
            return 0x20;
        }

        public int Initn()
        {
            if (_videoLineNumber < 56) return 0;
            if (_videoLineNumber > 255) return 0;
            if (_videoLineNumber == 56 && _videoLineCycle < 24) return 0;
            if (_videoLineNumber == 255 && _videoLineCycle > 62) return 0;
            return 0x80;
        }

        private void FetchUnknownOpCode(int opcode)
        {
            // thanks to D.Coulom for the next instructions
            // used by his emulator dcmoto
            switch (opcode)
            {
                case 0x11EC:
                    // lecture bit cassette
                    ReadBit(Cpu);
                    break;
                case 0x11F1: // lecture octet cassette (pour 6809)
                case 0x11ED: // lecture octet cassette (pour compatibilite 6309)
                    ReadK7Byte();
                    break;
                case 0x11F2: // ecriture octet cassette(pour 6809)
                case 0x11EE: // ecriture octet cassette(pour compatibilite 6309)
                    WriteK7Byte();
                    break;
                case 0x11F3: // initialisation controleur disquette
                    break;
                // 0xF4: formatage disquette
                case 0x11F5:
                    ReadSector();
                    break;
                // TODO:
                // 0xF8: lecture position souris
                // 0xF9: lecture des boutons de la souris
                case 0x11FA: // envoi d'un octet a l'imprimante
                    Print();
                    break;
                // 0xFC: lecture du clavier TO8
                // 0xFD: ecriture vers clavier TO8
                // 0xFE: emission commande nanoreseau
                case 0x11FF: // lecture coordonnees crayon optique
                    ReadPenXy();
                    break;
            }
        }

        private void ReadPenXy()
        {
            if ((Input.LightPenX < 0) || (Input.LightPenX >= 320)) { Cpu.RegCc |= 1; return; }
            if ((Input.LightPenY < 0) || (Input.LightPenY >= 200)) { Cpu.RegCc |= 1; return; }
            _mem.Write16(Cpu.RegS + 6, (short)Input.LightPenX);
            _mem.Write16(Cpu.RegS + 8, (short)Input.LightPenY);
            Cpu.RegCc &= 0xFE;
        }

        private void Print()
        {
            if (_fPrinter == null)
            {
                _fPrinter = File.OpenWrite("mo5-printer.txt");
            }
            _fPrinter.WriteByte((byte)Cpu.RegB);
            Cpu.RegCc &= 0xFE;
        }
    }
}