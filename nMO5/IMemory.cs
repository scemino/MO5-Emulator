using System;
using System.IO;

namespace nMO5
{
    public interface IMemory
    {
        event EventHandler<AddressWrittenEventArgs> Written;
        
        int SoundMem { get; }

        int Read(int address);
        int Read16(int address);

        void Write(int address, int value);
        void Write16(int address, int value);

        bool IsDirty(int line);
        int BorderColor { get; }
        int Point(int address);
        int Color(int address);

        void OpenMemo(Stream memo);
        void CloseMemo();

        void SaveState(Stream stream);
        void RestoreState(Stream stream);
    }
}