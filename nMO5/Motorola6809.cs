using System.Text;

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
                            System.Console.Error.WriteLine("opcode 10 " + Hex(opcode0X10, 2) + " not implemented");
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
                            System.Console.Error.WriteLine("opcode 11" + Hex(opcode0X11, 2) + " not implemented");
                            System.Console.Error.WriteLine(PrintState());
                            break;
                    } // of case opcode 0x11 
                    break;

                default:
                    System.Console.Error.WriteLine("opcode " + Hex(opcode, 2) + " not implemented");
                    System.Console.Error.WriteLine(PrintState());
                    break;
            } // of case  opcode
        } // of method fetch()


// DISASSEMBLE/DEBUG PART
        public string PrintState()
        {
            _cc = Getcc();
            string s = "A=" + Hex(_a, 2) + " B=" + Hex(_b, 2);
            s += " X=" + Hex(_x, 4) + " Y=" + Hex(_y, 4) + "\n";
            s += "PC=" + Hex(Pc, 4) + " DP=" + Hex(_dp, 2);
            s += " U=" + Hex(_u, 4) + " S=" + Hex(_s, 4);
            s += " CC=" + Hex(_cc, 2);
            return s;
        }

        private string Hex(int val, int size)
        {
            string output = "";
            for (var t = 0; t < size; t++)
            {
                var coef = (size - t - 1) * 4;
                var mask = 0xF << coef;
                var q = (val & mask) >> coef;
                if (q < 10) output = output + q;
                else
                {
                    switch (q)
                    {
                        case 10:
                            output = output + "A";
                            break;
                        case 11:
                            output = output + "B";
                            break;
                        case 12:
                            output = output + "C";
                            break;
                        case 13:
                            output = output + "D";
                            break;
                        case 14:
                            output = output + "E";
                            break;
                        case 15:
                            output = output + "F";
                            break;
                    }
                }
            }
            return output;
        }

// force sign extension in a portable but ugly maneer
        private int SignedChar(int v)
        {
            if ((v & 0x80) == 0) return v & 0xFF;
            int delta = -1; // delta is 0xFFFF.... independently of 32/64bits
            delta = (delta >> 8) << 8; // force last 8bits to 0
            return (v & 0xFF) | delta; // result is now signed
        }

