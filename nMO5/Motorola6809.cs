using System;
using System.IO;
using static nMO5.Util;

namespace nMO5
{
    public class M6809
    {
        private const int SoundSize = 1024;

        private readonly IMemory _mem;

        // Sound emulation parameters
        public byte[] SoundBuffer { get; }

        private int _soundAddr;
        private readonly ISound _play;

		private int _clock;
        private int _instructionsCount;

        /// <summary>
        /// Program counter.
        /// </summary>
        public int Pc;

        // 8bits registers
        /// <summary>
        /// Accumulator Register.
        /// </summary>
        public int A;
        /// <summary>
        /// Accumulator Register.
        /// </summary>
        public int B;
        /// <summary>
        /// Direct page register.
        /// </summary>
        public int Dp;
        /// <summary>
        /// Condition code register.
        /// </summary>
        public int Cc;

        // 16bits registers
        /// <summary>
        /// Index register.
        /// </summary>
        public int X;
        /// <summary>
        /// Index register.
        /// </summary>
        public int Y;
        /// <summary>
        /// Stack pointer register.
        /// </summary>
        public int U;
        /// <summary>
        /// Stack pointer register.
        /// </summary>
        public int S;
        /// <summary>
        /// Accumulator register: D=A+B.
        /// </summary>
        public int D
        {
            get { return (A << 8) | (B & 0xFF); }
            set
            {
                A = value >> 8;
                B = value & 0xFF;
            }
        }

		public int CyclesCount => _clock;
        public int InstructionsCount { get => _instructionsCount; }

        // fast CC bits (as ints) 
        private int _res;
        private int _m1;
        private int _m2;
        private int _sign;
        private int _ovfl;
        private int _h1;
        private int _h2;
        private int _ccrest;

        public event EventHandler<OpcodeExecutedEventArgs> OpcodeExecuted;

        public M6809(IMemory mem, ISound play)
        {
            _mem = mem;
            _play = play;

            SoundBuffer = new byte[SoundSize];
            _soundAddr = 0;

            Reset();
        }

        public void SaveState(Stream stream)
        {
            Getcc();
            var writer = new BinaryWriter(stream);
            writer.Write(Pc);
            writer.Write(A);
            writer.Write(B);
            writer.Write(Dp);
            writer.Write(Cc);
            writer.Write(X);
            writer.Write(Y);
            writer.Write(U);
            writer.Write(S);
            writer.Write(_clock);
        }

        public void RestoreState(Stream stream)
        {
            var br = new BinaryReader(stream);
            Pc = br.ReadInt32();
            A = br.ReadInt32();
            B = br.ReadInt32();
            Dp = br.ReadInt32();
            Cc = br.ReadInt32();
            X = br.ReadInt32();
            Y = br.ReadInt32();
            U = br.ReadInt32();
            S = br.ReadInt32();
            _clock = br.ReadInt32();
            Setcc(Cc);
        }

        public void Reset()
        {
            Pc = _mem.Read16(0xFFFE);
            Dp = 0x00;
            S = 0x8000;
            Cc = 0x10;
            _clock = 0;
            _instructionsCount = 0;
        }

		public void ResetClock()
		{
			_clock = 0;
		}

		public void ResetInstructionsCount()
		{
			_instructionsCount = 0;
		}

        // basic 6809 addressing modes
        private int Immed8()
        {
            int m;
            m = Pc;
            Pc++;
            return m;
        }

        private int Immed16()
        {
            int m = Pc;
            Pc += 2;
            return m;
        }

        private int Direc()
        {
            int m = (Dp << 8) | _mem.Read(Pc);
            Pc++;
            return m;
        }

        private int Etend()
        {
            int m = _mem.Read16(Pc);
            Pc += 2;
            return m;
        }

