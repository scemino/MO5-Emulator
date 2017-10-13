namespace nMO5
{
    public interface IMemory
    {
        int SoundMem { get; }
        int LightPenX { get; set; }
        int LightPenY { get; set; }

        int Read(int address);
        int Read16(int address);
        void ReadByte(M6809 machine);
        void ReadBit(M6809 machine);

        void Write(int address, int value);
        void Write16(int address, int value);
        void Set16(int address, int value);
        void ReadSector();
    }
}