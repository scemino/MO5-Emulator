using System;

namespace nMO5
{
    public class OpcodeExecutedEventArgs : EventArgs
    {
		public int Pc { get; }
		public int Opcode { get; }

        public OpcodeExecutedEventArgs(int pc, int opcode)
        {
            Pc = pc;
            Opcode = opcode;
        }
    }
}