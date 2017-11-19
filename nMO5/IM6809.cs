using System;
using System.IO;

namespace nMO5
{
    public interface IM6809
    {
        event EventHandler<OpcodeExecutedEventArgs> OpcodeExecuted;

        int RegA { get; set; }
        int RegB { get; set; }
        int RegCc { get; set; }
        int RegS { get; set; }
        int RegPc { get; set; }
        int RegD { get; set; }
        int RegDp { get; set; }
        int RegU { get; set; }
        int RegX { get; set; }
        int RegY { get; set; }

        int CyclesCount { get; }
        int InstructionsCount { get; }

        int Fetch();
        void Irq();
        void Reset();
        void ResetClock();
        void ResetInstructionsCount();

        void SaveState(Stream stream);
        void RestoreState(Stream stream);
    }
}
