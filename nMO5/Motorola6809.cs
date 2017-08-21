using System.Text;
using static nMO5.Util;

namespace nMO5
{
    internal class M6809
    {
        private const int SoundSize = 1024;

        private readonly Memory _mem;

// Sound emulation parameters
        public byte[] SoundBuffer { get; }

        private int _soundAddr;
        private readonly ISound _play;

        private int _cl;

// 8bits registers
        private int _a;

        private int _b;
        private int _dp;
        private int _cc;

// 16bits registers
        private int _x;

        private int _y;
        private int _u;
        private int _s;
        private int _d; // D is A+B

// fast CC bits (as ints) 
        private int _res;

        private int _m1;
        private int _m2;
        private int _sign;
        private int _ovfl;
        private int _h1;
        private int _h2;
        private int _ccrest;

        public M6809(Memory mem, ISound play)
        {
            _mem = mem;
            _play = play;

            // Sound emulation init
            SoundBuffer = new byte[SoundSize];
            _soundAddr = 0;

            Reset();
        }

        public int Pc { get; private set; }

        public void Reset()
        {
            Pc = (_mem.Read(0xFFFE) << 8) | _mem.Read(0xFFFF);
            _dp = 0x00;
            _s = 0x8000;
            _cc = 0x00;
        }

// recalculate A and B or D
        private void Calcd()
        {
            _d = (_a << 8) | _b;
        }

        private void Calcab()
        {
            _a = _d >> 8;
            _b = _d & 0xFF;
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
            int m;
            m = Pc;
            Pc += 2;
            return m;
        }

        private int Direc()
        {
            int m;
            m = (_dp << 8) | _mem.Read(Pc);
            Pc++;
            return m;
        }