        private int Indexe()
        {
            int m2;
            int M;
            int m = _mem.Read(Pc);
            Pc++;
            if (m < 0x80)
            {
                // effectue le complement a 2 sur la precision int
                int delta;
                if ((m & 0x10) == 0x10)
                    delta = ((-1 >> 5) << 5) | (m & 0x1F);
                else
                    delta = m & 0x1F;
                int reg;
                switch (m & 0xE0)
                {
                    case 0x00:
                        reg = X;
                        break;
                    case 0x20:
                        reg = Y;
                        break;
                    case 0x40:
                        reg = U;
                        break;
                    case 0x60:
                        reg = S;
                        break;
                    default: return 0;
                }
                _clock++;
                return (reg + delta) & 0xFFFF;
            }
            switch (m)
            {
                case 0x80: //i_d_P1_X
                    M = X;
                    X = (X + 1) & 0xFFFF;
                    _clock += 2;
                    return M;
                case 0x81: //i_d_P2_X
                    M = X;
                    X = (X + 2) & 0xFFFF;
                    _clock += 3;
                    return M;
                case 0x82: //i_d_M1_X
                    X = (X - 1) & 0xFFFF;
                    M = X;
                    _clock += 2;
                    return M;
                case 0x83: //i_d_M2_X
                    X = (X - 2) & 0xFFFF;
                    M = X;
                    _clock += 3;
                    return M;
                case 0x84: //i_d_X
                    M = X;
                    return M;
                case 0x85: //i_d_B_X
                    M = (X + SignedChar(B)) & 0xFFFF;
                    _clock += 1;
                    return M;
                case 0x86: //i_d_A_X;
                    M = (X + SignedChar(A)) & 0xFFFF;
                    _clock += 1;
                    return M;
                case 0x87: return 0; //i_undoc;	/* empty */
                case 0x88: //i_d_8_X;
                    m2 = _mem.Read(Pc);
                    Pc++;
                    M = (X + SignedChar(m2)) & 0xFFFF;
                    _clock += 1;
                    return M;
                case 0x89: //i_d_16_X;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc += 2;
                    M = (X + Signed16Bits(m2)) & 0xFFFF;
                    _clock += 4;
                    return M;
                case 0x8A: return 0; //i_undoc;	/* empty */
                case 0x8B: //i_d_D_X;
                    M = (X + Signed16Bits((A << 8) | B)) & 0xFFFF;
                    _clock += 4;
                    return M;
                case 0x8C: //i_d_PC8;
                case 0xAC: //i_d_PC8;
                case 0xCC: //i_d_PC8;
                case 0xEC: //i_d_PC8;
                    m = _mem.Read(Pc);
                    Pc = (Pc + 1) & 0xFFFF;
                    M = (Pc + SignedChar(m)) & 0xFFFF;
                    _clock++;
                    return M;
                case 0x8D: //i_d_PC16;
                case 0xAD: //i_d_PC16;
                case 0xCD: //i_d_PC16;
                case 0xED: //i_d_PC16;
                    M = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc = (Pc + 2) & 0xFFFF;
                    M = (Pc + Signed16Bits(M)) & 0xFFFF;
                    _clock += 5;
                    return M;
                case 0x8E: return 0; //i_undoc;	/* empty */
                case 0x8F: return 0; //i_undoc;	/* empty */
                case 0x90: return 0; //i_undoc;	/* empty */
                case 0x91: //i_i_P2_X;
                    M = (_mem.Read(X) << 8) | _mem.Read(X + 1);
                    X = (X + 2) & 0xFFFF;
                    _clock += 6;
                    return M;
                case 0x92: return 0; //i_undoc;	/* empty */
                case 0x93: //i_i_M2_X;
                    X = (X - 2) & 0xFFFF;
                    M = (_mem.Read(X) << 8) | _mem.Read(X + 1);
                    _clock += 6;
                    return M;
                case 0x94: //i_i_0_X;
                    M = (_mem.Read(X) << 8) | _mem.Read(X + 1);
                    _clock += 3;
                    return M;
                case 0x95: //i_i_B_X;
                    M = (X + SignedChar(B)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 4;
                    return M;
                case 0x96: //i_i_A_X;
                    M = (X + SignedChar(A)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 4;
                    return M;
                case 0x97: return 0; //i_undoc;	/* empty */
                case 0x98: //i_i_8_X;
                    m2 = _mem.Read(Pc);
                    Pc = (Pc + 1) & 0xFFFF;
                    M = (X + SignedChar(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 4;
                    return M;
                case 0x99: //i_i_16_X;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc = (Pc + 2) & 0xFFFF;
                    M = (X + Signed16Bits(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 7;
                    return M;
                case 0x9A: return 0; //i_undoc;	/* empty */
                case 0x9B: //i_i_D_X;
                    M = (X + Signed16Bits((A << 8) | B)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 7;
                    return M;
                case 0x9C: //i_i_PC8;
                case 0xBC: //i_i_PC8;
                case 0xDC: //i_i_PC8;
                case 0xFC: //i_i_PC8;
                    m2 = _mem.Read(Pc);
                    Pc = (Pc + 1) & 0xFFFF;
                    M = (Pc + SignedChar(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 4;
                    return M;
                case 0x9D: //i_i_PC16;
                case 0xBD: //i_i_PC16;
                case 0xDD: //i_i_PC16;
                case 0xFD: //i_i_PC16;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc = (Pc + 2) & 0xFFFF;
                    M = (Pc + Signed16Bits(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 8;
                    return M;
                case 0x9E: return 0; //i_undoc;	/* empty */
                case 0x9F: //i_i_e16;
                case 0xBF: //i_i_e16;
                case 0xDF: //i_i_e16;
                case 0xFF: //i_i_e16;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc = (Pc + 2) & 0xFFFF;
                    M = (_mem.Read(m2) << 8) | _mem.Read(m2 + 1);
                    _clock += 5;
                    return M;
                // Y
                case 0xA0: //i_d_P1_Y;
                    M = Y;
                    Y = (Y + 1) & 0xFFFF;
                    _clock += 2;
                    return M;
                case 0xA1: //i_d_P2_Y;
                    M = Y;
                    Y = (Y + 2) & 0xFFFF;
                    _clock += 3;
                    return M;
                case 0xA2: //i_d_M1_Y;
                    Y = (Y - 1) & 0xFFFF;
                    M = Y;
                    _clock += 2;
                    return M;
                case 0xA3: //i_d_M2_Y;
                    Y = (Y - 2) & 0xFFFF;
                    M = Y;
                    _clock += 3;
                    return M;
                case 0xA4: //i_d_Y;
                    M = Y;
                    return M;
                case 0xA5: //i_d_B_Y;
                    M = (Y + SignedChar(B)) & 0xFFFF;
                    _clock += 1;
                    return M;
                case 0xA6: //i_d_A_Y;
                    M = (Y + SignedChar(A)) & 0xFFFF;
                    _clock += 1;
                    return M;
                case 0xA7: return 0; //i_undoc;	/* empty */
                case 0xA8: //i_d_8_Y;
                    m2 = _mem.Read(Pc);
                    Pc++;
                    M = (Y + SignedChar(m2)) & 0xFFFF;
                    _clock += 1;
                    return M;
                case 0xA9: //i_d_16_Y;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc += 2;
                    M = (Y + Signed16Bits(m2)) & 0xFFFF;
                    _clock += 4;
                    return M;
                case 0xAA: return 0; //i_undoc;	/* empty */
                case 0xAB: //i_d_D_Y;
                    M = (Y + Signed16Bits((A << 8) | B)) & 0xFFFF;
                    _clock += 4;
                    return M;
                case 0xAE: return 0; //i_undoc;	/* empty */
                case 0xAF: return 0; //i_undoc;	/* empty */
                case 0xB0: return 0; //i_undoc;	/* empty */
                case 0xB1: //i_i_P2_Y;
                    M = (_mem.Read(Y) << 8) | _mem.Read(Y + 1);
                    Y = (Y + 2) & 0xFFFF;
                    _clock += 6;
                    return M;
                case 0xB2: return 0; //i_undoc;	/* empty */
                case 0xB3: //i_i_M2_Y;
                    Y = (Y - 2) & 0xFFFF;
                    M = (_mem.Read(Y) << 8) | _mem.Read(Y + 1);
                    _clock += 6;
                    return M;
                case 0xB4: //i_i_0_Y;
                    M = (_mem.Read(Y) << 8) | _mem.Read(Y + 1);
                    _clock += 3;
                    return M;
                case 0xB5: //i_i_B_Y;
                    M = (Y + SignedChar(B)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 4;
                    return M;
                case 0xB6: //i_i_A_Y;
                    M = (Y + SignedChar(A)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 4;
                    return M;
                case 0xB7: return 0; //i_undoc;	/* empty */
                case 0xB8: //i_i_8_Y;
                    m2 = _mem.Read(Pc);
                    Pc = (Pc + 1) & 0xFFFF;
                    M = (Y + SignedChar(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 4;
                    return M;
                case 0xB9: //i_i_16_Y;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc = (Pc + 2) & 0xFFFF;
                    M = (Y + Signed16Bits(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 7;
                    return M;
                case 0xBA: return 0; //i_undoc;	/* empty */
                case 0xBB: //i_i_D_Y;
                    M = (Y + Signed16Bits((A << 8) | B)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 7;
                    return M;
                case 0xBE: return 0; //i_undoc;	/* empty */

                // U
                case 0xC0: //i_d_P1_U;
                    M = U;
                    U = (U + 1) & 0xFFFF;
                    _clock += 2;
                    return M;
                case 0xC1: //i_d_P2_U;
                    M = U;
                    U = (U + 2) & 0xFFFF;
                    _clock += 3;
                    return M;
                case 0xC2: //i_d_M1_U;
                    U = (U - 1) & 0xFFFF;
                    M = U;
                    _clock += 2;
                    return M;
                case 0xC3: //i_d_M2_U;
                    U = (U - 2) & 0xFFFF;
                    M = U;
                    _clock += 3;
                    return M;
                case 0xC4: //i_d_U;
                    M = U;
                    return M;
                case 0xC5: //i_d_B_U;
                    M = (U + SignedChar(B)) & 0xFFFF;
                    _clock += 1;
                    return M;
                case 0xC6: //i_d_A_U;
                    M = (U + SignedChar(A)) & 0xFFFF;
                    _clock += 1;
                    return M;
                case 0xC7: return 0; //i_undoc;	/* empty */
                case 0xC8: //i_d_8_U;
                    m2 = _mem.Read(Pc);
                    Pc++;
                    M = (U + SignedChar(m2)) & 0xFFFF;
                    _clock += 1;
                    return M;
                case 0xC9: //i_d_16_U;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc += 2;
                    M = (U + Signed16Bits(m2)) & 0xFFFF;
                    _clock += 4;
                    return M;
                case 0xCA: return 0; //i_undoc;	/* empty */
                case 0xCB: //i_d_D_U;
                    M = (U + Signed16Bits((A << 8) | B)) & 0xFFFF;
                    _clock += 4;
                    return M;
                case 0xCE: return 0; //i_undoc;	/* empty */
                case 0xCF: return 0; //i_undoc;	/* empty */
                case 0xD0: return 0; //i_undoc;	/* empty */
                case 0xD1: //i_i_P2_U;
                    M = (_mem.Read(U) << 8) | _mem.Read(U + 1);
                    U = (U + 2) & 0xFFFF;
                    _clock += 6;
                    return M;
                case 0xD2: return 0; //i_undoc;	/* empty */
                case 0xD3: //i_i_M2_U;
                    U = (U - 2) & 0xFFFF;
                    M = (_mem.Read(U) << 8) | _mem.Read(U + 1);
                    _clock += 6;
                    return M;
                case 0xD4: //i_i_0_U;
                    M = (_mem.Read(U) << 8) | _mem.Read(U + 1);
                    _clock += 3;
                    return M;
                case 0xD5: //i_i_B_U;
                    M = (U + SignedChar(B)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 4;
                    return M;
                case 0xD6: //i_i_A_U;
                    M = (U + SignedChar(A)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 4;
                    return M;
                case 0xD7: return 0; //i_undoc;	/* empty */
                case 0xD8: //i_i_8_U;
                    m2 = _mem.Read(Pc);
                    Pc = (Pc + 1) & 0xFFFF;
                    M = (U + SignedChar(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 4;
                    return M;
                case 0xD9: //i_i_16_U;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc = (Pc + 2) & 0xFFFF;
                    M = (U + Signed16Bits(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 7;
                    return M;
                case 0xDA: return 0; //i_undoc;	/* empty */
                case 0xDB: //i_i_D_U;
                    M = (U + Signed16Bits((A << 8) | B)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 7;
                    return M;
                case 0xDE: return 0; //i_undoc;	/* empty */

                // S
                case 0xE0: //i_d_P1_S;
                    M = S;
                    S = (S + 1) & 0xFFFF;
                    _clock += 2;
                    return M;
                case 0xE1: //i_d_P2_S;
                    M = S;
                    S = (S + 2) & 0xFFFF;
                    _clock += 3;
                    return M;
                case 0xE2: //i_d_M1_S;
                    S = (S - 1) & 0xFFFF;
                    M = S;
                    _clock += 2;
                    return M;
                case 0xE3: //i_d_M2_S;
                    S = (S - 2) & 0xFFFF;
                    M = S;
                    _clock += 3;
                    return M;
                case 0xE4: //i_d_S;
                    M = S;
                    return M;
                case 0xE5: //i_d_B_S;
                    M = (S + SignedChar(B)) & 0xFFFF;
                    _clock += 1;
                    return M;
                case 0xE6: //i_d_A_S;
                    M = (S + SignedChar(A)) & 0xFFFF;
                    _clock += 1;
                    return M;
                case 0xE7: return 0; //i_undoc;	/* empty */
                case 0xE8: //i_d_8_S;
                    m2 = _mem.Read(Pc);
                    Pc++;
                    M = (S + SignedChar(m2)) & 0xFFFF;
                    _clock += 1;
                    return M;
                case 0xE9: //i_d_16_S;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc += 2;
                    M = (S + Signed16Bits(m2)) & 0xFFFF;
                    _clock += 4;
                    return M;
                case 0xEA: return 0; //i_undoc;	/* empty */
                case 0xEB: //i_d_D_S;
                    M = (S + Signed16Bits((A << 8) | B)) & 0xFFFF;
                    _clock += 4;
                    return M;
                case 0xEE: return 0; //i_undoc;	/* empty */
                case 0xEF: return 0; //i_undoc;	/* empty */
                case 0xF0: return 0; //i_undoc;	/* empty */
                case 0xF1: //i_i_P2_S;
                    M = (_mem.Read(S) << 8) | _mem.Read(S + 1);
                    S = (S + 2) & 0xFFFF;
                    _clock += 6;
                    return M;
                case 0xF2: return 0; //i_undoc;	/* empty */
                case 0xF3: //i_i_M2_S;
                    S = (S - 2) & 0xFFFF;
                    M = (_mem.Read(S) << 8) | _mem.Read(S + 1);
                    _clock += 6;
                    return M;
                case 0xF4: //i_i_0_S;
                    M = (_mem.Read(S) << 8) | _mem.Read(S + 1);
                    _clock += 3;
                    return M;
                case 0xF5: //i_i_B_S;
                    M = (S + SignedChar(B)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 4;
                    return M;
                case 0xF6: //i_i_A_S;
                    M = (S + SignedChar(A)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 4;
                    return M;
                case 0xF7: return 0; //i_undoc;	/* empty */
                case 0xF8: //i_i_8_S;
                    m2 = _mem.Read(Pc);
                    Pc = (Pc + 1) & 0xFFFF;
                    M = (S + SignedChar(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 4;
                    return M;
                case 0xF9: //i_i_16_S;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc = (Pc + 2) & 0xFFFF;
                    M = (S + Signed16Bits(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 7;
                    return M;
                case 0xFA: return 0; //i_undoc;	/* empty */
                case 0xFB: //i_i_D_S;
                    M = (S + Signed16Bits((A << 8) | B)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _clock += 7;
                    return M;
                case 0xFE: return 0; //i_undoc;	/* empty */
            }
            Console.Error.WriteLine("Indexed mode not implemented");
            return 0;
        }

        // cc register recalculate from separate bits
        private int Getcc()
        {
            if ((_res & 0xff) == 0)
                Cc = ((((_h1 & 15) + (_h2 & 15)) & 16) << 1)
                      | ((_sign & 0x80) >> 4)
                      | 4
                      | ((~(_m1 ^ _m2) & (_m1 ^ _ovfl) & 0x80) >> 6)
                      | ((_res & 0x100) >> 8)
                      | _ccrest;
            else
                Cc = ((((_h1 & 15) + (_h2 & 15)) & 16) << 1)
                      | ((_sign & 0x80) >> 4)
                      | ((~(_m1 ^ _m2) & (_m1 ^ _ovfl) & 0x80) >> 6)
                      | ((_res & 0x100) >> 8)
                      | _ccrest;

            return Cc;
        }

        // calculate CC fast bits from CC register
        public void Setcc(int i)
        {
            _m1 = _m2 = 0;
            _res = ((i & 1) << 8) | (4 - (i & 4));
            _ovfl = (i & 2) << 6;
            _sign = (i & 8) << 4;
            _h1 = _h2 = (i & 32) >> 2;
            _ccrest = i & 0xd0;
        }

        public int ReadCc()
        {
            Getcc();
            return Cc;
        }

        // LDx
        private int Ld8(int m, int c)
        {
            _sign = _mem.Read(m);
            _m1 = _ovfl;
            _res = (_res & 0x100) | _sign;
            _clock += c;
            return _sign;
        }

        private int Ld16(int m, int c)
        {
            int r = _mem.Read16(m);
            _m1 = _ovfl;
            _sign = r >> 8;
            _res = (_res & 0x100) | ((_sign | r) & 0xFF);
            _clock += c;
            return r;
        }

        // STx
        private void St8(int r, int adr, int c)
        {
            _mem.Write(adr, r);
            _m1 = _ovfl;
            _sign = r;
            _res = (_res & 0x100) | _sign;
            _clock += c;
        }

        private void St16(int r, int adr, int c)
        {
            _mem.Write16(adr, r);
            _m1 = _ovfl;
            _sign = r >> 8;
            _res = (_res & 0x100) | ((_sign | r) & 0xFF);
            _clock += c;
        }

        // LEA
        private int Lea()
        {
            int r = Indexe();
            _res = (_res & 0x100) | ((r | (r >> 8)) & 0xFF);
            _clock += 4;
            return r;
        }

        // CLR
        private void Clr(int m, int c)
        {
            _mem.Write(m, 0);
            _m1 = ~_m2;
            _sign = _res = 0;
            _clock += c;
        }

        // EXG
        private void Exg()
        {
            int r1;
            int r2;
            int m;
            int k;
            int l;
            m = _mem.Read(Pc++);
            r1 = (m & 0xF0) >> 4;
            r2 = m & 0x0F;
            k = 0; // only for javac
            l = 0; // only for javac
            switch (r1)
            {
                case 0x00:
                    k = (A << 8) | B;
                    break;
                case 0x01:
                    k = X;
                    break;
                case 0x02:
                    k = Y;
                    break;
                case 0x03:
                    k = U;
                    break;
                case 0x04:
                    k = S;
                    break;
                case 0x05:
                    k = Pc;
                    break;
                case 0x06:
                    k = Getcc();
                    break;
                case 0x07:
                    k = Getcc();
                    break;
                case 0x08:
                    k = A;
                    break;
                case 0x09:
                    k = B;
                    break;
                case 0x0A:
                    k = Getcc();
                    break;
                case 0x0B:
                    k = Dp;
                    break;
                case 0x0C:
                    k = Getcc();
                    break;
                case 0x0D:
                    k = Getcc();
                    break;
                case 0x0E:
                    k = Getcc();
                    break;
                case 0x0F:
                    k = Getcc();
                    break;
            } // of switch r1
            switch (r2)
            {
                case 0x00:
                    l = (A << 8) | B;
                    A = (k >> 8) & 255;
                    B = k & 255;
                    break;
                case 0x01:
                    l = X;
                    X = k;
                    break;
                case 0x02:
                    l = Y;
                    Y = k;
                    break;
                case 0x03:
                    l = U;
                    U = k;
                    break;
                case 0x04:
                    l = S;
                    S = k;
                    break;
                case 0x05:
                    l = Pc;
                    Pc = k;
                    break;
                case 0x06:
                    l = Getcc();
                    Setcc(k);
                    break;
                case 0x07:
                    l = Getcc();
                    Setcc(k);
                    break;
                case 0x08:
                    l = A;
                    A = k & 0xff;
                    break;
                case 0x09:
                    l = B;
                    B = k & 0xff;
                    break;
                case 0x0A:
                    l = Getcc();
                    Setcc(k);
                    break;
                case 0x0B:
                    l = Dp;
                    Dp = k & 0xff;
                    break;
                case 0x0C:
                    l = Getcc();
                    Setcc(k);
                    break;
                case 0x0D:
                    l = Getcc();
                    Setcc(k);
                    break;
                case 0x0E:
                    l = Getcc();
                    Setcc(k);
                    break;
                case 0x0F:
                    l = Getcc();
                    Setcc(k);
                    break;
            } // of switch r2
            switch (r1)
            {
                case 0x00:
                    A = (l >> 8) & 255;
                    B = l & 255;
                    break;
                case 0x01:
                    X = l;
                    break;
                case 0x02:
                    Y = l;
                    break;
                case 0x03:
                    U = l;
                    break;
                case 0x04:
                    S = l;
                    break;
                case 0x05:
                    Pc = l;
                    break;
                case 0x06:
                    Setcc(l);
                    break;
                case 0x07:
                    Setcc(l);
                    break;
                case 0x08:
                    A = l & 0xff;
                    break;
                case 0x09:
                    B = l & 0xff;
                    break;
                case 0x0A:
                    Setcc(l);
                    break;
                case 0x0B:
                    Dp = l & 0xff;
                    break;
                case 0x0C:
                    Setcc(l);
                    break;
                case 0x0D:
                    Setcc(l);
                    break;
                case 0x0E:
                    Setcc(l);
                    break;
                case 0x0F:
                    Setcc(l);
                    break;
            } // of second switch r1
            _clock += 8;
        }

        private void Tfr()
        {
            int r1;
            int r2;
            int m;
            int k;
            m = _mem.Read(Pc++);
            r1 = (m & 0xF0) >> 4;
            r2 = m & 0x0F;
            k = 0; // only for javac
            switch (r1)
            {
                case 0x00:
                    k = (A << 8) | B;
                    break;
                case 0x01:
                    k = X;
                    break;
                case 0x02:
                    k = Y;
                    break;
                case 0x03:
                    k = U;
                    break;
                case 0x04:
                    k = S;
                    break;
                case 0x05:
                    k = Pc;
                    break;
                case 0x06:
                    k = Getcc();
                    break;
                case 0x07:
                    k = Getcc();
                    break;
                case 0x08:
                    k = A;
                    break;
                case 0x09:
                    k = B;
                    break;
                case 0x0A:
                    k = Getcc();
                    break;
                case 0x0B:
                    k = Dp;
                    break;
                case 0x0C:
                    k = Getcc();
                    break;
                case 0x0D:
                    k = Getcc();
                    break;
                case 0x0E:
                    k = Getcc();
                    break;
                case 0x0F:
                    k = Getcc();
                    break;
            } // of switch r1
            switch (r2)
            {
                case 0x00:
                    A = (k >> 8) & 255;
                    B = k & 255;
                    break;
                case 0x01:
                    X = k;
                    break;
                case 0x02:
                    Y = k;
                    break;
                case 0x03:
                    U = k;
                    break;
                case 0x04:
                    S = k;
                    break;
                case 0x05:
                    Pc = k;
                    break;
                case 0x06:
                    Setcc(k);
                    break;
                case 0x07:
                    Setcc(k);
                    break;
                case 0x08:
                    A = k & 0xff;
                    break;
                case 0x09:
                    B = k & 0xff;
                    break;
                case 0x0A:
                    Setcc(k);
                    break;
                case 0x0B:
                    Dp = k & 0xff;
                    break;
                case 0x0C:
                    Setcc(k);
                    break;
                case 0x0D:
                    Setcc(k);
                    break;
                case 0x0E:
                    Setcc(k);
                    break;
                case 0x0F:
                    Setcc(k);
                    break;
            } // of switch r2
        }

        private void Pshs()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((m & 0x80) != 0)
            {
                S--;
                _mem.Write(S, Pc & 0x00FF);
                S--;
                _mem.Write(S, Pc >> 8);
                _clock += 2;
            }
            if ((m & 0x40) != 0)
            {
                S--;
                _mem.Write(S, U & 0x00FF);
                S--;
                _mem.Write(S, U >> 8);
                _clock += 2;
            }
            if ((m & 0x20) != 0)
            {
                S--;
                _mem.Write(S, Y & 0x00FF);
                S--;
                _mem.Write(S, Y >> 8);
                _clock += 2;
            }
            if ((m & 0x10) != 0)
            {
                S--;
                _mem.Write(S, X & 0x00FF);
                S--;
                _mem.Write(S, X >> 8);
                _clock += 2;
            }
            if ((m & 0x08) != 0)
            {
                S--;
                _mem.Write(S, Dp);
                _clock++;
            }
            if ((m & 0x04) != 0)
            {
                S--;
                _mem.Write(S, B);
                _clock++;
            }
            if ((m & 0x02) != 0)
            {
                S--;
                _mem.Write(S, A);
                _clock++;
            }
            if ((m & 0x01) != 0)
            {
                S--;
                Getcc();
                _mem.Write(S, Cc);
                _clock++;
            }
            _clock += 5;
        }

        private void Pshu()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((m & 0x80) != 0)
            {
                U--;
                _mem.Write(U, Pc & 0x00FF);
                U--;
                _mem.Write(U, Pc >> 8);
                _clock += 2;
            }
            if ((m & 0x40) != 0)
            {
                U--;
                _mem.Write(U, S & 0x00FF);
                U--;
                _mem.Write(U, S >> 8);
                _clock += 2;
            }
            if ((m & 0x20) != 0)
            {
                U--;
                _mem.Write(U, Y & 0x00FF);
                U--;
                _mem.Write(U, Y >> 8);
                _clock += 2;
            }
            if ((m & 0x10) != 0)
            {
                U--;
                _mem.Write(U, X & 0x00FF);
                U--;
                _mem.Write(U, X >> 8);
                _clock += 2;
            }
            if ((m & 0x08) != 0)
            {
                U--;
                _mem.Write(U, Dp);
                _clock++;
            }
            if ((m & 0x04) != 0)
            {
                U--;
                _mem.Write(U, B);
                _clock++;
            }
            if ((m & 0x02) != 0)
            {
                U--;
                _mem.Write(U, A);
                _clock++;
            }
            if ((m & 0x01) != 0)
            {
                U--;
                Getcc();
                _mem.Write(U, Cc);
                _clock++;
            }
            _clock += 5;
        }

        private void Puls()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((m & 0x01) != 0)
            {
                Cc = _mem.Read(S);
                Setcc(Cc);
                S++;
                _clock++;
            }
            if ((m & 0x02) != 0)
            {
                A = _mem.Read(S);
                S++;
                _clock++;
            }
            if ((m & 0x04) != 0)
            {
                B = _mem.Read(S);
                S++;
                _clock++;
            }
            if ((m & 0x08) != 0)
            {
                Dp = _mem.Read(S);
                S++;
                _clock++;
            }
            if ((m & 0x10) != 0)
            {
                X = (_mem.Read(S) << 8) | _mem.Read(S + 1);
                S += 2;
                _clock += 2;
            }
            if ((m & 0x20) != 0)
            {
                Y = (_mem.Read(S) << 8) | _mem.Read(S + 1);
                S += 2;
                _clock += 2;
            }
            if ((m & 0x40) != 0)
            {
                U = (_mem.Read(S) << 8) | _mem.Read(S + 1);
                S += 2;
                _clock += 2;
            }
            if ((m & 0x80) != 0)
            {
                Pc = (_mem.Read(S) << 8) | _mem.Read(S + 1);
                S += 2;
                _clock += 2;
            }
            _clock += 5;
        }

        private void Pulu()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((m & 0x01) != 0)
            {
                Cc = _mem.Read(U);
                Setcc(Cc);
                U++;
                _clock++;
            }
            if ((m & 0x02) != 0)
            {
                A = _mem.Read(U);
                U++;
                _clock++;
            }
            if ((m & 0x04) != 0)
            {
                B = _mem.Read(U);
                U++;
                _clock++;
            }
            if ((m & 0x08) != 0)
            {
                Dp = _mem.Read(U);
                U++;
                _clock++;
            }
            if ((m & 0x10) != 0)
            {
                X = (_mem.Read(U) << 8) | _mem.Read(U + 1);
                U += 2;
                _clock += 2;
            }
            if ((m & 0x20) != 0)
            {
                Y = (_mem.Read(U) << 8) | _mem.Read(U + 1);
                U += 2;
                _clock += 2;
            }
            if ((m & 0x40) != 0)
            {
                S = (_mem.Read(U) << 8) | _mem.Read(U + 1);
                U += 2;
                _clock += 2;
            }
            if ((m & 0x80) != 0)
            {
                Pc = (_mem.Read(U) << 8) | _mem.Read(U + 1);
                U += 2;
                _clock += 2;
            }
            _clock += 5;
        }

        private void Inca()
        {
            _m1 = A;
            _m2 = 0;
            A = (A + 1) & 0xFF;
            _ovfl = _sign = A;
            _res = (_res & 0x100) | _sign;
            _clock += 2;
        }

        private void Incb()
        {
            _m1 = B;
            _m2 = 0;
            B = (B + 1) & 0xFF;
            _ovfl = _sign = B;
            _res = (_res & 0x100) | _sign;
            _clock += 2;
        }

        private void Inc(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = val;
            _m2 = 0;
            val++;
            _mem.Write(adr, val);
            _ovfl = _sign = val & 0xFF;
            _res = (_res & 0x100) | _sign;
            _clock += c;
        }

        // DEC
        private void Deca()
        {
            _m1 = A;
            _m2 = 0x80;
            A = (A - 1) & 0xFF;
            _ovfl = _sign = A;
            _res = (_res & 0x100) | _sign;
            _clock += 2;
        }

        private void Decb()
        {
            _m1 = B;
            _m2 = 0x80;
            B = (B - 1) & 0xFF;
            _ovfl = _sign = B;
            _res = (_res & 0x100) | _sign;
            _clock += 2;
        }

        private void Dec(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = val;
            _m2 = 0x80;
            val--;
            _mem.Write(adr, val);
            _ovfl = _sign = val & 0xFF;
            _res = (_res & 0x100) | _sign;
            _clock += c;
        }

        private void Bit(int r, int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _ovfl;
            _sign = r & val;
            _res = (_res & 0x100) | _sign;
            _clock += c;
        }

        private void Cmp8(int r, int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = r;
            _m2 = -val;
            _ovfl = _res = _sign = r - val;
            _clock += c;
        }

        private void Cmp16(int r, int adr, int c)
        {
            int val;
            val = (_mem.Read(adr) << 8) | _mem.Read(adr + 1);
            _m1 = r >> 8;
            _m2 = -val >> 8;
            _ovfl = _res = _sign = ((r - val) >> 8) & 0xFFFFFF;
            _res |= (r - val) & 0xFF;
            _clock += c;
        }

        // TST
        private void TstAi()
        {
            _m1 = _ovfl;
            _sign = A;
            _res = (_res & 0x100) | _sign;
            _clock += 2;
        }

        private void TstBi()
        {
            _m1 = _ovfl;
            _sign = B;
            _res = (_res & 0x100) | _sign;
            _clock += 2;
        }

        private void Tst(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = ~_m2;
            _sign = val;
            _res = (_res & 0x100) | _sign;
            _clock += c;
        }

        private void Anda(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _ovfl;
            A &= val;
            _sign = A;
            _res = (_res & 0x100) | _sign;
            _clock += c;
        }

        private void Andb(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _ovfl;
            B &= val;
            _sign = B;
            _res = (_res & 0x100) | _sign;
            _clock += c;
        }

        private void Andcc(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            //	getcc();
            Cc &= val;
            Setcc(Cc);
            _clock += c;
        }

        private void Ora(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _ovfl;
            A |= val;
            _sign = A;
            _res = (_res & 0x100) | _sign;
            _clock += c;
        }

        private void Orb(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _ovfl;
            B |= val;
            _sign = B;
            _res = (_res & 0x100) | _sign;
            _clock += c;
        }

        private void Orcc(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            Getcc();
            Cc |= val;
            Setcc(Cc);
            _clock += c;
        }

        private void Eora(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _ovfl;
            A ^= val;
            _sign = A;
            _res = (_res & 0x100) | _sign;
            _clock += c;
        }

        private void Eorb(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _ovfl;
            B ^= val;
            _sign = B;
            _res = (_res & 0x100) | _sign;
            _clock += c;
        }

        private void Coma()
        {
            _m1 = _ovfl;
            A = ~A & 0xFF;
            _sign = A;
            _res = _sign | 0x100;
            _clock += 2;
        }

        private void Comb()
        {
            _m1 = _ovfl;
            B = ~B & 0xFF;
            _sign = B;
            _res = _sign | 0x100;
            _clock += 2;
        }

        private void Com(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = ~_m2;
            val = ~val & 0xFF;
            _mem.Write(adr, val);
            _sign = val;
            _res = _sign | 0x100;
            _clock += c;
        }

        private void Nega()
        {
            _m1 = A;
            _m2 = -A;
            A = -A;
            _ovfl = _res = _sign = A;
            A &= 0xFF;
            _clock += 2;
        }

        private void Negb()
        {
            _m1 = B;
            _m2 = -B;
            B = -B;
            _ovfl = _res = _sign = B;
            B &= 0xFF;
            _clock += 2;
        }

        private void Neg(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = val;
            _m2 = -val;
            val = -val;
            _mem.Write(adr, val);
            _ovfl = _res = _sign = val;
            _clock += c;
        }

        private void Abx()
        {
            X = (X + B) & 0xFFFF;
            _clock += 3;
        }

        private void Adda(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _h1 = A;
            _m2 = _h2 = val;
            A += val;
            _ovfl = _res = _sign = A;
            A &= 0xFF;
            _clock += c;
        }

        private void Addb(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _h1 = B;
            _m2 = _h2 = val;
            B += val;
            _ovfl = _res = _sign = B;
            B &= 0xFF;
            _clock += c;
        }

        private void Addd(int adr, int c)
        {
            int val;
            val = (_mem.Read(adr) << 8) | _mem.Read(adr + 1);
            _m1 = A;
            _m2 = val >> 8;
            D = (A << 8) + B + val;
            A = D >> 8;
            B = D & 0xFF;
            _ovfl = _res = _sign = A;
            _res |= B;
            A &= 0xFF;
            _clock += c;
        }

        private void Adca(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _h1 = A;
            _m2 = val;
            _h2 = val + ((_res & 0x100) >> 8);
            A += _h2;
            _ovfl = _res = _sign = A;
            A &= 0xFF;
            _clock += c;
        }

        private void Adcb(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _h1 = B;
            _m2 = val;
            _h2 = val + ((_res & 0x100) >> 8);
            B += _h2;
            _ovfl = _res = _sign = B;
            B &= 0xFF;
            _clock += c;
        }

        private void Mul()
        {
            int k;
            k = A * B;
            A = (k >> 8) & 0xFF;
            B = k & 0xFF;
            _res = ((B & 0x80) << 1) | ((k | (k >> 8)) & 0xFF);
            _clock += 11;
        }

        private void Sbca(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = A;
            _m2 = -val;
            A -= val + ((_res & 0x100) >> 8);
            _ovfl = _res = _sign = A;
            A &= 0xFF;
            _clock += c;
        }

        private void Sbcb(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = B;
            _m2 = -val;
            B -= val + ((_res & 0x100) >> 8);
            _ovfl = _res = _sign = B;
            B &= 0xFF;
            _clock += c;
        }

        private void Suba(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = A;
            _m2 = -val;
            A -= val;
            _ovfl = _res = _sign = A;
            A &= 0xFF;
            _clock += c;
        }

        private void Subb(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = B;
            _m2 = -val;
            B -= val;
            _ovfl = _res = _sign = B;
            B &= 0xFF;
            _clock += c;
        }

        private void Subd(int adr, int c)
        {
            int val;
            val = (_mem.Read(adr) << 8) | _mem.Read(adr + 1);
            _m1 = A;
            _m2 = -val >> 8;
            D = (A << 8) + B - val;
            A = D >> 8;
            B = D & 0xFF;
            _ovfl = _res = _sign = A;
            _res |= B;
            A &= 0xFF;
            _clock += c;
        }

        private void Sex()
        {
            if ((B & 0x80) == 0x80) A = 0xFF;
            else A = 0;
            _sign = B;
            _res = (_res & 0x100) | _sign;
            _clock += 2;
        }

        private void Asla()
        {
            _m1 = _m2 = A;
            A <<= 1;
            _ovfl = _sign = _res = A;
            A &= 0xFF;
            _clock += 2;
        }

        private void Aslb()
        {
            _m1 = _m2 = B;
            B <<= 1;
            _ovfl = _sign = _res = B;
            B &= 0xFF;
            _clock += 2;
        }

        private void Asl(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _m2 = val;
            val <<= 1;
            _mem.Write(adr, val);
            _ovfl = _sign = _res = val;
            _clock += c;
        }

        private void Asra()
        {
            _res = (A & 1) << 8;
            A = (A >> 1) | (A & 0x80);
            _sign = A;
            _res |= _sign;
            _clock += 2;
        }

        private void Asrb()
        {
            _res = (B & 1) << 8;
            B = (B >> 1) | (B & 0x80);
            _sign = B;
            _res |= _sign;
            _clock += 2;
        }

        private void Asr(int adr, int c)
        {
            var val = _mem.Read(adr);
            _res = (val & 1) << 8;
            val = (val >> 1) | (val & 0x80);
            _mem.Write(adr, val);
            _sign = val;
            _res |= _sign;
            _clock += c;
        }

        private void Lsra()
        {
            _res = (A & 1) << 8;
            A = A >> 1;
            _sign = 0;
            _res |= A;
            _clock += 2;
        }

        private void Lsrb()
        {
            _res = (B & 1) << 8;
            B = B >> 1;
            _sign = 0;
            _res |= B;
            _clock += 2;
        }

        private void Lsr(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _res = (val & 1) << 8;
            val = val >> 1;
            _mem.Write(adr, val);
            _sign = 0;
            _res |= val;
            _clock += c;
        }

        private void Rola()
        {
            _m1 = _m2 = A;
            A = (A << 1) | ((_res & 0x100) >> 8);
            _ovfl = _sign = _res = A;
            A &= 0xFF;
            _clock += 2;
        }

        private void Rolb()
        {
            _m1 = _m2 = B;
            B = (B << 1) | ((_res & 0x100) >> 8);
            _ovfl = _sign = _res = B;
            B &= 0xFF;
            _clock += 2;
        }

        private void Rol(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _m2 = val;
            val = (val << 1) | ((_res & 0x100) >> 8);
            _mem.Write(adr, val);
            _ovfl = _sign = _res = val;
            _clock += c;
        }

        private void Rora()
        {
            int i;
            i = A;
            A = (A | (_res & 0x100)) >> 1;
            _sign = A;
            _res = ((i & 1) << 8) | _sign;
            _clock += 2;
        }

        private void Rorb()
        {
            int i;
            i = B;
            B = (B | (_res & 0x100)) >> 1;
            _sign = B;
            _res = ((i & 1) << 8) | _sign;
            _clock += 2;
        }

        private void Ror(int adr, int c)
        {
            int i;
            int val;
            i = val = _mem.Read(adr);
            val = (val | (_res & 0x100)) >> 1;
            _mem.Write(adr, val);
            _sign = val;
            _res = ((i & 1) << 8) | _sign;
            _clock += c;
        }

        private void Bra()
        {
            int m;
            m = _mem.Read(Pc++);
            Pc += SignedChar(m);
            _clock += 3;
        }

        private void Lbra()
        {
            int m;
            int off;
            m = _mem.Read(Pc++);
            off = m << 8;
            m = _mem.Read(Pc++);
            off |= m;
            Pc = (Pc + off) & 0xFFFF;
            _clock += 5;
        }

        private void JmPd()
        {
            int m;
            m = _mem.Read(Pc++);
            Pc = (Dp << 8) | m;
            _clock += 3;
        }

        private void JmPe()
        {
            int adr;
            adr = Etend();
            Pc = adr;
            _clock += 4;
        }

        private void JmPx()
        {
            int adr;
            adr = Indexe();
            Pc = adr;
            _clock += 3;
        }

        private void Bsr()
        {
            int m;
            m = _mem.Read(Pc++);
            S--;
            _mem.Write(S, Pc & 0x00FF);
            S--;
            _mem.Write(S, Pc >> 8);
            Pc += SignedChar(m);
            _clock += 7;
        }

        private void Lbsr()
        {
            int off;
            int m;
            m = _mem.Read(Pc++);
            off = m << 8;
            m = _mem.Read(Pc++);
            off |= m;
            S--;
            _mem.Write(S, Pc & 0x00FF);
            S--;
            _mem.Write(S, Pc >> 8);
            Pc = (Pc + off) & 0xFFFF;
            _clock += 9;
        }

        private void JsRd()
        {
            int m;
            m = _mem.Read(Pc++);
            S--;
            _mem.Write(S, Pc & 0x00FF);
            S--;
            _mem.Write(S, Pc >> 8);
            Pc = (Dp << 8) | m;
            _clock += 7;
        }

        private void JsRe()
        {
            int adr;
            adr = Etend();
            S--;
            _mem.Write(S, Pc & 0x00FF);
            S--;
            _mem.Write(S, Pc >> 8);
            Pc = adr;
            _clock += 8;
        }

        private void JsRx()
        {
            int adr;
            adr = Indexe();
            S--;
            _mem.Write(S, Pc & 0x00FF);
            S--;
            _mem.Write(S, Pc >> 8);
            Pc = adr;
            _clock += 7;
        }

        private void Brn()
        {
            _mem.Read(Pc++);
            _clock += 3;
        }

        private void Lbrn()
        {
            _mem.Read(Pc++);
            _mem.Read(Pc++);
            _clock += 5;
        }

        private void Nop()
        {
            _clock += 2;
        }

        private void Rts()
        {
            Pc = (_mem.Read(S) << 8) | _mem.Read(S + 1);
            S += 2;
            _clock += 5;
        }

        /* Branchements conditionnels */

        private void Bcc()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_res & 0x100) != 0x100) Pc += SignedChar(m);
            _clock += 3;
        }

        private void Lbcc()
        {
            int off;
            int m;
            m = _mem.Read(Pc++);
            off = m << 8;
            m = _mem.Read(Pc++);
            off |= m;
            if ((_res & 0x100) != 0x100)
            {
                Pc = (Pc + off) & 0xFFFF;
                _clock += 6;
            }
            else _clock += 5;
        }

        private void Bcs()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_res & 0x100) == 0x100) Pc += SignedChar(m);
            _clock += 3;
        }

        private void Lbcs()
        {
            int off;
            int m;
            m = _mem.Read(Pc++);
            off = m << 8;
            m = _mem.Read(Pc++);
            off |= m;
            if ((_res & 0x100) == 0x100)
            {
                Pc = (Pc + off) & 0xFFFF;
                _clock += 6;
            }
            else _clock += 5;
        }

        private void Beq()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_res & 0xff) == 0x00) Pc += SignedChar(m);
            _clock += 3;
        }

        private void Lbeq()
        {
            int off;
            int m;
            m = _mem.Read(Pc++);
            off = m << 8;
            m = _mem.Read(Pc++);
            off |= m;
            if ((_res & 0xff) == 0x00)
            {
                Pc = (Pc + off) & 0xFFFF;
                _clock += 6;
            }
            else _clock += 6;
        }

        private void Bne()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_res & 0xff) != 0) Pc += SignedChar(m);
            _clock += 3;
        }

        private void Lbne()
        {
            int off;
            int m;
            m = _mem.Read(Pc++);
            off = m << 8;
            m = _mem.Read(Pc++);
            off |= m;
            if ((_res & 0xff) != 0)
            {
                Pc = (Pc + off) & 0xFFFF;
                _clock += 6;
            }
            else _clock += 5;
        }

        private void Bge()
        {
            int m;
            m = _mem.Read(Pc++);
            if (((_sign ^ (~(_m1 ^ _m2) & (_m1 ^ _ovfl))) & 0x80) == 0) Pc += SignedChar(m);
            _clock += 3;
        }

        private void Lbge()
        {
            int off;
            int m;
            m = _mem.Read(Pc++);
            off = m << 8;
            m = _mem.Read(Pc++);
            off |= m;
            if (((_sign ^ (~(_m1 ^ _m2) & (_m1 ^ _ovfl))) & 0x80) == 0)
            {
                Pc = (Pc + off) & 0xFFFF;
                _clock += 6;
            }
            else _clock += 5;
        }

        private void Ble()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_res & 0xff) == 0
                || ((_sign ^ (~(_m1 ^ _m2) & (_m1 ^ _ovfl))) & 0x80) != 0) Pc += SignedChar(m);
            _clock += 3;
        }

        private void Lble()
        {
            int off;
            int m;
            m = _mem.Read(Pc++);
            off = m << 8;
            m = _mem.Read(Pc++);
            off |= m;
            if ((_res & 0xff) == 0
                || ((_sign ^ (~(_m1 ^ _m2) & (_m1 ^ _ovfl))) & 0x80) != 0)
            {
                Pc = (Pc + off) & 0xFFFF;
                _clock += 6;
            }
            else _clock += 5;
        }

        private void Bls()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_res & 0x100) != 0 || (_res & 0xff) == 0) Pc += SignedChar(m);
            _clock += 3;
        }

        private void Lbls()
        {
            int off;
            int m;
            m = _mem.Read(Pc++);
            off = m << 8;
            m = _mem.Read(Pc++);
            off |= m;
            if ((_res & 0x100) != 0 || (_res & 0xff) == 0)
            {
                Pc = (Pc + off) & 0xFFFF;
                _clock += 6;
            }
            else _clock += 5;
        }

        private void Bgt()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_res & 0xff) != 0
                && ((_sign ^ (~(_m1 ^ _m2) & (_m1 ^ _ovfl))) & 0x80) == 0) Pc += SignedChar(m);
            _clock += 3;
        }

        private void Lbgt()
        {
            int off;
            int m;
            m = _mem.Read(Pc++);
            off = m << 8;
            m = _mem.Read(Pc++);
            off |= m;
            if ((_res & 0xff) != 0
                && ((_sign ^ (~(_m1 ^ _m2) & (_m1 ^ _ovfl))) & 0x80) == 0)
            {
                Pc = (Pc + off) & 0xFFFF;
                _clock += 6;
            }
            else _clock += 5;
        }

        private void Blt()
        {
            int m;
            m = _mem.Read(Pc++);
            if (((_sign ^ (~(_m1 ^ _m2) & (_m1 ^ _ovfl))) & 0x80) != 0) Pc += SignedChar(m);
            _clock += 3;
        }

        private void Lblt()
        {
            int off;
            int m;
            m = _mem.Read(Pc++);
            off = m << 8;
            m = _mem.Read(Pc++);
            off |= m;
            if (((_sign ^ (~(_m1 ^ _m2) & (_m1 ^ _ovfl))) & 0x80) != 0)
            {
                Pc = (Pc + off) & 0xFFFF;
                _clock += 6;
            }
            else _clock += 5;
        }

        private void Bhi()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_res & 0x100) == 0 && (_res & 0xff) != 0) Pc += SignedChar(m);
            _clock += 3;
        }

        private void Lbhi()
        {
            int off;
            int m;
            m = _mem.Read(Pc++);
            off = m << 8;
            m = _mem.Read(Pc++);
            off |= m;
            if ((_res & 0x100) == 0 && (_res & 0xff) != 0)
            {
                Pc = (Pc + off) & 0xFFFF;
                _clock += 6;
            }
            _clock += 5;
        }

        private void Bmi()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_sign & 0x80) != 0) Pc += SignedChar(m);
            _clock += 3;
        }

        private void Lbmi()
        {
            int off;
            int m;
            m = _mem.Read(Pc++);
            off = m << 8;
            m = _mem.Read(Pc++);
            off |= m;
            if ((_sign & 0x80) != 0)
            {
                Pc = (Pc + off) & 0xFFFF;
                _clock += 6;
            }
            else _clock += 5;
        }

        private void Bpl()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_sign & 0x80) == 0) Pc += SignedChar(m);
            _clock += 3;
        }

        private void Lbpl()
        {
            int off;
            int m;
            m = _mem.Read(Pc++);
            off = m << 8;
            m = _mem.Read(Pc++);
            off |= m;
            if ((_sign & 0x80) == 0)
            {
                Pc = (Pc + off) & 0xFFFF;
                _clock += 6;
            }
            else _clock += 5;
        }

        private void Bvs()
        {
            int m;
            m = _mem.Read(Pc++);
            if (((_m1 ^ _m2) & 0x80) == 0 && ((_m1 ^ _ovfl) & 0x80) != 0) Pc += SignedChar(m);
            _clock += 3;
        }

        private void Lbvs()
        {
            int off;
            int m;
            m = _mem.Read(Pc++);
            off = m << 8;
            m = _mem.Read(Pc++);
            off |= m;
            if (((_m1 ^ _m2) & 0x80) == 0 && ((_m1 ^ _ovfl) & 0x80) != 0)
            {
                Pc = (Pc + off) & 0xFFFF;
                _clock += 6;
            }
            else _clock += 5;
        }

        private void Bvc()
        {
            int m;
            m = _mem.Read(Pc++);
            if (((_m1 ^ _m2) & 0x80) != 0 || ((_m1 ^ _ovfl) & 0x80) == 0) Pc += SignedChar(m);
            _clock += 3;
        }

        private void Lbvc()
        {
            int off;
            int m;
            m = _mem.Read(Pc++);
            off = m << 8;
            m = _mem.Read(Pc++);
            off |= m;
            if (((_m1 ^ _m2) & 0x80) != 0 || ((_m1 ^ _ovfl) & 0x80) == 0)
            {
                Pc = (Pc + off) & 0xFFFF;
                _clock += 6;
            }
            else _clock += 5;
        }

        private void Swi()
        {
            Getcc();
            Cc |= 0x80; /* bit E � 1 */
            Setcc(Cc);
            S--;
            _mem.Write(S, Pc & 0x00FF);
            S--;
            _mem.Write(S, Pc >> 8);
            S--;
            _mem.Write(S, U & 0x00FF);
            S--;
            _mem.Write(S, U >> 8);
            S--;
            _mem.Write(S, Y & 0x00FF);
            S--;
            _mem.Write(S, Y >> 8);
            S--;
            _mem.Write(S, X & 0x00FF);
            S--;
            _mem.Write(S, X >> 8);
            S--;
            _mem.Write(S, Dp);
            S--;
            _mem.Write(S, B);
            S--;
            _mem.Write(S, A);
            S--;
            _mem.Write(S, Cc);

            Pc = (_mem.Read(0xFFFA) << 8) | _mem.Read(0xFFFB);
            _clock += 19;
        }

        private void Rti()
        {
            Cc = _mem.Read(S);
            Setcc(Cc);
            S++;
            if ((Cc & 0x80) == 0x80)
            {
                A = _mem.Read(S);
                S++;
                B = _mem.Read(S);
                S++;
                Dp = _mem.Read(S);
                S++;
                X = (_mem.Read(S) << 8) | _mem.Read(S + 1);
                S += 2;
                Y = (_mem.Read(S) << 8) | _mem.Read(S + 1);
                S += 2;
                U = (_mem.Read(S) << 8) | _mem.Read(S + 1);
                S += 2;
                _clock += 15;
            }
            else _clock += 6;

            Pc = (_mem.Read(S) << 8) | _mem.Read(S + 1);
            S += 2;
        }

        public void Irq()
        {
            /* mise � 1 du bit E sur le CC */
            Getcc();
            Cc |= 0x80;
            Setcc(Cc);
            S--;
            _mem.Write(S, Pc & 0x00FF);
            S--;
            _mem.Write(S, Pc >> 8);
            S--;
            _mem.Write(S, U & 0x00FF);
            S--;
            _mem.Write(S, U >> 8);
            S--;
            _mem.Write(S, Y & 0x00FF);
            S--;
            _mem.Write(S, Y >> 8);
            S--;
            _mem.Write(S, X & 0x00FF);
            S--;
            _mem.Write(S, X >> 8);
            S--;
            _mem.Write(S, Dp);
            S--;
            _mem.Write(S, B);
            S--;
            _mem.Write(S, A);
            S--;
            _mem.Write(S, Cc);
            Pc = (_mem.Read(0xFFF8) << 8) | _mem.Read(0xFFF9);
            Cc |= 0x10;
            Setcc(Cc);
            _clock += 19;
        }

        private void Daa()
        {
            int i = A + (_res & 0x100);
            if ((A & 15) > 9 || (_h1 & 15) + (_h2 & 15) > 15) i += 6;
            if (i > 0x99) i += 0x60;
            _res = _sign = i;
            A = i & 255;
            _clock += 2;
        }

        private void Cwai()
        {
            Getcc();
            Cc &= _mem.Read(Pc);
            Setcc(Cc);
            Pc++;
            _clock += 20;
        }

        public int Fetch()
        {
            int clock = _clock;
            int pc = Pc;
            int opcode = _mem.Read(Pc);
            int opcode0X10 = 0;
			int opcode0X11 = 0;
            Pc++;

            // 	Sound emulation process
            SoundBuffer[_soundAddr] = (byte)_mem.SoundMem;
            _soundAddr = (_soundAddr + 1) % SoundSize;
            if (_soundAddr == 0)
                _play.PlaySound(SoundBuffer);

            switch (opcode)
            {
                // the mystery undocumented opcode
                case 0x01:
                    Pc++;
                    _clock += 2;
                    break;

                // LDA
                case 0x86:
                    A = Ld8(Immed8(), 2);
                    break;
                case 0x96:
                    A = Ld8(Direc(), 4);
                    break;
                case 0xB6:
                    A = Ld8(Etend(), 5);
                    break;
                case 0xA6:
                    A = Ld8(Indexe(), 4);
                    break;

                // LDB
                case 0xC6:
                    B = Ld8(Immed8(), 2);
                    break;
                case 0xD6:
                    B = Ld8(Direc(), 4);
                    break;
                case 0xF6:
                    B = Ld8(Etend(), 5);
                    break;
                case 0xE6:
                    B = Ld8(Indexe(), 4);
                    break;

                // LDD
                case 0xCC:
                    D = Ld16(Immed16(), 3);
                    break;
                case 0xDC:
                    D = Ld16(Direc(), 5);
                    break;
                case 0xFC:
                    D = Ld16(Etend(), 6);
                    break;
                case 0xEC:
                    D = Ld16(Indexe(), 5);
                    break;

                // LDU
                case 0xCE:
                    U = Ld16(Immed16(), 3);
                    break;
                case 0xDE:
                    U = Ld16(Direc(), 5);
                    break;
                case 0xFE:
                    U = Ld16(Etend(), 6);
                    break;
                case 0xEE:
                    U = Ld16(Indexe(), 5);
                    break;


                // LDX
                case 0x8E:
                    X = Ld16(Immed16(), 3);
                    break;
                case 0x9E:
                    X = Ld16(Direc(), 5);
                    break;
                case 0xBE:
                    X = Ld16(Etend(), 6);
                    break;
                case 0xAE:
                    X = Ld16(Indexe(), 5);
                    break;

                // STA 
                case 0x97:
                    St8(A, Direc(), 4);
                    break;
                case 0xB7:
                    St8(A, Etend(), 5);
                    break;
                case 0xA7:
                    St8(A, Indexe(), 4);
                    break;

                // STB
                case 0xD7:
                    St8(B, Direc(), 4);
                    break;
                case 0xF7:
                    St8(B, Etend(), 5);
                    break;
                case 0xE7:
                    St8(B, Indexe(), 4);
                    break;

                // STD
                case 0xDD:
                    St16(D, Direc(), 5);
                    break;
                case 0xFD:
                    St16(D, Etend(), 6);
                    break;
                case 0xED:
                    St16(D, Indexe(), 6);
                    break;

                // STU
                case 0xDF:
                    St16(U, Direc(), 5);
                    break;
                case 0xFF:
                    St16(U, Etend(), 6);
                    break;
                case 0xEF:
                    St16(U, Indexe(), 5);
                    break;

                // STX
                case 0x9F:
                    St16(X, Direc(), 5);
                    break;
                case 0xBF:
                    St16(X, Etend(), 6);
                    break;
                case 0xAF:
                    St16(X, Indexe(), 5);
                    break;

                // LEAS
                case 0x32:
                    S = Indexe();
                    break;
                // LEAU
                case 0x33:
                    U = Indexe();
                    break;
                // LEAX
                case 0x30:
                    X = Lea();
                    break;
                // LEAY
                case 0x31:
                    Y = Lea();
                    break;

                // CLRA
                case 0x4F:
                    A = 0;
                    _m1 = _ovfl;
                    _sign = _res = 0;
                    _clock += 2;
                    break;
                // CLRB
                case 0x5F:
                    B = 0;
                    _m1 = _ovfl;
                    _sign = _res = 0;
                    _clock += 2;
                    break;
                // CLR
                case 0x0F:
                    Clr(Direc(), 6);
                    break;
                case 0x7F:
                    Clr(Etend(), 7);
                    break;
                case 0x6F:
                    Clr(Indexe(), 6);
                    break;

                // EXG
                case 0x1E:
                    Exg();
                    break;

                // TFR
                case 0x1F:
                    Tfr();
                    break;

                // PSH/PUL
                case 0x34:
                    Pshs();
                    break;
                case 0x36:
                    Pshu();
                    break;
                case 0x35:
                    Puls();
                    break;
                case 0x37:
                    Pulu();
                    break;

                // INC
                case 0x4C:
                    Inca();
                    break;
                case 0x5C:
                    Incb();
                    break;
                case 0x7C:
                    Inc(Etend(), 7);
                    break;
                case 0x0C:
                    Inc(Direc(), 6);
                    break;
                case 0x6C:
                    Inc(Indexe(), 6);
                    break;

                // DEC
                case 0x4A:
                    Deca();
                    break;
                case 0x5A:
                    Decb();
                    break;
                case 0x7A:
                    Dec(Etend(), 7);
                    break;
                case 0x0A:
                    Dec(Direc(), 6);
                    break;
                case 0x6A:
                    Dec(Indexe(), 6);
                    break;

                // BIT
                case 0x85:
                    Bit(A, Immed8(), 2);
                    break;
                case 0x95:
                    Bit(A, Direc(), 4);
                    break;
                case 0xB5:
                    Bit(A, Etend(), 5);
                    break;
                case 0xA5:
                    Bit(A, Indexe(), 4);
                    break;
                case 0xC5:
                    Bit(B, Immed8(), 2);
                    break;
                case 0xD5:
                    Bit(B, Direc(), 4);
                    break;
                case 0xF5:
                    Bit(B, Etend(), 5);
                    break;
                case 0xE5:
                    Bit(B, Indexe(), 4);
                    break;

                // CMP
                case 0x81:
                    Cmp8(A, Immed8(), 2);
                    break;
                case 0x91:
                    Cmp8(A, Direc(), 4);
                    break;
                case 0xB1:
                    Cmp8(A, Etend(), 5);
                    break;
                case 0xA1:
                    Cmp8(A, Indexe(), 4);
                    break;
                case 0xC1:
                    Cmp8(B, Immed8(), 2);
                    break;
                case 0xD1:
                    Cmp8(B, Direc(), 4);
                    break;
                case 0xF1:
                    Cmp8(B, Etend(), 5);
                    break;
                case 0xE1:
                    Cmp8(B, Indexe(), 4);
                    break;
                case 0x8C:
                    Cmp16(X, Immed16(), 5);
                    break;
                case 0x9C:
                    Cmp16(X, Direc(), 7);
                    break;
                case 0xBC:
                    Cmp16(X, Etend(), 8);
                    break;
                case 0xAC:
                    Cmp16(X, Indexe(), 7);
                    break;

                // TST
                case 0x4D:
                    TstAi();
                    break;
                case 0x5D:
                    TstBi();
                    break;
                case 0x0D:
                    Tst(Direc(), 6);
                    break;
                case 0x7D:
                    Tst(Etend(), 7);
                    break;
                case 0x6D:
                    Tst(Indexe(), 6);
                    break;

                // AND	
                case 0x84:
                    Anda(Immed8(), 2);
                    break;
                case 0x94:
                    Anda(Direc(), 4);
                    break;
                case 0xB4:
                    Anda(Etend(), 5);
                    break;
                case 0xA4:
                    Anda(Indexe(), 4);
                    break;
                case 0xC4:
                    Andb(Immed8(), 2);
                    break;
                case 0xD4:
                    Andb(Direc(), 4);
                    break;
                case 0xF4:
                    Andb(Etend(), 5);
                    break;
                case 0xE4:
                    Andb(Indexe(), 4);
                    break;
                case 0x1C:
                    Andcc(Immed8(), 3);
                    break;

                // OR	
                case 0x8A:
                    Ora(Immed8(), 2);
                    break;
                case 0x9A:
                    Ora(Direc(), 4);
                    break;
                case 0xBA:
                    Ora(Etend(), 5);
                    break;
                case 0xAA:
                    Ora(Indexe(), 4);
                    break;
                case 0xCA:
                    Orb(Immed8(), 2);
                    break;
                case 0xDA:
                    Orb(Direc(), 4);
                    break;
                case 0xFA:
                    Orb(Etend(), 5);
                    break;
                case 0xEA:
                    Orb(Indexe(), 4);
                    break;
                case 0x1A:
                    Orcc(Immed8(), 3);
                    break;

                // EOR	
                case 0x88:
                    Eora(Immed8(), 2);
                    break;
                case 0x98:
                    Eora(Direc(), 4);
                    break;
                case 0xB8:
                    Eora(Etend(), 5);
                    break;
                case 0xA8:
                    Eora(Indexe(), 4);
                    break;
                case 0xC8:
                    Eorb(Immed8(), 2);
                    break;
                case 0xD8:
                    Eorb(Direc(), 4);
                    break;
                case 0xF8:
                    Eorb(Etend(), 5);
                    break;
                case 0xE8:
                    Eorb(Indexe(), 4);
                    break;

                // COM
                case 0x43:
                    Coma();
                    break;
                case 0x53:
                    Comb();
                    break;
                case 0x03:
                    Com(Direc(), 6);
                    break;
                case 0x73:
                    Com(Etend(), 7);
                    break;
                case 0x63:
                    Com(Indexe(), 6);
                    break;

                // NEG
                case 0x40:
                    Nega();
                    break;
                case 0x50:
                    Negb();
                    break;
                case 0x00:
                    Neg(Direc(), 6);
                    break;
                case 0x70:
                    Neg(Etend(), 7);
                    break;
                case 0x60:
                    Neg(Indexe(), 6);
                    break;

                // ABX
                case 0x3A:
                    Abx();
                    break;

                //ADD	
                case 0x8B:
                    Adda(Immed8(), 2);
                    break;
                case 0x9B:
                    Adda(Direc(), 4);
                    break;
                case 0xBB:
                    Adda(Etend(), 5);
                    break;
                case 0xAB:
                    Adda(Indexe(), 4);
                    break;

                case 0xCB:
                    Addb(Immed8(), 2);
                    break;
                case 0xDB:
                    Addb(Direc(), 4);
                    break;
                case 0xFB:
                    Addb(Etend(), 5);
                    break;
                case 0xEB:
                    Addb(Indexe(), 4);
                    break;

                case 0xC3:
                    Addd(Immed16(), 4);
                    break;
                case 0xD3:
                    Addd(Direc(), 6);
                    break;
                case 0xF3:
                    Addd(Etend(), 7);
                    break;
                case 0xE3:
                    Addd(Indexe(), 6);
                    break;

                // ADC
                case 0x89:
                    Adca(Immed8(), 2);
                    break;
                case 0x99:
                    Adca(Direc(), 4);
                    break;
                case 0xB9:
                    Adca(Etend(), 5);
                    break;
                case 0xA9:
                    Adca(Indexe(), 4);
                    break;

                case 0xC9:
                    Adcb(Immed8(), 2);
                    break;
                case 0xD9:
                    Adcb(Direc(), 4);
                    break;
                case 0xF9:
                    Adcb(Etend(), 5);
                    break;
                case 0xE9:
                    Adcb(Indexe(), 4);
                    break;

                // MUL
                case 0x3D:
                    Mul();
                    break;

                // SBC
                case 0x82:
                    Sbca(Immed8(), 2);
                    break;
                case 0x92:
                    Sbca(Direc(), 4);
                    break;
                case 0xB2:
                    Sbca(Etend(), 5);
                    break;
                case 0xA2:
                    Sbca(Indexe(), 4);
                    break;

                case 0xC2:
                    Sbcb(Immed8(), 2);
                    break;
                case 0xD2:
                    Sbcb(Direc(), 4);
                    break;
                case 0xF2:
                    Sbcb(Etend(), 5);
                    break;
                case 0xE2:
                    Sbcb(Indexe(), 4);
                    break;

                //SUB	
                case 0x80:
                    Suba(Immed8(), 2);
                    break;
                case 0x90:
                    Suba(Direc(), 4);
                    break;
                case 0xB0:
                    Suba(Etend(), 5);
                    break;
                case 0xA0:
                    Suba(Indexe(), 4);
                    break;

                case 0xC0:
                    Subb(Immed8(), 2);
                    break;
                case 0xD0:
                    Subb(Direc(), 4);
                    break;
                case 0xF0:
                    Subb(Etend(), 5);
                    break;
                case 0xE0:
                    Subb(Indexe(), 4);
                    break;

                case 0x83:
                    Subd(Immed16(), 4);
                    break;
                case 0x93:
                    Subd(Direc(), 6);
                    break;
                case 0xB3:
                    Subd(Etend(), 7);
                    break;
                case 0xA3:
                    Subd(Indexe(), 6);
                    break;

                // SEX
                case 0x1D:
                    Sex();
                    break;

                // ASL
                case 0x48:
                    Asla();
                    break;
                case 0x58:
                    Aslb();
                    break;
                case 0x08:
                    Asl(Direc(), 6);
                    break;
                case 0x78:
                    Asl(Etend(), 7);
                    break;
                case 0x68:
                    Asl(Indexe(), 6);
                    break;

                // ASR
                case 0x47:
                    Asra();
                    break;
                case 0x57:
                    Asrb();
                    break;
                case 0x07:
                    Asr(Direc(), 6);
                    break;
                case 0x77:
                    Asr(Etend(), 7);
                    break;
                case 0x67:
                    Asr(Indexe(), 6);
                    break;

                // LSR
                case 0x44:
                    Lsra();
                    break;
                case 0x54:
                    Lsrb();
                    break;
                case 0x04:
                    Lsr(Direc(), 6);
                    break;
                case 0x74:
                    Lsr(Etend(), 7);
                    break;
                case 0x64:
                    Lsr(Indexe(), 6);
                    break;

                // ROL
                case 0x49:
                    Rola();
                    break;
                case 0x59:
                    Rolb();
                    break;
                case 0x09:
                    Rol(Direc(), 6);
                    break;
                case 0x79:
                    Rol(Etend(), 7);
                    break;
                case 0x69:
                    Rol(Indexe(), 6);
                    break;

                // ROR
                case 0x46:
                    Rora();
                    break;
                case 0x56:
                    Rorb();
                    break;
                case 0x06:
                    Ror(Direc(), 6);
                    break;
                case 0x76:
                    Ror(Etend(), 7);
                    break;
                case 0x66:
                    Ror(Indexe(), 6);
                    break;

                // BRA 
                case 0x20:
                    Bra();
                    break;
                case 0x16:
                    Lbra();
                    break;

                // JMP 
                case 0x0E:
                    JmPd();
                    break;
                case 0x7E:
                    JmPe();
                    break;
                case 0x6E:
                    JmPx();
                    break;

                // BSR 
                case 0x8D:
                    Bsr();
                    break;
                case 0x17:
                    Lbsr();
                    break;

                // JSR 
                case 0x9D:
                    JsRd();
                    break;
                case 0xBD:
                    JsRe();
                    break;
                case 0xAD:
                    JsRx();
                    break;

                case 0x12:
                    Nop();
                    break;
                case 0x39:
                    Rts();
                    break;

                // Bxx
                case 0x21:
                    Brn();
                    break;
                case 0x24:
                    Bcc();
                    break;
                case 0x25:
                    Bcs();
                    break;
                case 0x27:
                    Beq();
                    break;
                case 0x26:
                    Bne();
                    break;
                case 0x2C:
                    Bge();
                    break;
                case 0x2F:
                    Ble();
                    break;
                case 0x23:
                    Bls();
                    break;
                case 0x2E:
                    Bgt();
                    break;
                case 0x2D:
                    Blt();
                    break;
                case 0x22:
                    Bhi();
                    break;
                case 0x2B:
                    Bmi();
                    break;
                case 0x2A:
                    Bpl();
                    break;
                case 0x28:
                    Bvc();
                    break;
                case 0x29:
                    Bvs();
                    break;

                case 0x3F:
                    Swi();
                    break;
                case 0x3B:
                    Rti();
                    break;
                case 0x19:
                    Daa();
                    break;
                case 0x3C:
                    Cwai();
                    break;

                // extended mode
                case 0x10:
                    opcode0X10 = _mem.Read(Pc++);
                    switch (opcode0X10)
                    {
                        // LDS
                        case 0xCE:
                            S = Ld16(Immed16(), 3);
                            break;
                        case 0xDE:
                            S = Ld16(Direc(), 5);
                            break;
                        case 0xFE:
                            S = Ld16(Etend(), 6);
                            break;
                        case 0xEE:
                            S = Ld16(Indexe(), 5);
                            break;

                        // LDY
                        case 0x8E:
                            Y = Ld16(Immed16(), 3);
                            break;
                        case 0x9E:
                            Y = Ld16(Direc(), 5);
                            break;
                        case 0xBE:
                            Y = Ld16(Etend(), 6);
                            break;
                        case 0xAE:
                            Y = Ld16(Indexe(), 5);
                            break;

                        // STS
                        case 0xDF:
                            St16(S, Direc(), 5);
                            break;
                        case 0xFF:
                            St16(S, Etend(), 6);
                            break;
                        case 0xEF:
                            St16(S, Indexe(), 5);
                            break;

                        // STY
                        case 0x9F:
                            St16(Y, Direc(), 5);
                            break;
                        case 0xBF:
                            St16(Y, Etend(), 6);
                            break;
                        case 0xAF:
                            St16(Y, Indexe(), 5);
                            break;

                        // CMP
                        case 0x83:
                            Cmp16(D, Immed16(), 5);
                            break;
                        case 0x93:
                            Cmp16(D, Direc(), 7);
                            break;
                        case 0xB3:
                            Cmp16(D, Etend(), 8);
                            break;
                        case 0xA3:
                            Cmp16(D, Indexe(), 7);
                            break;
                        case 0x8C:
                            Cmp16(Y, Immed16(), 5);
                            break;
                        case 0x9C:
                            Cmp16(Y, Direc(), 7);
                            break;
                        case 0xBC:
                            Cmp16(Y, Etend(), 8);
                            break;
                        case 0xAC:
                            Cmp16(Y, Indexe(), 7);
                            break;

                        // Bxx
                        case 0x21:
                            Lbrn();
                            break;
                        case 0x24:
                            Lbcc();
                            break;
                        case 0x25:
                            Lbcs();
                            break;
                        case 0x27:
                            Lbeq();
                            break;
                        case 0x26:
                            Lbne();
                            break;
                        case 0x2C:
                            Lbge();
                            break;
                        case 0x2F:
                            Lble();
                            break;
                        case 0x23:
                            Lbls();
                            break;
                        case 0x2E:
                            Lbgt();
                            break;
                        case 0x2D:
                            Lblt();
                            break;
                        case 0x22:
                            Lbhi();
                            break;
                        case 0x2B:
                            Lbmi();
                            break;
                        case 0x2A:
                            Lbpl();
                            break;
                        case 0x28:
                            Lbvc();
                            break;
                        case 0x29:
                            Lbvs();
                            break;
                        default:
                            opcode = opcode << 8 | opcode0X10;
                            OnOpcodeExecuted(pc, opcode);
                            _instructionsCount++;
                            return -opcode;
                    } // of case opcode0x10
                    break;
                case 0x11:
                    opcode0X11 = _mem.Read(Pc++);
                    switch (opcode0X11)
                    {
                        // CMP
                        case 0x8C:
                            Cmp16(S, Immed16(), 5);
                            break;
                        case 0x9C:
                            Cmp16(S, Direc(), 7);
                            break;
                        case 0xBC:
                            Cmp16(S, Etend(), 8);
                            break;
                        case 0xAC:
                            Cmp16(S, Indexe(), 7);
                            break;
                        case 0x83:
                            Cmp16(U, Immed16(), 5);
                            break;
                        case 0x93:
                            Cmp16(U, Direc(), 7);
                            break;
                        case 0xB3:
                            Cmp16(U, Etend(), 8);
                            break;
                        case 0xA3:
                            Cmp16(U, Indexe(), 7);
                            break;
                        default:
                            opcode = opcode << 8 | opcode0X11;
                            OnOpcodeExecuted(pc, opcode);
                            _instructionsCount++;
                            return -opcode;
                    } // of case opcode 0x11 
                    break;

                default:
                    return -opcode;
            }
            OnOpcodeExecuted(pc, opcode);
            _instructionsCount++;
            return _clock - clock;
        }

        private void OnOpcodeExecuted(int pc, int opcode)
        {
            OpcodeExecuted?.Invoke(this, new OpcodeExecutedEventArgs(pc, opcode));
        }
    } // of class M6809
}