// force sign extension in a portable but ugly maneer
        private int Signed16Bits(int v)
        {
            if ((v & 0x8000) == 0) return v & 0xFFFF;
            int delta = -1; // delta is 0xFFFF.... independently of 32/64bits
            delta = (delta >> 16) << 16; // force last 16bits to 0
            return (v & 0xFFFF) | delta; // result is now signed
        }

        private static string Regx(int m)
        {
            switch (m & 0x60)
            {
                case 0x00:
                    return "X";
                case 0x20:
                    return "Y";
                case 0x40:
                    return "U";
                case 0x60:
                    return "S";
            }
            return "?";
        }

        private string r_tfr(int m)
        {
            var output = new StringBuilder();
            switch (m & 0xF0)
            {
                case 0x80:
                    output.Append("A,");
                    break;
                case 0x90:
                    output.Append("B,");
                    break;
                case 0xA0:
                    output.Append("CC,");
                    break;
                case 0x00:
                    output.Append("D,");
                    break;
                case 0xB0:
                    output.Append("DP,");
                    break;
                case 0x50:
                    output.Append("PC,");
                    break;
                case 0x40:
                    output.Append("S,");
                    break;
                case 0x30:
                    output.Append("U,");
                    break;
                case 0x10:
                    output.Append("X,");
                    break;
                case 0x20:
                    output.Append("Y,");
                    break;
            }
            switch (m & 0x0F)
            {
                case 0x8:
                    output.Append("A");
                    break;
                case 0x9:
                    output.Append("B");
                    break;
                case 0xA:
                    output.Append("CC");
                    break;
                case 0x0:
                    output.Append("D");
                    break;
                case 0xB:
                    output.Append("DP");
                    break;
                case 0x5:
                    output.Append("PC");
                    break;
                case 0x4:
                    output.Append("S");
                    break;
                case 0x3:
                    output.Append("U");
                    break;
                case 0x1:
                    output.Append("X");
                    break;
                case 0x2:
                    output.Append("Y");
                    break;
            }
            return output.ToString();
        }

        private string r_pile(int m)
        {
            var output = new StringBuilder();
            if ((m & 0x80) != 0) output.Append("PC,");
            if ((m & 0x40) != 0) output.Append("U/S,");
            if ((m & 0x20) != 0) output.Append("Y,");
            if ((m & 0x10) != 0) output.Append("X,");
            if ((m & 0x08) != 0) output.Append("DP,");
            if ((m & 0x04) != 0) output.Append("B,");
            if ((m & 0x02) != 0) output.Append("A,");
            if ((m & 0x01) != 0) output.Append("CC");
            return output.ToString();
        }


        public string Disassemble(int start, int maxLines)
        {
            string[] MNEMO = new string[256];
            string[] mnemo10 = new string[256];
            string[] mnemo11 = new string[256];

            string output = "";

            // init all strings
            for (var l = 0; l < 256; l++)
            {
                MNEMO[l] = "ILL -";
                mnemo10[l] = "ILL -";
                mnemo11[l] = "ILL -";
            }

            /* LDA opcode */
            MNEMO[0x86] = "LDA i";
            MNEMO[0x96] = "LDA d";
            MNEMO[0xB6] = "LDA e";
            MNEMO[0xA6] = "LDA x";

            /* LDB opcode */
            MNEMO[0xC6] = "LDB i";
            MNEMO[0xD6] = "LDB d";
            MNEMO[0xF6] = "LDB e";
            MNEMO[0xE6] = "LDB x";

            /* LDD opcode */
            MNEMO[0xCC] = "LDD I";
            MNEMO[0xDC] = "LDD d";
            MNEMO[0xFC] = "LDD e";
            MNEMO[0xEC] = "LDD x";

            /* LDU opcode */
            MNEMO[0xCE] = "LDU I";
            MNEMO[0xDE] = "LDU d";
            MNEMO[0xFE] = "LDU e";
            MNEMO[0xEE] = "LDU x";

            /* LDX opcode */
            MNEMO[0x8E] = "LDX I";
            MNEMO[0x9E] = "LDX d";
            MNEMO[0xBE] = "LDX e";
            MNEMO[0xAE] = "LDX x";

            /* LDS opcode */
            mnemo10[0xCE] = "LDS I";
            mnemo10[0xDE] = "LDS d";
            mnemo10[0xFE] = "LDS e";
            mnemo10[0xEE] = "LDS x";

            /* LDY opcode */
            mnemo10[0x8E] = "LDY I";
            mnemo10[0x9E] = "LDY d";
            mnemo10[0xBE] = "LDY e";
            mnemo10[0xAE] = "LDY x";

            /* STA opcode */
            MNEMO[0x97] = "STA d";
            MNEMO[0xB7] = "STA e";
            MNEMO[0xA7] = "STA x";

            /* STB opcode */
            MNEMO[0xD7] = "STB d";
            MNEMO[0xF7] = "STB e";
            MNEMO[0xE7] = "STB x";

            /* STD opcode */
            MNEMO[0xDD] = "STD d";
            MNEMO[0xFD] = "STD e";
            MNEMO[0xED] = "STD x";

            /* STS opcode */
            mnemo10[0xDF] = "STS d";
            mnemo10[0xFF] = "STS e";
            mnemo10[0xEF] = "STS x";

            /* STU opcode */
            MNEMO[0xDF] = "STU d";
            MNEMO[0xFF] = "STU e";
            MNEMO[0xEF] = "STU x";

            /* STX opcode */
            MNEMO[0x9F] = "STX d";
            MNEMO[0xBF] = "STX e";
            MNEMO[0xAF] = "STX x";

            /* STY opcode */
            mnemo10[0x9F] = "STY d";
            mnemo10[0xBF] = "STY e";
            mnemo10[0xAF] = "STY x";

            /* LEA opcode */
            MNEMO[0x32] = "LEASx";
            MNEMO[0x33] = "LEAUx";
            MNEMO[0x30] = "LEAXx";
            MNEMO[0x31] = "LEAYx";

            /* CLR opcode */
            MNEMO[0x0F] = "CLR d";
            MNEMO[0x7F] = "CLR e";
            MNEMO[0x6F] = "CLR x";
            MNEMO[0x4F] = "CLRA-";
            MNEMO[0x5F] = "CLRB-";

            /* EXG */
            MNEMO[0x1E] = "EXG r";

            /* TFR */
            MNEMO[0x1F] = "TFR r";

            /* PSH */
            MNEMO[0x34] = "PSHSR";
            MNEMO[0x36] = "PSHUR";

            /* PUL */
            MNEMO[0x35] = "PULSR";
            MNEMO[0x37] = "PULUR";

            /* INC */
            MNEMO[0x4C] = "INCA-";
            MNEMO[0x5C] = "INCB-";
            MNEMO[0x7C] = "INC e";
            MNEMO[0x0C] = "INC d";
            MNEMO[0x6C] = "INC x";

            /* DEC */
            MNEMO[0x4A] = "DECA-";
            MNEMO[0x5A] = "DECB-";
            MNEMO[0x7A] = "DEC e";
            MNEMO[0x0A] = "DEC d";
            MNEMO[0x6A] = "DEC x";

            /* BIT */
            MNEMO[0x85] = "BITAi";
            MNEMO[0x95] = "BITAd";
            MNEMO[0xB5] = "BITAe";
            MNEMO[0xA5] = "BITAx";
            MNEMO[0xC5] = "BITBi";
            MNEMO[0xD5] = "BITBd";
            MNEMO[0xF5] = "BITBe";
            MNEMO[0xE5] = "BITBx";

            /* CMP */
            MNEMO[0x81] = "CMPAi";
            MNEMO[0x91] = "CMPAd";
            MNEMO[0xB1] = "CMPAe";
            MNEMO[0xA1] = "CMPAx";
            MNEMO[0xC1] = "CMPBi";
            MNEMO[0xD1] = "CMPBd";
            MNEMO[0xF1] = "CMPBe";
            MNEMO[0xE1] = "CMPBx";
            mnemo10[0x83] = "CMPDI";
            mnemo10[0x93] = "CMPDd";
            mnemo10[0xB3] = "CMPDe";
            mnemo10[0xA3] = "CMPDx";
            mnemo11[0x8C] = "CMPSI";
            mnemo11[0x9C] = "CMPSd";
            mnemo11[0xBC] = "CMPSe";
            mnemo11[0xAC] = "CMPSx";
            mnemo11[0x83] = "CMPUI";
            mnemo11[0x93] = "CMPUd";
            mnemo11[0xB3] = "CMPUe";
            mnemo11[0xA3] = "CMPUx";
            MNEMO[0x8C] = "CMPXI";
            MNEMO[0x9C] = "CMPXd";
            MNEMO[0xBC] = "CMPXe";
            MNEMO[0xAC] = "CMPXx";
            mnemo10[0x8C] = "CMPYI";
            mnemo10[0x9C] = "CMPYd";
            mnemo10[0xBC] = "CMPYe";
            mnemo10[0xAC] = "CMPYx";

            /* TST */
            MNEMO[0x4D] = "TSTA-";
            MNEMO[0x5D] = "TSTB-";
            MNEMO[0x0D] = "TST d";
            MNEMO[0x7D] = "TST e";
            MNEMO[0x6D] = "TST x";

            /* AND */
            MNEMO[0x84] = "ANDAi";
            MNEMO[0x94] = "ANDAd";
            MNEMO[0xB4] = "ANDAe";
            MNEMO[0xA4] = "ANDAx";
            MNEMO[0xC4] = "ANDBi";
            MNEMO[0xD4] = "ANDBd";
            MNEMO[0xF4] = "ANDBe";
            MNEMO[0xE4] = "ANDBx";
            MNEMO[0x1C] = "& CCi";

            /* OR */
            MNEMO[0x8A] = "ORA i";
            MNEMO[0x9A] = "ORA d";
            MNEMO[0xBA] = "ORA e";
            MNEMO[0xAA] = "ORA x";
            MNEMO[0xCA] = "ORB i";
            MNEMO[0xDA] = "ORB d";
            MNEMO[0xFA] = "ORB e";
            MNEMO[0xEA] = "ORB x";
            MNEMO[0x1A] = "ORCCi";

            /* EOR */
            MNEMO[0x88] = "EORAi";
            MNEMO[0x98] = "EORAd";
            MNEMO[0xB8] = "EORAe";
            MNEMO[0xA8] = "EORAx";
            MNEMO[0xC8] = "EORBi";
            MNEMO[0xD8] = "EORBd";
            MNEMO[0xF8] = "EORBe";
            MNEMO[0xE8] = "EORBx";

            /* COM */
            MNEMO[0x03] = "COM d";
            MNEMO[0x73] = "COM e";
            MNEMO[0x63] = "COM x";
            MNEMO[0x43] = "COMA-";
            MNEMO[0x53] = "COMB-";

            /* NEG */
            MNEMO[0x00] = "NEG d";
            MNEMO[0x70] = "NEG e";
            MNEMO[0x60] = "NEG x";
            MNEMO[0x40] = "NEGA-";
            MNEMO[0x50] = "NEGB-";

            /* ABX */
            MNEMO[0x3A] = "ABX -";

            /* ADC */
            MNEMO[0x89] = "ADCAi";
            MNEMO[0x99] = "ADCAd";
            MNEMO[0xB9] = "ADCAe";
            MNEMO[0xA9] = "ADCAx";
            MNEMO[0xC9] = "ADCBi";
            MNEMO[0xD9] = "ADCBd";
            MNEMO[0xF9] = "ADCBe";
            MNEMO[0xE9] = "ADCBx";

            /* ADD */
            MNEMO[0x8B] = "ADDAi";
            MNEMO[0x9B] = "ADDAd";
            MNEMO[0xBB] = "ADDAe";
            MNEMO[0xAB] = "ADDAx";
            MNEMO[0xCB] = "ADDBi";
            MNEMO[0xDB] = "ADDBd";
            MNEMO[0xFB] = "ADDBe";
            MNEMO[0xEB] = "ADDBx";
            MNEMO[0xC3] = "ADDDI";
            MNEMO[0xD3] = "ADDDd";
            MNEMO[0xF3] = "ADDDe";
            MNEMO[0xE3] = "ADDDx";

            /* MUL */
            MNEMO[0x3D] = "MUL -";


            /* SBC */
            MNEMO[0x82] = "SBCAi";
            MNEMO[0x92] = "SBCAd";
            MNEMO[0xB2] = "SBCAe";
            MNEMO[0xA2] = "SBCAx";
            MNEMO[0xC2] = "SBCBi";
            MNEMO[0xD2] = "SBCBd";
            MNEMO[0xF2] = "SBCBe";
            MNEMO[0xE2] = "SBCBx";

            /* SUB */
            MNEMO[0x80] = "SUBAi";
            MNEMO[0x90] = "SUBAd";
            MNEMO[0xB0] = "SUBAe";
            MNEMO[0xA0] = "SUBAx";
            MNEMO[0xC0] = "SUBBi";
            MNEMO[0xD0] = "SUBBd";
            MNEMO[0xF0] = "SUBBe";
            MNEMO[0xE0] = "SUBBx";
            MNEMO[0x83] = "SUBDI";
            MNEMO[0x93] = "SUBDd";
            MNEMO[0xB3] = "SUBDe";
            MNEMO[0xA3] = "SUBDx";

            /* SEX */
            MNEMO[0x1D] = "SEX -";

            /* ASL */
            MNEMO[0x08] = "ASL d";
            MNEMO[0x78] = "ASL e";
            MNEMO[0x68] = "ASL x";
            MNEMO[0x48] = "ASLA-";
            MNEMO[0x58] = "ASLB-";

            /* ASR */
            MNEMO[0x07] = "ASR d";
            MNEMO[0x77] = "ASR e";
            MNEMO[0x67] = "ASR x";
            MNEMO[0x47] = "ASRA-";
            MNEMO[0x57] = "ASRB-";

            /* LSR */
            MNEMO[0x04] = "LSR d";
            MNEMO[0x74] = "LSR e";
            MNEMO[0x64] = "LSR x";
            MNEMO[0x44] = "LSRA-";
            MNEMO[0x54] = "LSRB-";

            /* ROL */
            MNEMO[0x09] = "ROL d";
            MNEMO[0x79] = "ROL e";
            MNEMO[0x69] = "ROL x";
            MNEMO[0x49] = "ROLA-";
            MNEMO[0x59] = "ROLB-";

            /* ROR */
            MNEMO[0x06] = "ROR d";
            MNEMO[0x76] = "ROR e";
            MNEMO[0x66] = "ROR x";
            MNEMO[0x46] = "RORA-";
            MNEMO[0x56] = "RORB-";

            /* BRA */
            MNEMO[0x20] = "BRA o";
            MNEMO[0x16] = "LBRAO";

            /* JMP */
            MNEMO[0x0E] = "JMP d";
            MNEMO[0x7E] = "JMP e";
            MNEMO[0x6E] = "JMP x";

            /* BSR */
            MNEMO[0x8D] = "BSR o";
            MNEMO[0x17] = "LBSRO";

            /* JSR */
            MNEMO[0x9D] = "JSR d";
            MNEMO[0xBD] = "JSR e";
            MNEMO[0xAD] = "JSR x";

            /* BRN */
            MNEMO[0x21] = "BRN o";
            mnemo10[0x21] = "LBRNO";

            /* NOP */
            MNEMO[0x12] = "NOP -";

            /* RTS */
            MNEMO[0x39] = "RTS -";

            /* BCC */
            MNEMO[0x24] = "BCC o";
            mnemo10[0x24] = "LBCCO";

            /* BCS */
            MNEMO[0x25] = "BCS o";
            mnemo10[0x25] = "LBCSO";

            /* BEQ */
            MNEMO[0x27] = "BEQ o";
            mnemo10[0x27] = "LBEQO";

            /* BNE */
            MNEMO[0x26] = "BNE o";
            mnemo10[0x26] = "LBNEO";

            /* BGE */
            MNEMO[0x2C] = "BGE o";
            mnemo10[0x2C] = "LBGEO";

            /* BLE */
            MNEMO[0x2F] = "BLE o";
            mnemo10[0x2F] = "LBLEO";

            /* BLS */
            MNEMO[0x23] = "BLS o";
            mnemo10[0x23] = "LBLSO";

            /* BGT */
            MNEMO[0x2E] = "BGT o";
            mnemo10[0x2E] = "LBGTO";

            /* BLT */
            MNEMO[0x2D] = "BLT o";
            mnemo10[0x2D] = "LBLTO";

            /* BHI */
            MNEMO[0x22] = "BHI o";
            mnemo10[0x22] = "LBHIO";

            /* BMI */
            MNEMO[0x2B] = "BMI o";
            mnemo10[0x2B] = "LBMIO";

            /* BPL */
            MNEMO[0x2A] = "BPL o";
            mnemo10[0x2A] = "LBPLO";

            /* BVC */
            MNEMO[0x28] = "BVC o";
            mnemo10[0x28] = "LBVCO";

            /* BVS */
            MNEMO[0x29] = "BVS o";
            mnemo10[0x29] = "LBVSO";

            /* SWI1&3 */
            MNEMO[0x3F] = "SWI i";
            mnemo11[0x3F] = "SWI3-";

            /* RTI */
            MNEMO[0x3B] = "RTI -";


            int where = start;

            for (var line = 0; line < maxLines; line++)
            {
                var mm = _mem.Read(@where);
                where++;

                var output1 = Hex(@where - 1, 4) + ".";
                output1 = output1 + Hex(mm, 2) + " ";
                var output2 = "";

                string mnemo;
                if (mm == 0x10)
                {
                    mm = _mem.Read(where);
                    where++;
                    mnemo = mnemo10[mm];
                    output1 = output1 + Hex(mm, 2) + " ";
                    output2 = output2 + mnemo.Substring(0, 4) + " ";
                }
                else if (mm == 0x11)
                {
                    mm = _mem.Read(where);
                    where++;
                    mnemo = mnemo11[mm];
                    output1 = output1 + Hex(mm, 2) + " ";
                    output2 = output2 + mnemo.Substring(0, 4) + " ";
                }
                else
                {
                    mnemo = MNEMO[mm];
                    output2 = output2 + mnemo.Substring(0, 4) + " ";
                }
                switch (mnemo[4])
                {
                    case 'I':
                        mm = _mem.Read(where);
                        where++;
                        output1 = output1 + Hex(mm, 2) + " ";
                        output2 = output2 + "#x" + Hex(mm, 2);
                        mm = _mem.Read(where);
                        where++;
                        output1 = output1 + Hex(mm, 2) + " ";
                        output2 = output2 + Hex(mm, 2);
                        break;
                    case 'i':
                        mm = _mem.Read(where);
                        where++;
                        output1 = output1 + Hex(mm, 2) + " ";
                        output2 = output2 + "#x" + Hex(mm, 2);
                        break;
                    case 'e':
                        mm = _mem.Read(where);
                        where++;
                        output1 = output1 + Hex(mm, 2) + " ";
                        output2 = output2 + "x" + Hex(mm, 2);
                        mm = _mem.Read(where);
                        where++;
                        output1 = output1 + Hex(mm, 2) + " ";
                        output2 = output2 + Hex(mm, 2);
                        break;
                    case 'd':
                        mm = _mem.Read(where);
                        where++;
                        output1 = output1 + Hex(mm, 2) + " ";
                        output2 = output2 + "x" + Hex(mm, 2);
                        break;
                    case 'o':
                        mm = _mem.Read(where);
                        where++;
                        output1 = output1 + Hex(mm, 2) + " ";
                        output2 = output2 + SignedChar(mm) + " (=x" + Hex((where + SignedChar(mm)) & 0xFFFF, 4) + ")";

                        break;
                    case 'O':
                        mm = _mem.Read(where) << 8;
                        where++;
                        mm |= _mem.Read(where);
                        where++;
                        output1 = output1 + Hex(mm, 4) + " ";
                        output2 = output2 + Signed16Bits(mm) + " (=x" + Hex((where + Signed16Bits(mm)) & 0xFFFF, 4) +
                                  ")";

                        break;
                    case 'x':
                        int mmx;
                        mmx = _mem.Read(where);
                        where++;
                        output1 = output1 + Hex(mmx, 2) + " ";
                        if ((mmx & 0x80) == 0)
                        {
                            if ((mmx & 0x10) == 0)
                            {
                                output2 += (mmx & 0x0F) + ",";
                                output2 += Regx(mmx);
                            }
                            else
                            {
                                output2 += "-" + (mmx & 0x0F) + ",";
                                output2 += Regx(mmx);
                            }
                        }
                        else
                            switch (mmx & 0x1F)
                            {
                                case 0x04:
                                    output2 += ",";
                                    output2 += Regx(mmx);
                                    break;
                                case 0x14:
                                    output2 += "[,";
                                    output2 += Regx(mmx);
                                    output2 += "]";
                                    break;
                                case 0x08:
                                    mm = _mem.Read(where);
                                    where++;
                                    output1 = output1 + Hex(mm, 2) + " ";
                                    output2 += SignedChar(mm) + ",";
                                    output2 += Regx(mmx);
                                    break;
                                case 0x18:
                                    mm = _mem.Read(where);
                                    where++;
                                    output1 = output1 + Hex(mm, 2) + " ";
                                    output2 += "[" + SignedChar(mm) + ",";
                                    output2 += Regx(mmx);
                                    output2 += "]";
                                    break;
                                case 0x09:
                                    mm = _mem.Read(where) << 8;
                                    where++;
                                    mm |= _mem.Read(where);
                                    where++;
                                    output1 = output1 + Hex(mm, 4) + " ";
                                    output2 += Signed16Bits(mm) + ",";
                                    output2 += Regx(mmx);
                                    break;
                                case 0x19:
                                    mm = _mem.Read(where) << 8;
                                    where++;
                                    mm |= _mem.Read(where);
                                    where++;
                                    output1 = output1 + Hex(mm, 4) + " ";
                                    output2 += "[" + Signed16Bits(mm) + ",";
                                    output2 += Regx(mmx);
                                    output2 += "]";
                                    break;
                                case 0x06:
                                    output2 += "A,";
                                    output2 += Regx(mmx);
                                    break;
                                case 0x16:
                                    output2 += "[A,";
                                    output2 += Regx(mmx);
                                    output2 += "]";
                                    break;
                                case 0x05:
                                    output2 += "B,";
                                    output2 += Regx(mmx);
                                    break;
                                case 0x15:
                                    output2 += "[B,";
                                    output2 += Regx(mmx);
                                    output2 += "]";
                                    break;
                                case 0x0B:
                                    output2 += "D,";
                                    output2 += Regx(mmx);
                                    break;
                                case 0x1B:
                                    output2 += "[D,";
                                    output2 += Regx(mmx);
                                    output2 += "]";
                                    break;
                                case 0x00:
                                    output2 += ",";
                                    output2 += Regx(mmx);
                                    output2 += "+";
                                    break;
                                case 0x01:
                                    output2 += ",";
                                    output2 += Regx(mmx);
                                    output2 += "++";
                                    break;
                                case 0x11:
                                    output2 += "[,";
                                    output2 += Regx(mmx);
                                    output2 += "++]";
                                    break;
                                case 0x02:
                                    output2 += ",-";
                                    output2 += Regx(mmx);
                                    break;
                                case 0x03:
                                    output2 += ",--";
                                    output2 += Regx(mmx);
                                    break;
                                case 0x13:
                                    output2 += "[,--";
                                    output2 += Regx(mmx);
                                    output2 += "]";
                                    break;
                                case 0x0C:
                                    mm = _mem.Read(where);
                                    where++;
                                    output1 = output1 + Hex(mm, 2) + " ";
                                    output2 += SignedChar(mm) + ",PC";
                                    break;
                                case 0x1C:
                                    mm = _mem.Read(where);
                                    where++;
                                    output1 = output1 + Hex(mm, 2) + " ";
                                    output2 += "[" + SignedChar(mm) + ",PC";
                                    output2 += "]";
                                    break;
                                case 0x0D:
                                    mm = _mem.Read(where) << 8;
                                    where++;
                                    mm |= _mem.Read(where);
                                    where++;
                                    output1 = output1 + Hex(mm, 4) + " ";
                                    output2 += Signed16Bits(mm) + ",PC";
                                    break;
                                case 0x1D:
                                    mm = _mem.Read(where) << 8;
                                    where++;
                                    mm |= _mem.Read(where);
                                    where++;
                                    output1 = output1 + Hex(mm, 4) + " ";
                                    output2 += "[" + Signed16Bits(mm) + ",PC]";
                                    break;
                                case 0x1F:
                                    mm = _mem.Read(where) << 8;
                                    where++;
                                    mm |= _mem.Read(where);
                                    where++;
                                    output1 = output1 + Hex(mm, 2) + " ";
                                    output1 = output1 + Hex(mm, 4) + " ";
                                    output2 += "[x" + Hex(mm, 4) + "]";
                                    break;
                                default:
                                    output2 += "Illegal !";
                                    break;
                            }

                        break;
                    case 'r':
                        mm = _mem.Read(where);
                        where++;
                        output1 = output1 + Hex(mm, 2) + " ";
                        output2 += r_tfr(mm);
                        break;
                    case 'R':
                        mm = _mem.Read(where);
                        where++;
                        output1 = output1 + Hex(mm, 2) + " ";
                        output2 += r_pile(mm);
                        break;
                }

                var lll = output1.Length;
                for (var ll = 0; ll < 32 - lll; ll++) output1 += " ";
                output += output1 + output2 + "\n";
            } // of for ... maxLines
            return output;
        }
    } // of class M6809
}