        private int Etend()
        {
            int m;
            m = _mem.Read(Pc) << 8;
            Pc++;
            m |= _mem.Read(Pc);
            Pc++;
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
                        reg = _x;
                        break;
                    case 0x20:
                        reg = _y;
                        break;
                    case 0x40:
                        reg = _u;
                        break;
                    case 0x60:
                        reg = _s;
                        break;
                    default: return 0;
                }
                _cl++;
                return (reg + delta) & 0xFFFF;
            }
            switch (m)
            {
                case 0x80: //i_d_P1_X
                    M = _x;
                    _x = (_x + 1) & 0xFFFF;
                    _cl += 2;
                    return M;
                case 0x81: //i_d_P2_X
                    M = _x;
                    _x = (_x + 2) & 0xFFFF;
                    _cl += 3;
                    return M;
                case 0x82: //i_d_M1_X
                    _x = (_x - 1) & 0xFFFF;
                    M = _x;
                    _cl += 2;
                    return M;
                case 0x83: //i_d_M2_X
                    _x = (_x - 2) & 0xFFFF;
                    M = _x;
                    _cl += 3;
                    return M;
                case 0x84: //i_d_X
                    M = _x;
                    return M;
                case 0x85: //i_d_B_X
                    M = (_x + SignedChar(_b)) & 0xFFFF;
                    _cl += 1;
                    return M;
                case 0x86: //i_d_A_X;
                    M = (_x + SignedChar(_a)) & 0xFFFF;
                    _cl += 1;
                    return M;
                case 0x87: return 0; //i_undoc;	/* empty */
                case 0x88: //i_d_8_X;
                    m2 = _mem.Read(Pc);
                    Pc++;
                    M = (_x + SignedChar(m2)) & 0xFFFF;
                    _cl += 1;
                    return M;
                case 0x89: //i_d_16_X;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc += 2;
                    M = (_x + Signed16Bits(m2)) & 0xFFFF;
                    _cl += 4;
                    return M;
                case 0x8A: return 0; //i_undoc;	/* empty */
                case 0x8B: //i_d_D_X;
                    M = (_x + Signed16Bits((_a << 8) | _b)) & 0xFFFF;
                    _cl += 4;
                    return M;
                case 0x8C: //i_d_PC8;
                case 0xAC: //i_d_PC8;
                case 0xCC: //i_d_PC8;
                case 0xEC: //i_d_PC8;
                    m = _mem.Read(Pc);
                    Pc = (Pc + 1) & 0xFFFF;
                    M = (Pc + SignedChar(m)) & 0xFFFF;
                    _cl++;
                    return M;
                case 0x8D: //i_d_PC16;
                case 0xAD: //i_d_PC16;
                case 0xCD: //i_d_PC16;
                case 0xED: //i_d_PC16;
                    M = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc = (Pc + 2) & 0xFFFF;
                    M = (Pc + Signed16Bits(M)) & 0xFFFF;
                    _cl += 5;
                    return M;
                case 0x8E: return 0; //i_undoc;	/* empty */
                case 0x8F: return 0; //i_undoc;	/* empty */
                case 0x90: return 0; //i_undoc;	/* empty */
                case 0x91: //i_i_P2_X;
                    M = (_mem.Read(_x) << 8) | _mem.Read(_x + 1);
                    _x = (_x + 2) & 0xFFFF;
                    _cl += 6;
                    return M;
                case 0x92: return 0; //i_undoc;	/* empty */
                case 0x93: //i_i_M2_X;
                    _x = (_x - 2) & 0xFFFF;
                    M = (_mem.Read(_x) << 8) | _mem.Read(_x + 1);
                    _cl += 6;
                    return M;
                case 0x94: //i_i_0_X;
                    M = (_mem.Read(_x) << 8) | _mem.Read(_x + 1);
                    _cl += 3;
                    return M;
                case 0x95: //i_i_B_X;
                    M = (_x + SignedChar(_b)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 4;
                    return M;
                case 0x96: //i_i_A_X;
                    M = (_x + SignedChar(_a)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 4;
                    return M;
                case 0x97: return 0; //i_undoc;	/* empty */
                case 0x98: //i_i_8_X;
                    m2 = _mem.Read(Pc);
                    Pc = (Pc + 1) & 0xFFFF;
                    M = (_x + SignedChar(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 4;
                    return M;
                case 0x99: //i_i_16_X;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc = (Pc + 2) & 0xFFFF;
                    M = (_x + Signed16Bits(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 7;
                    return M;
                case 0x9A: return 0; //i_undoc;	/* empty */
                case 0x9B: //i_i_D_X;
                    M = (_x + Signed16Bits((_a << 8) | _b)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 7;
                    return M;
                case 0x9C: //i_i_PC8;
                case 0xBC: //i_i_PC8;
                case 0xDC: //i_i_PC8;
                case 0xFC: //i_i_PC8;
                    m2 = _mem.Read(Pc);
                    Pc = (Pc + 1) & 0xFFFF;
                    M = (Pc + SignedChar(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 4;
                    return M;
                case 0x9D: //i_i_PC16;
                case 0xBD: //i_i_PC16;
                case 0xDD: //i_i_PC16;
                case 0xFD: //i_i_PC16;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc = (Pc + 2) & 0xFFFF;
                    M = (Pc + Signed16Bits(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 8;
                    return M;
                case 0x9E: return 0; //i_undoc;	/* empty */
                case 0x9F: //i_i_e16;
                case 0xBF: //i_i_e16;
                case 0xDF: //i_i_e16;
                case 0xFF: //i_i_e16;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc = (Pc + 2) & 0xFFFF;
                    M = (_mem.Read(m2) << 8) | _mem.Read(m2 + 1);
                    _cl += 5;
                    return M;
                // Y
                case 0xA0: //i_d_P1_Y;
                    M = _y;
                    _y = (_y + 1) & 0xFFFF;
                    _cl += 2;
                    return M;
                case 0xA1: //i_d_P2_Y;
                    M = _y;
                    _y = (_y + 2) & 0xFFFF;
                    _cl += 3;
                    return M;
                case 0xA2: //i_d_M1_Y;
                    _y = (_y - 1) & 0xFFFF;
                    M = _y;
                    _cl += 2;
                    return M;
                case 0xA3: //i_d_M2_Y;
                    _y = (_y - 2) & 0xFFFF;
                    M = _y;
                    _cl += 3;
                    return M;
                case 0xA4: //i_d_Y;
                    M = _y;
                    return M;
                case 0xA5: //i_d_B_Y;
                    M = (_y + SignedChar(_b)) & 0xFFFF;
                    _cl += 1;
                    return M;
                case 0xA6: //i_d_A_Y;
                    M = (_y + SignedChar(_a)) & 0xFFFF;
                    _cl += 1;
                    return M;
                case 0xA7: return 0; //i_undoc;	/* empty */
                case 0xA8: //i_d_8_Y;
                    m2 = _mem.Read(Pc);
                    Pc++;
                    M = (_y + SignedChar(m2)) & 0xFFFF;
                    _cl += 1;
                    return M;
                case 0xA9: //i_d_16_Y;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc += 2;
                    M = (_y + Signed16Bits(m2)) & 0xFFFF;
                    _cl += 4;
                    return M;
                case 0xAA: return 0; //i_undoc;	/* empty */
                case 0xAB: //i_d_D_Y;
                    M = (_y + Signed16Bits((_a << 8) | _b)) & 0xFFFF;
                    _cl += 4;
                    return M;
                case 0xAE: return 0; //i_undoc;	/* empty */
                case 0xAF: return 0; //i_undoc;	/* empty */
                case 0xB0: return 0; //i_undoc;	/* empty */
                case 0xB1: //i_i_P2_Y;
                    M = (_mem.Read(_y) << 8) | _mem.Read(_y + 1);
                    _y = (_y + 2) & 0xFFFF;
                    _cl += 6;
                    return M;
                case 0xB2: return 0; //i_undoc;	/* empty */
                case 0xB3: //i_i_M2_Y;
                    _y = (_y - 2) & 0xFFFF;
                    M = (_mem.Read(_y) << 8) | _mem.Read(_y + 1);
                    _cl += 6;
                    return M;
                case 0xB4: //i_i_0_Y;
                    M = (_mem.Read(_y) << 8) | _mem.Read(_y + 1);
                    _cl += 3;
                    return M;
                case 0xB5: //i_i_B_Y;
                    M = (_y + SignedChar(_b)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 4;
                    return M;
                case 0xB6: //i_i_A_Y;
                    M = (_y + SignedChar(_a)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 4;
                    return M;
                case 0xB7: return 0; //i_undoc;	/* empty */
                case 0xB8: //i_i_8_Y;
                    m2 = _mem.Read(Pc);
                    Pc = (Pc + 1) & 0xFFFF;
                    M = (_y + SignedChar(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 4;
                    return M;
                case 0xB9: //i_i_16_Y;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc = (Pc + 2) & 0xFFFF;
                    M = (_y + Signed16Bits(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 7;
                    return M;
                case 0xBA: return 0; //i_undoc;	/* empty */
                case 0xBB: //i_i_D_Y;
                    M = (_y + Signed16Bits((_a << 8) | _b)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 7;
                    return M;
                case 0xBE: return 0; //i_undoc;	/* empty */

                // U
                case 0xC0: //i_d_P1_U;
                    M = _u;
                    _u = (_u + 1) & 0xFFFF;
                    _cl += 2;
                    return M;
                case 0xC1: //i_d_P2_U;
                    M = _u;
                    _u = (_u + 2) & 0xFFFF;
                    _cl += 3;
                    return M;
                case 0xC2: //i_d_M1_U;
                    _u = (_u - 1) & 0xFFFF;
                    M = _u;
                    _cl += 2;
                    return M;
                case 0xC3: //i_d_M2_U;
                    _u = (_u - 2) & 0xFFFF;
                    M = _u;
                    _cl += 3;
                    return M;
                case 0xC4: //i_d_U;
                    M = _u;
                    return M;
                case 0xC5: //i_d_B_U;
                    M = (_u + SignedChar(_b)) & 0xFFFF;
                    _cl += 1;
                    return M;
                case 0xC6: //i_d_A_U;
                    M = (_u + SignedChar(_a)) & 0xFFFF;
                    _cl += 1;
                    return M;
                case 0xC7: return 0; //i_undoc;	/* empty */
                case 0xC8: //i_d_8_U;
                    m2 = _mem.Read(Pc);
                    Pc++;
                    M = (_u + SignedChar(m2)) & 0xFFFF;
                    _cl += 1;
                    return M;
                case 0xC9: //i_d_16_U;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc += 2;
                    M = (_u + Signed16Bits(m2)) & 0xFFFF;
                    _cl += 4;
                    return M;
                case 0xCA: return 0; //i_undoc;	/* empty */
                case 0xCB: //i_d_D_U;
                    M = (_u + Signed16Bits((_a << 8) | _b)) & 0xFFFF;
                    _cl += 4;
                    return M;
                case 0xCE: return 0; //i_undoc;	/* empty */
                case 0xCF: return 0; //i_undoc;	/* empty */
                case 0xD0: return 0; //i_undoc;	/* empty */
                case 0xD1: //i_i_P2_U;
                    M = (_mem.Read(_u) << 8) | _mem.Read(_u + 1);
                    _u = (_u + 2) & 0xFFFF;
                    _cl += 6;
                    return M;
                case 0xD2: return 0; //i_undoc;	/* empty */
                case 0xD3: //i_i_M2_U;
                    _u = (_u - 2) & 0xFFFF;
                    M = (_mem.Read(_u) << 8) | _mem.Read(_u + 1);
                    _cl += 6;
                    return M;
                case 0xD4: //i_i_0_U;
                    M = (_mem.Read(_u) << 8) | _mem.Read(_u + 1);
                    _cl += 3;
                    return M;
                case 0xD5: //i_i_B_U;
                    M = (_u + SignedChar(_b)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 4;
                    return M;
                case 0xD6: //i_i_A_U;
                    M = (_u + SignedChar(_a)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 4;
                    return M;
                case 0xD7: return 0; //i_undoc;	/* empty */
                case 0xD8: //i_i_8_U;
                    m2 = _mem.Read(Pc);
                    Pc = (Pc + 1) & 0xFFFF;
                    M = (_u + SignedChar(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 4;
                    return M;
                case 0xD9: //i_i_16_U;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc = (Pc + 2) & 0xFFFF;
                    M = (_u + Signed16Bits(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 7;
                    return M;
                case 0xDA: return 0; //i_undoc;	/* empty */
                case 0xDB: //i_i_D_U;
                    M = (_u + Signed16Bits((_a << 8) | _b)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 7;
                    return M;
                case 0xDE: return 0; //i_undoc;	/* empty */

                // S
                case 0xE0: //i_d_P1_S;
                    M = _s;
                    _s = (_s + 1) & 0xFFFF;
                    _cl += 2;
                    return M;
                case 0xE1: //i_d_P2_S;
                    M = _s;
                    _s = (_s + 2) & 0xFFFF;
                    _cl += 3;
                    return M;
                case 0xE2: //i_d_M1_S;
                    _s = (_s - 1) & 0xFFFF;
                    M = _s;
                    _cl += 2;
                    return M;
                case 0xE3: //i_d_M2_S;
                    _s = (_s - 2) & 0xFFFF;
                    M = _s;
                    _cl += 3;
                    return M;
                case 0xE4: //i_d_S;
                    M = _s;
                    return M;
                case 0xE5: //i_d_B_S;
                    M = (_s + SignedChar(_b)) & 0xFFFF;
                    _cl += 1;
                    return M;
                case 0xE6: //i_d_A_S;
                    M = (_s + SignedChar(_a)) & 0xFFFF;
                    _cl += 1;
                    return M;
                case 0xE7: return 0; //i_undoc;	/* empty */
                case 0xE8: //i_d_8_S;
                    m2 = _mem.Read(Pc);
                    Pc++;
                    M = (_s + SignedChar(m2)) & 0xFFFF;
                    _cl += 1;
                    return M;
                case 0xE9: //i_d_16_S;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc += 2;
                    M = (_s + Signed16Bits(m2)) & 0xFFFF;
                    _cl += 4;
                    return M;
                case 0xEA: return 0; //i_undoc;	/* empty */
                case 0xEB: //i_d_D_S;
                    M = (_s + Signed16Bits((_a << 8) | _b)) & 0xFFFF;
                    _cl += 4;
                    return M;
                case 0xEE: return 0; //i_undoc;	/* empty */
                case 0xEF: return 0; //i_undoc;	/* empty */
                case 0xF0: return 0; //i_undoc;	/* empty */
                case 0xF1: //i_i_P2_S;
                    M = (_mem.Read(_s) << 8) | _mem.Read(_s + 1);
                    _s = (_s + 2) & 0xFFFF;
                    _cl += 6;
                    return M;
                case 0xF2: return 0; //i_undoc;	/* empty */
                case 0xF3: //i_i_M2_S;
                    _s = (_s - 2) & 0xFFFF;
                    M = (_mem.Read(_s) << 8) | _mem.Read(_s + 1);
                    _cl += 6;
                    return M;
                case 0xF4: //i_i_0_S;
                    M = (_mem.Read(_s) << 8) | _mem.Read(_s + 1);
                    _cl += 3;
                    return M;
                case 0xF5: //i_i_B_S;
                    M = (_s + SignedChar(_b)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 4;
                    return M;
                case 0xF6: //i_i_A_S;
                    M = (_s + SignedChar(_a)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 4;
                    return M;
                case 0xF7: return 0; //i_undoc;	/* empty */
                case 0xF8: //i_i_8_S;
                    m2 = _mem.Read(Pc);
                    Pc = (Pc + 1) & 0xFFFF;
                    M = (_s + SignedChar(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 4;
                    return M;
                case 0xF9: //i_i_16_S;
                    m2 = (_mem.Read(Pc) << 8) | _mem.Read(Pc + 1);
                    Pc = (Pc + 2) & 0xFFFF;
                    M = (_s + Signed16Bits(m2)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 7;
                    return M;
                case 0xFA: return 0; //i_undoc;	/* empty */
                case 0xFB: //i_i_D_S;
                    M = (_s + Signed16Bits((_a << 8) | _b)) & 0xFFFF;
                    M = (_mem.Read(M) << 8) | _mem.Read(M + 1);
                    _cl += 7;
                    return M;
                case 0xFE: return 0; //i_undoc;	/* empty */
            }
            System.Console.Error.WriteLine("Indexed mode not implemented");
            return 0;
        }

// cc register recalculate from separate bits
        private int Getcc()
        {
            if ((_res & 0xff) == 0)
                _cc = ((((_h1 & 15) + (_h2 & 15)) & 16) << 1)
                      | ((_sign & 0x80) >> 4)
                      | 4
                      | ((~(_m1 ^ _m2) & (_m1 ^ _ovfl) & 0x80) >> 6)
                      | ((_res & 0x100) >> 8)
                      | _ccrest;
            else
                _cc = ((((_h1 & 15) + (_h2 & 15)) & 16) << 1)
                      | ((_sign & 0x80) >> 4)
                      | ((~(_m1 ^ _m2) & (_m1 ^ _ovfl) & 0x80) >> 6)
                      | ((_res & 0x100) >> 8)
                      | _ccrest;

            return _cc;
        }

// calculate CC fast bits from CC register
        private void Setcc(int i)
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
            return _cc;
        }

// LDx
        private int Ld8(int m, int c)
        {
            _sign = _mem.Read(m);
            _m1 = _ovfl;
            _res = (_res & 0x100) | _sign;
            _cl += c;
            return _sign;
        }

        private int Ld16(int m, int c)
        {
            int r;
            r = ((_mem.Read(m) << 8) | _mem.Read(m + 1)) & 0xFFFF;
            _m1 = _ovfl;
            _sign = r >> 8;
            _res = (_res & 0x100) | ((_sign | r) & 0xFF);
            _cl += c;
            return r;
        }

// STx
        private void St8(int r, int adr, int c)
        {
            _mem.Write(adr, r);
            _m1 = _ovfl;
            _sign = r;
            _res = (_res & 0x100) | _sign;
            _cl += c;
        }

        private void St16(int r, int adr, int c)
        {
            _mem.Write(adr, r >> 8);
            _mem.Write(adr + 1, r & 0xFF);
            _m1 = _ovfl;
            _sign = r >> 8;
            _res = (_res & 0x100) | ((_sign | r) & 0xFF);
            _cl += c;
        }

// LEA
        private int Lea()
        {
            int r = Indexe();
            _res = (_res & 0x100) | ((r | (r >> 8)) & 0xFF);
            _cl += 4;
            return r;
        }

// CLR
        private void Clr(int m, int c)
        {
            _mem.Write(m, 0);
            _m1 = ~_m2;
            _sign = _res = 0;
            _cl += c;
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
                    k = (_a << 8) | _b;
                    break;
                case 0x01:
                    k = _x;
                    break;
                case 0x02:
                    k = _y;
                    break;
                case 0x03:
                    k = _u;
                    break;
                case 0x04:
                    k = _s;
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
                    k = _a;
                    break;
                case 0x09:
                    k = _b;
                    break;
                case 0x0A:
                    k = Getcc();
                    break;
                case 0x0B:
                    k = _dp;
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
                    l = (_a << 8) | _b;
                    _a = (k >> 8) & 255;
                    _b = k & 255;
                    break;
                case 0x01:
                    l = _x;
                    _x = k;
                    break;
                case 0x02:
                    l = _y;
                    _y = k;
                    break;
                case 0x03:
                    l = _u;
                    _u = k;
                    break;
                case 0x04:
                    l = _s;
                    _s = k;
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
                    l = _a;
                    _a = k & 0xff;
                    break;
                case 0x09:
                    l = _b;
                    _b = k & 0xff;
                    break;
                case 0x0A:
                    l = Getcc();
                    Setcc(k);
                    break;
                case 0x0B:
                    l = _dp;
                    _dp = k & 0xff;
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
                    _a = (l >> 8) & 255;
                    _b = l & 255;
                    break;
                case 0x01:
                    _x = l;
                    break;
                case 0x02:
                    _y = l;
                    break;
                case 0x03:
                    _u = l;
                    break;
                case 0x04:
                    _s = l;
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
                    _a = l & 0xff;
                    break;
                case 0x09:
                    _b = l & 0xff;
                    break;
                case 0x0A:
                    Setcc(l);
                    break;
                case 0x0B:
                    _dp = l & 0xff;
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
            _cl += 8;
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
                    k = (_a << 8) | _b;
                    break;
                case 0x01:
                    k = _x;
                    break;
                case 0x02:
                    k = _y;
                    break;
                case 0x03:
                    k = _u;
                    break;
                case 0x04:
                    k = _s;
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
                    k = _a;
                    break;
                case 0x09:
                    k = _b;
                    break;
                case 0x0A:
                    k = Getcc();
                    break;
                case 0x0B:
                    k = _dp;
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
                    _a = (k >> 8) & 255;
                    _b = k & 255;
                    break;
                case 0x01:
                    _x = k;
                    break;
                case 0x02:
                    _y = k;
                    break;
                case 0x03:
                    _u = k;
                    break;
                case 0x04:
                    _s = k;
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
                    _a = k & 0xff;
                    break;
                case 0x09:
                    _b = k & 0xff;
                    break;
                case 0x0A:
                    Setcc(k);
                    break;
                case 0x0B:
                    _dp = k & 0xff;
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
                _s--;
                _mem.Write(_s, Pc & 0x00FF);
                _s--;
                _mem.Write(_s, Pc >> 8);
                _cl += 2;
            }
            if ((m & 0x40) != 0)
            {
                _s--;
                _mem.Write(_s, _u & 0x00FF);
                _s--;
                _mem.Write(_s, _u >> 8);
                _cl += 2;
            }
            if ((m & 0x20) != 0)
            {
                _s--;
                _mem.Write(_s, _y & 0x00FF);
                _s--;
                _mem.Write(_s, _y >> 8);
                _cl += 2;
            }
            if ((m & 0x10) != 0)
            {
                _s--;
                _mem.Write(_s, _x & 0x00FF);
                _s--;
                _mem.Write(_s, _x >> 8);
                _cl += 2;
            }
            if ((m & 0x08) != 0)
            {
                _s--;
                _mem.Write(_s, _dp);
                _cl++;
            }
            if ((m & 0x04) != 0)
            {
                _s--;
                _mem.Write(_s, _b);
                _cl++;
            }
            if ((m & 0x02) != 0)
            {
                _s--;
                _mem.Write(_s, _a);
                _cl++;
            }
            if ((m & 0x01) != 0)
            {
                _s--;
                Getcc();
                _mem.Write(_s, _cc);
                _cl++;
            }
            _cl += 5;
        }

        private void Pshu()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((m & 0x80) != 0)
            {
                _u--;
                _mem.Write(_u, Pc & 0x00FF);
                _u--;
                _mem.Write(_u, Pc >> 8);
                _cl += 2;
            }
            if ((m & 0x40) != 0)
            {
                _u--;
                _mem.Write(_u, _s & 0x00FF);
                _u--;
                _mem.Write(_u, _s >> 8);
                _cl += 2;
            }
            if ((m & 0x20) != 0)
            {
                _u--;
                _mem.Write(_u, _y & 0x00FF);
                _u--;
                _mem.Write(_u, _y >> 8);
                _cl += 2;
            }
            if ((m & 0x10) != 0)
            {
                _u--;
                _mem.Write(_u, _x & 0x00FF);
                _u--;
                _mem.Write(_u, _x >> 8);
                _cl += 2;
            }
            if ((m & 0x08) != 0)
            {
                _u--;
                _mem.Write(_u, _dp);
                _cl++;
            }
            if ((m & 0x04) != 0)
            {
                _u--;
                _mem.Write(_u, _b);
                _cl++;
            }
            if ((m & 0x02) != 0)
            {
                _u--;
                _mem.Write(_u, _a);
                _cl++;
            }
            if ((m & 0x01) != 0)
            {
                _u--;
                Getcc();
                _mem.Write(_u, _cc);
                _cl++;
            }
            _cl += 5;
        }

        private void Puls()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((m & 0x01) != 0)
            {
                _cc = _mem.Read(_s);
                Setcc(_cc);
                _s++;
                _cl++;
            }
            if ((m & 0x02) != 0)
            {
                _a = _mem.Read(_s);
                _s++;
                _cl++;
            }
            if ((m & 0x04) != 0)
            {
                _b = _mem.Read(_s);
                _s++;
                _cl++;
            }
            if ((m & 0x08) != 0)
            {
                _dp = _mem.Read(_s);
                _s++;
                _cl++;
            }
            if ((m & 0x10) != 0)
            {
                _x = (_mem.Read(_s) << 8) | _mem.Read(_s + 1);
                _s += 2;
                _cl += 2;
            }
            if ((m & 0x20) != 0)
            {
                _y = (_mem.Read(_s) << 8) | _mem.Read(_s + 1);
                _s += 2;
                _cl += 2;
            }
            if ((m & 0x40) != 0)
            {
                _u = (_mem.Read(_s) << 8) | _mem.Read(_s + 1);
                _s += 2;
                _cl += 2;
            }
            if ((m & 0x80) != 0)
            {
                Pc = (_mem.Read(_s) << 8) | _mem.Read(_s + 1);
                _s += 2;
                _cl += 2;
            }
            _cl += 5;
        }

        private void Pulu()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((m & 0x01) != 0)
            {
                _cc = _mem.Read(_u);
                Setcc(_cc);
                _u++;
                _cl++;
            }
            if ((m & 0x02) != 0)
            {
                _a = _mem.Read(_u);
                _u++;
                _cl++;
            }
            if ((m & 0x04) != 0)
            {
                _b = _mem.Read(_u);
                _u++;
                _cl++;
            }
            if ((m & 0x08) != 0)
            {
                _dp = _mem.Read(_u);
                _u++;
                _cl++;
            }
            if ((m & 0x10) != 0)
            {
                _x = (_mem.Read(_u) << 8) | _mem.Read(_u + 1);
                _u += 2;
                _cl += 2;
            }
            if ((m & 0x20) != 0)
            {
                _y = (_mem.Read(_u) << 8) | _mem.Read(_u + 1);
                _u += 2;
                _cl += 2;
            }
            if ((m & 0x40) != 0)
            {
                _s = (_mem.Read(_u) << 8) | _mem.Read(_u + 1);
                _u += 2;
                _cl += 2;
            }
            if ((m & 0x80) != 0)
            {
                Pc = (_mem.Read(_u) << 8) | _mem.Read(_u + 1);
                _u += 2;
                _cl += 2;
            }
            _cl += 5;
        }

        private void Inca()
        {
            _m1 = _a;
            _m2 = 0;
            _a = (_a + 1) & 0xFF;
            _ovfl = _sign = _a;
            _res = (_res & 0x100) | _sign;
            _cl += 2;
        }

        private void Incb()
        {
            _m1 = _b;
            _m2 = 0;
            _b = (_b + 1) & 0xFF;
            _ovfl = _sign = _b;
            _res = (_res & 0x100) | _sign;
            _cl += 2;
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
            _cl += c;
        }

// DEC
        private void Deca()
        {
            _m1 = _a;
            _m2 = 0x80;
            _a = (_a - 1) & 0xFF;
            _ovfl = _sign = _a;
            _res = (_res & 0x100) | _sign;
            _cl += 2;
        }

        private void Decb()
        {
            _m1 = _b;
            _m2 = 0x80;
            _b = (_b - 1) & 0xFF;
            _ovfl = _sign = _b;
            _res = (_res & 0x100) | _sign;
            _cl += 2;
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
            _cl += c;
        }

        private void Bit(int r, int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _ovfl;
            _sign = r & val;
            _res = (_res & 0x100) | _sign;
            _cl += c;
        }

        private void Cmp8(int r, int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = r;
            _m2 = -val;
            _ovfl = _res = _sign = r - val;
            _cl += c;
        }

        private void Cmp16(int r, int adr, int c)
        {
            int val;
            val = (_mem.Read(adr) << 8) | _mem.Read(adr + 1);
            _m1 = r >> 8;
            _m2 = -val >> 8;
            _ovfl = _res = _sign = ((r - val) >> 8) & 0xFFFFFF;
            _res |= (r - val) & 0xFF;
            _cl += c;
        }

// TST
        private void TstAi()
        {
            _m1 = _ovfl;
            _sign = _a;
            _res = (_res & 0x100) | _sign;
            _cl += 2;
        }

        private void TstBi()
        {
            _m1 = _ovfl;
            _sign = _b;
            _res = (_res & 0x100) | _sign;
            _cl += 2;
        }

        private void Tst(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = ~_m2;
            _sign = val;
            _res = (_res & 0x100) | _sign;
            _cl += c;
        }

        private void Anda(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _ovfl;
            _a &= val;
            _sign = _a;
            _res = (_res & 0x100) | _sign;
            _cl += c;
        }

        private void Andb(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _ovfl;
            _b &= val;
            _sign = _b;
            _res = (_res & 0x100) | _sign;
            _cl += c;
        }

        private void Andcc(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
//	getcc();
            _cc &= val;
            Setcc(_cc);
            _cl += c;
        }

        private void Ora(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _ovfl;
            _a |= val;
            _sign = _a;
            _res = (_res & 0x100) | _sign;
            _cl += c;
        }

        private void Orb(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _ovfl;
            _b |= val;
            _sign = _b;
            _res = (_res & 0x100) | _sign;
            _cl += c;
        }

        private void Orcc(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            Getcc();
            _cc |= val;
            Setcc(_cc);
            _cl += c;
        }

        private void Eora(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _ovfl;
            _a ^= val;
            _sign = _a;
            _res = (_res & 0x100) | _sign;
            _cl += c;
        }

        private void Eorb(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _ovfl;
            _b ^= val;
            _sign = _b;
            _res = (_res & 0x100) | _sign;
            _cl += c;
        }

        private void Coma()
        {
            _m1 = _ovfl;
            _a = ~_a & 0xFF;
            _sign = _a;
            _res = _sign | 0x100;
            _cl += 2;
        }

        private void Comb()
        {
            _m1 = _ovfl;
            _b = ~_b & 0xFF;
            _sign = _b;
            _res = _sign | 0x100;
            _cl += 2;
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
            _cl += c;
        }

        private void Nega()
        {
            _m1 = _a;
            _m2 = -_a;
            _a = -_a;
            _ovfl = _res = _sign = _a;
            _a &= 0xFF;
            _cl += 2;
        }

        private void Negb()
        {
            _m1 = _b;
            _m2 = -_b;
            _b = -_b;
            _ovfl = _res = _sign = _b;
            _b &= 0xFF;
            _cl += 2;
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
            _cl += c;
        }

        private void Abx()
        {
            _x = (_x + _b) & 0xFFFF;
            _cl += 3;
        }

        private void Adda(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _h1 = _a;
            _m2 = _h2 = val;
            _a += val;
            _ovfl = _res = _sign = _a;
            _a &= 0xFF;
            _cl += c;
        }

        private void Addb(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _h1 = _b;
            _m2 = _h2 = val;
            _b += val;
            _ovfl = _res = _sign = _b;
            _b &= 0xFF;
            _cl += c;
        }

        private void Addd(int adr, int c)
        {
            int val;
            val = (_mem.Read(adr) << 8) | _mem.Read(adr + 1);
            _m1 = _a;
            _m2 = val >> 8;
            _d = (_a << 8) + _b + val;
            _a = _d >> 8;
            _b = _d & 0xFF;
            _ovfl = _res = _sign = _a;
            _res |= _b;
            _a &= 0xFF;
            _cl += c;
        }

        private void Adca(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _h1 = _a;
            _m2 = val;
            _h2 = val + ((_res & 0x100) >> 8);
            _a += _h2;
            _ovfl = _res = _sign = _a;
            _a &= 0xFF;
            _cl += c;
        }

        private void Adcb(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _h1 = _b;
            _m2 = val;
            _h2 = val + ((_res & 0x100) >> 8);
            _b += _h2;
            _ovfl = _res = _sign = _b;
            _b &= 0xFF;
            _cl += c;
        }

        private void Mul()
        {
            int k;
            k = _a * _b;
            _a = (k >> 8) & 0xFF;
            _b = k & 0xFF;
            _res = ((_b & 0x80) << 1) | ((k | (k >> 8)) & 0xFF);
            _cl += 11;
        }

        private void Sbca(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _a;
            _m2 = -val;
            _a -= val + ((_res & 0x100) >> 8);
            _ovfl = _res = _sign = _a;
            _a &= 0xFF;
            _cl += c;
        }

        private void Sbcb(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _b;
            _m2 = -val;
            _b -= val + ((_res & 0x100) >> 8);
            _ovfl = _res = _sign = _b;
            _b &= 0xFF;
            _cl += c;
        }

        private void Suba(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _a;
            _m2 = -val;
            _a -= val;
            _ovfl = _res = _sign = _a;
            _a &= 0xFF;
            _cl += c;
        }

        private void Subb(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _b;
            _m2 = -val;
            _b -= val;
            _ovfl = _res = _sign = _b;
            _b &= 0xFF;
            _cl += c;
        }

        private void Subd(int adr, int c)
        {
            int val;
            val = (_mem.Read(adr) << 8) | _mem.Read(adr + 1);
            _m1 = _a;
            _m2 = -val >> 8;
            _d = (_a << 8) + _b - val;
            _a = _d >> 8;
            _b = _d & 0xFF;
            _ovfl = _res = _sign = _a;
            _res |= _b;
            _a &= 0xFF;
            _cl += c;
        }

        private void Sex()
        {
            if ((_b & 0x80) == 0x80) _a = 0xFF;
            else _a = 0;
            _sign = _b;
            _res = (_res & 0x100) | _sign;
            _cl += 2;
        }

        private void Asla()
        {
            _m1 = _m2 = _a;
            _a <<= 1;
            _ovfl = _sign = _res = _a;
            _a &= 0xFF;
            _cl += 2;
        }

        private void Aslb()
        {
            _m1 = _m2 = _b;
            _b <<= 1;
            _ovfl = _sign = _res = _b;
            _b &= 0xFF;
            _cl += 2;
        }

        private void Asl(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _m2 = val;
            val <<= 1;
            _mem.Write(adr, val);
            _ovfl = _sign = _res = val;
            _cl += c;
        }

        private void Asra()
        {
            _res = (_a & 1) << 8;
            _a = (_a >> 1) | (_a & 0x80);
            _sign = _a;
            _res |= _sign;
            _cl += 2;
        }

        private void Asrb()
        {
            _res = (_b & 1) << 8;
            _b = (_b >> 1) | (_b & 0x80);
            _sign = _b;
            _res |= _sign;
            _cl += 2;
        }

        private void Asr(int adr, int c)
        {
            var val = _mem.Read(adr);
            _res = (val & 1) << 8;
            val = (val >> 1) | (val & 0x80);
            _mem.Write(adr, val);
            _sign = val;
            _res |= _sign;
            _cl += c;
        }

        private void Lsra()
        {
            _res = (_a & 1) << 8;
            _a = _a >> 1;
            _sign = 0;
            _res |= _a;
            _cl += 2;
        }

        private void Lsrb()
        {
            _res = (_b & 1) << 8;
            _b = _b >> 1;
            _sign = 0;
            _res |= _b;
            _cl += 2;
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
            _cl += c;
        }

        private void Rola()
        {
            _m1 = _m2 = _a;
            _a = (_a << 1) | ((_res & 0x100) >> 8);
            _ovfl = _sign = _res = _a;
            _a &= 0xFF;
            _cl += 2;
        }

        private void Rolb()
        {
            _m1 = _m2 = _b;
            _b = (_b << 1) | ((_res & 0x100) >> 8);
            _ovfl = _sign = _res = _b;
            _b &= 0xFF;
            _cl += 2;
        }

        private void Rol(int adr, int c)
        {
            int val;
            val = _mem.Read(adr);
            _m1 = _m2 = val;
            val = (val << 1) | ((_res & 0x100) >> 8);
            _mem.Write(adr, val);
            _ovfl = _sign = _res = val;
            _cl += c;
        }

        private void Rora()
        {
            int i;
            i = _a;
            _a = (_a | (_res & 0x100)) >> 1;
            _sign = _a;
            _res = ((i & 1) << 8) | _sign;
            _cl += 2;
        }

        private void Rorb()
        {
            int i;
            i = _b;
            _b = (_b | (_res & 0x100)) >> 1;
            _sign = _b;
            _res = ((i & 1) << 8) | _sign;
            _cl += 2;
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
            _cl += c;
        }

        private void Bra()
        {
            int m;
            m = _mem.Read(Pc++);
            Pc += SignedChar(m);
            _cl += 3;
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
            _cl += 5;
        }

        private void JmPd()
        {
            int m;
            m = _mem.Read(Pc++);
            Pc = (_dp << 8) | m;
            _cl += 3;
        }

        private void JmPe()
        {
            int adr;
            adr = Etend();
            Pc = adr;
            _cl += 4;
        }

        private void JmPx()
        {
            int adr;
            adr = Indexe();
            Pc = adr;
            _cl += 3;
        }

        private void Bsr()
        {
            int m;
            m = _mem.Read(Pc++);
            _s--;
            _mem.Write(_s, Pc & 0x00FF);
            _s--;
            _mem.Write(_s, Pc >> 8);
            Pc += SignedChar(m);
            _cl += 7;
        }

        private void Lbsr()
        {
            int off;
            int m;
            m = _mem.Read(Pc++);
            off = m << 8;
            m = _mem.Read(Pc++);
            off |= m;
            _s--;
            _mem.Write(_s, Pc & 0x00FF);
            _s--;
            _mem.Write(_s, Pc >> 8);
            Pc = (Pc + off) & 0xFFFF;
            _cl += 9;
        }

        private void JsRd()
        {
            int m;
            m = _mem.Read(Pc++);
            _s--;
            _mem.Write(_s, Pc & 0x00FF);
            _s--;
            _mem.Write(_s, Pc >> 8);
            Pc = (_dp << 8) | m;
            _cl += 7;
        }

        private void JsRe()
        {
            int adr;
            adr = Etend();
            _s--;
            _mem.Write(_s, Pc & 0x00FF);
            _s--;
            _mem.Write(_s, Pc >> 8);
            Pc = adr;
            _cl += 8;
        }

        private void JsRx()
        {
            int adr;
            adr = Indexe();
            _s--;
            _mem.Write(_s, Pc & 0x00FF);
            _s--;
            _mem.Write(_s, Pc >> 8);
            Pc = adr;
            _cl += 7;
        }

        private void Brn()
        {
            _mem.Read(Pc++);
            _cl += 3;
        }

        private void Lbrn()
        {
            _mem.Read(Pc++);
            _mem.Read(Pc++);
            _cl += 5;
        }

        private void Nop()
        {
            _cl += 2;
        }

        private void Rts()
        {
            Pc = (_mem.Read(_s) << 8) | _mem.Read(_s + 1);
            _s += 2;
            _cl += 5;
        }

/* Branchements conditionnels */

        private void Bcc()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_res & 0x100) != 0x100) Pc += SignedChar(m);
            _cl += 3;
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
                _cl += 6;
            }
            else _cl += 5;
        }

        private void Bcs()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_res & 0x100) == 0x100) Pc += SignedChar(m);
            _cl += 3;
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
                _cl += 6;
            }
            else _cl += 5;
        }

        private void Beq()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_res & 0xff) == 0x00) Pc += SignedChar(m);
            _cl += 3;
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
                _cl += 6;
            }
            else _cl += 6;
        }

        private void Bne()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_res & 0xff) != 0) Pc += SignedChar(m);
            _cl += 3;
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
                _cl += 6;
            }
            else _cl += 5;
        }

        private void Bge()
        {
            int m;
            m = _mem.Read(Pc++);
            if (((_sign ^ (~(_m1 ^ _m2) & (_m1 ^ _ovfl))) & 0x80) == 0) Pc += SignedChar(m);
            _cl += 3;
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
                _cl += 6;
            }
            else _cl += 5;
        }

        private void Ble()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_res & 0xff) == 0
                || ((_sign ^ (~(_m1 ^ _m2) & (_m1 ^ _ovfl))) & 0x80) != 0) Pc += SignedChar(m);
            _cl += 3;
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
                _cl += 6;
            }
            else _cl += 5;
        }

        private void Bls()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_res & 0x100) != 0 || (_res & 0xff) == 0) Pc += SignedChar(m);
            _cl += 3;
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
                _cl += 6;
            }
            else _cl += 5;
        }

        private void Bgt()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_res & 0xff) != 0
                && ((_sign ^ (~(_m1 ^ _m2) & (_m1 ^ _ovfl))) & 0x80) == 0) Pc += SignedChar(m);
            _cl += 3;
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
                _cl += 6;
            }
            else _cl += 5;
        }

        private void Blt()
        {
            int m;
            m = _mem.Read(Pc++);
            if (((_sign ^ (~(_m1 ^ _m2) & (_m1 ^ _ovfl))) & 0x80) != 0) Pc += SignedChar(m);
            _cl += 3;
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
                _cl += 6;
            }
            else _cl += 5;
        }

        private void Bhi()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_res & 0x100) == 0 && (_res & 0xff) != 0) Pc += SignedChar(m);
            _cl += 3;
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
                _cl += 6;
            }
            _cl += 5;
        }

        private void Bmi()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_sign & 0x80) != 0) Pc += SignedChar(m);
            _cl += 3;
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
                _cl += 6;
            }
            else _cl += 5;
        }

        private void Bpl()
        {
            int m;
            m = _mem.Read(Pc++);
            if ((_sign & 0x80) == 0) Pc += SignedChar(m);
            _cl += 3;
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
                _cl += 6;
            }
            else _cl += 5;
        }

        private void Bvs()
        {
            int m;
            m = _mem.Read(Pc++);
            if (((_m1 ^ _m2) & 0x80) == 0 && ((_m1 ^ _ovfl) & 0x80) != 0) Pc += SignedChar(m);
            _cl += 3;
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
                _cl += 6;
            }
            else _cl += 5;
        }

        private void Bvc()
        {
            int m;
            m = _mem.Read(Pc++);
            if (((_m1 ^ _m2) & 0x80) != 0 || ((_m1 ^ _ovfl) & 0x80) == 0) Pc += SignedChar(m);
            _cl += 3;
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
                _cl += 6;
            }
            else _cl += 5;
        }

        private void Swi()
        {
            Getcc();
            _cc |= 0x80; /* bit E � 1 */
            Setcc(_cc);
            _s--;
            _mem.Write(_s, Pc & 0x00FF);
            _s--;
            _mem.Write(_s, Pc >> 8);
            _s--;
            _mem.Write(_s, _u & 0x00FF);
            _s--;
            _mem.Write(_s, _u >> 8);
            _s--;
            _mem.Write(_s, _y & 0x00FF);
            _s--;
            _mem.Write(_s, _y >> 8);
            _s--;
            _mem.Write(_s, _x & 0x00FF);
            _s--;
            _mem.Write(_s, _x >> 8);
            _s--;
            _mem.Write(_s, _dp);
            _s--;
            _mem.Write(_s, _b);
            _s--;
            _mem.Write(_s, _a);
            _s--;
            _mem.Write(_s, _cc);

            Pc = (_mem.Read(0xFFFA) << 8) | _mem.Read(0xFFFB);
            _cl += 19;
        }

        private void Rti()
        {
            _cc = _mem.Read(_s);
            Setcc(_cc);
            _s++;
            if ((_cc & 0x80) == 0x80)
            {
                _a = _mem.Read(_s);
                _s++;
                _b = _mem.Read(_s);
                _s++;
                _dp = _mem.Read(_s);
                _s++;
                _x = (_mem.Read(_s) << 8) | _mem.Read(_s + 1);
                _s += 2;
                _y = (_mem.Read(_s) << 8) | _mem.Read(_s + 1);
                _s += 2;
                _u = (_mem.Read(_s) << 8) | _mem.Read(_s + 1);
                _s += 2;
                _cl += 15;
            }
            else _cl += 6;

            Pc = (_mem.Read(_s) << 8) | _mem.Read(_s + 1);
            _s += 2;
        }

        public void Irq()
        {
            /* mise � 1 du bit E sur le CC */
            Getcc();
            _cc |= 0x80;
            Setcc(_cc);
            _s--;
            _mem.Write(_s, Pc & 0x00FF);
            _s--;
            _mem.Write(_s, Pc >> 8);
            _s--;
            _mem.Write(_s, _u & 0x00FF);
            _s--;
            _mem.Write(_s, _u >> 8);
            _s--;
            _mem.Write(_s, _y & 0x00FF);
            _s--;
            _mem.Write(_s, _y >> 8);
            _s--;
            _mem.Write(_s, _x & 0x00FF);
            _s--;
            _mem.Write(_s, _x >> 8);
            _s--;
            _mem.Write(_s, _dp);
            _s--;
            _mem.Write(_s, _b);
            _s--;
            _mem.Write(_s, _a);
            _s--;
            _mem.Write(_s, _cc);
            Pc = (_mem.Read(0xFFF8) << 8) | _mem.Read(0xFFF9);
            _cc |= 0x10;
            Setcc(_cc);
            _cl += 19;
        }

        private void Daa()
        {
            int i = _a + (_res & 0x100);
            if ((_a & 15) > 9 || (_h1 & 15) + (_h2 & 15) > 15) i += 6;
            if (i > 0x99) i += 0x60;
            _res = _sign = i;
            _a = i & 255;
            _cl += 2;
        }

        private void Cwai()
        {
            Getcc();
            _cc &= _mem.Read(Pc);
            Setcc(_cc);
            Pc++;
            _cl += 20;
        }

        public int FetchUntil(int clock)
        {
            while (_cl < clock) Fetch();
            _cl -= clock;
            return _cl;
        }

        public void Fetch()
        {
            int opcode = _mem.Read(Pc++);

            // 	Sound emulation process
            SoundBuffer[_soundAddr] = (byte) _mem.SoundMem;
            _soundAddr = (_soundAddr + 1) % SoundSize;
            if (_soundAddr == 0)
                _play.PlaySound(SoundBuffer);

            switch (opcode)
            {
                // the mystery undocumented opcode
                case 0x01:
                    Pc++;
                    _cl += 2;
                    break;

                // PER (instruction d'emulation de périphérique)
                case 0x02:
                    _mem.Periph(Pc, _s, _a);
                    break;

                // LDA
                case 0x86:
                    _a = Ld8(Immed8(), 2);
                    break;
                case 0x96:
                    _a = Ld8(Direc(), 4);
                    break;
                case 0xB6:
                    _a = Ld8(Etend(), 5);
                    break;
                case 0xA6:
                    _a = Ld8(Indexe(), 4);
                    break;

                // LDB
                case 0xC6:
                    _b = Ld8(Immed8(), 2);
                    break;
                case 0xD6:
                    _b = Ld8(Direc(), 4);
                    break;
                case 0xF6:
                    _b = Ld8(Etend(), 5);
                    break;
                case 0xE6:
                    _b = Ld8(Indexe(), 4);
                    break;

                // LDD
                case 0xCC:
                    _d = Ld16(Immed16(), 3);
                    Calcab();
                    break;
                case 0xDC:
                    _d = Ld16(Direc(), 5);
                    Calcab();
                    break;
                case 0xFC:
                    _d = Ld16(Etend(), 6);
                    Calcab();
                    break;
                case 0xEC:
                    _d = Ld16(Indexe(), 5);
                    Calcab();
                    break;

                // LDU
                case 0xCE:
                    _u = Ld16(Immed16(), 3);
                    break;
                case 0xDE:
                    _u = Ld16(Direc(), 5);
                    break;
                case 0xFE:
                    _u = Ld16(Etend(), 6);
                    break;
                case 0xEE:
                    _u = Ld16(Indexe(), 5);
                    break;


                // LDX
                case 0x8E:
                    _x = Ld16(Immed16(), 3);
                    break;
                case 0x9E:
                    _x = Ld16(Direc(), 5);
                    break;
                case 0xBE:
                    _x = Ld16(Etend(), 6);
                    break;
                case 0xAE:
                    _x = Ld16(Indexe(), 5);
                    break;

                // STA 
                case 0x97:
                    St8(_a, Direc(), 4);
                    break;
                case 0xB7:
                    St8(_a, Etend(), 5);
                    break;
                case 0xA7:
                    St8(_a, Indexe(), 4);
                    break;

// STB
                case 0xD7:
                    St8(_b, Direc(), 4);
                    break;
                case 0xF7:
                    St8(_b, Etend(), 5);
                    break;
                case 0xE7:
                    St8(_b, Indexe(), 4);
                    break;

// STD
                case 0xDD:
                    Calcd();
                    St16(_d, Direc(), 5);
                    break;
                case 0xFD:
                    Calcd();
                    St16(_d, Etend(), 6);
                    break;
                case 0xED:
                    Calcd();
                    St16(_d, Indexe(), 6);
                    break;

// STU
                case 0xDF:
                    St16(_u, Direc(), 5);
                    break;
                case 0xFF:
                    St16(_u, Etend(), 6);
                    break;
                case 0xEF:
                    St16(_u, Indexe(), 5);
                    break;

// STX
                case 0x9F:
                    St16(_x, Direc(), 5);
                    break;
                case 0xBF:
                    St16(_x, Etend(), 6);
                    break;
                case 0xAF:
                    St16(_x, Indexe(), 5);
                    break;

// LEAS
                case 0x32:
                    _s = Indexe();
                    break;
// LEAU
                case 0x33:
                    _u = Indexe();
                    break;
// LEAX
                case 0x30:
                    _x = Lea();
                    break;
// LEAY
                case 0x31:
                    _y = Lea();
                    break;

// CLRA
                case 0x4F:
                    _a = 0;
                    _m1 = _ovfl;
                    _sign = _res = 0;
                    _cl += 2;
                    break;
// CLRB
                case 0x5F:
                    _b = 0;
                    _m1 = _ovfl;
                    _sign = _res = 0;
                    _cl += 2;
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
                    Bit(_a, Immed8(), 2);
                    break;
                case 0x95:
                    Bit(_a, Direc(), 4);
                    break;
                case 0xB5:
                    Bit(_a, Etend(), 5);
                    break;
                case 0xA5:
                    Bit(_a, Indexe(), 4);
                    break;
                case 0xC5:
                    Bit(_b, Immed8(), 2);
                    break;
                case 0xD5:
                    Bit(_b, Direc(), 4);
                    break;
                case 0xF5:
                    Bit(_b, Etend(), 5);
                    break;
                case 0xE5:
                    Bit(_b, Indexe(), 4);
                    break;

// CMP
                case 0x81:
                    Cmp8(_a, Immed8(), 2);
                    break;
                case 0x91:
                    Cmp8(_a, Direc(), 4);
                    break;
                case 0xB1:
                    Cmp8(_a, Etend(), 5);
                    break;
                case 0xA1:
                    Cmp8(_a, Indexe(), 4);
                    break;
                case 0xC1:
                    Cmp8(_b, Immed8(), 2);
                    break;
                case 0xD1:
                    Cmp8(_b, Direc(), 4);
                    break;
                case 0xF1:
                    Cmp8(_b, Etend(), 5);
                    break;
                case 0xE1:
                    Cmp8(_b, Indexe(), 4);
                    break;
                case 0x8C:
                    Cmp16(_x, Immed16(), 5);
                    break;
                case 0x9C:
                    Cmp16(_x, Direc(), 7);
                    break;
                case 0xBC:
                    Cmp16(_x, Etend(), 8);
                    break;
                case 0xAC:
                    Cmp16(_x, Indexe(), 7);
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

                    int opcode0X10 = _mem.Read(Pc++);

                    switch (opcode0X10)
                    {
// LDS
                        case 0xCE:
                            _s = Ld16(Immed16(), 3);
                            break;
                        case 0xDE:
                            _s = Ld16(Direc(), 5);
                            break;
                        case 0xFE:
                            _s = Ld16(Etend(), 6);
                            break;
                        case 0xEE:
                            _s = Ld16(Indexe(), 5);
                            break;

// LDY
                        case 0x8E:
                            _y = Ld16(Immed16(), 3);
                            break;
                        case 0x9E:
                            _y = Ld16(Direc(), 5);
                            break;
                        case 0xBE:
                            _y = Ld16(Etend(), 6);
                            break;
                        case 0xAE:
                            _y = Ld16(Indexe(), 5);
                            break;

// STS
                        case 0xDF:
                            St16(_s, Direc(), 5);
                            break;
                        case 0xFF:
                            St16(_s, Etend(), 6);
                            break;
                        case 0xEF:
                            St16(_s, Indexe(), 5);
                            break;

// STY
                        case 0x9F:
                            St16(_y, Direc(), 5);
                            break;
                        case 0xBF:
                            St16(_y, Etend(), 6);
                            break;
                        case 0xAF:
                            St16(_y, Indexe(), 5);
                            break;

// CMP
                        case 0x83:
                            Calcd();
                            Cmp16(_d, Immed16(), 5);
                            break;
                        case 0x93:
                            Calcd();
                            Cmp16(_d, Direc(), 7);
                            break;
                        case 0xB3:
                            Calcd();
                            Cmp16(_d, Etend(), 8);
                            break;
                        case 0xA3:
                            Calcd();
                            Cmp16(_d, Indexe(), 7);
                            break;
                        case 0x8C:
                            Cmp16(_y, Immed16(), 5);
                            break;
                        case 0x9C:
                            Cmp16(_y, Direc(), 7);
                            break;
                        case 0xBC:
                            Cmp16(_y, Etend(), 8);
                            break;
                        case 0xAC:
                            Cmp16(_y, Indexe(), 7);
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
                            System.Console.Error.WriteLine("opcode 10 {0:X2} not implemented", opcode0X10);
                            System.Console.Error.WriteLine(PrintState());
                            break;
                    } // of case opcode0x10
                    break;
                case 0x11:

                    int opcode0X11 = _mem.Read(Pc++);

                    switch (opcode0X11)
                    {
                        // CMP
                        case 0x8C:
                            Cmp16(_s, Immed16(), 5);
                            break;
                        case 0x9C:
                            Cmp16(_s, Direc(), 7);
                            break;
                        case 0xBC:
                            Cmp16(_s, Etend(), 8);
                            break;
                        case 0xAC:
                            Cmp16(_s, Indexe(), 7);
                            break;
                        case 0x83:
                            Cmp16(_u, Immed16(), 5);
                            break;
                        case 0x93:
                            Cmp16(_u, Direc(), 7);
                            break;
                        case 0xB3:
                            Cmp16(_u, Etend(), 8);
                            break;
                        case 0xA3:
                            Cmp16(_u, Indexe(), 7);
                            break;

                        default:
                            System.Console.Error.WriteLine("opcode 11 {0:X2} not implemented",opcode0X11);
                            System.Console.Error.WriteLine(PrintState());
                            break;
                    } // of case opcode 0x11 
                    break;

                default:
                    System.Console.Error.WriteLine("opcode {0:X2} not implemented", opcode);
                    System.Console.Error.WriteLine(PrintState());
                    break;
            } // of case  opcode
        } // of method fetch()


        // DISASSEMBLE/DEBUG PART
        public string PrintState()
        {
            _cc = Getcc();
            var s = new StringBuilder();
            s.AppendFormat(" A=  {0:X2}  B=  {1:X2}", _a, _b).AppendLine();
			s.AppendFormat(" X={0:X4}  Y={1:X4}", _x, _y).AppendLine();
			s.AppendFormat("PC={0:X4} DP={1:X4}", Pc, _dp).AppendLine();
            s.AppendFormat(" U={0:X4}  S={1:X4}", _u, _s).AppendLine();
            s.AppendFormat(" CC=  {0:X2}", _cc);
            return s.ToString();
        }
    } // of class M6809
}