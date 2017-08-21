using System.IO;
using System.Text;
using static nMO5.Util;

namespace nMO5
{
    public class Disassembler
    {
        private string[] MNEMO = new string[256];
        private string[] mnemo10 = new string[256];
        private string[] mnemo11 = new string[256];

        public Disassembler()
        {
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
        }

        public string Disassemble(Stream stream)
        {
            var br = new BinaryReader(stream);

            var output = new StringBuilder();
            while (stream.Position < stream.Length)
            {
                int mm = br.ReadByte();

                var output1 = Hex(mm, 2) + " ";
                var output2 = "";

                string mnemo;
                if (mm == 0x10)
                {
                    mm = br.ReadByte();
                    mnemo = mnemo10[mm];
                    output1 = output1 + Hex(mm, 2) + " ";
                    output2 = output2 + mnemo.Substring(0, 4) + " ";
                }
                else if (mm == 0x11)
                {
                    mm = br.ReadByte();
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
                        mm = br.ReadByte();
                        output1 = output1 + Hex(mm, 2) + " ";
                        output2 = output2 + "#$" + Hex(mm, 2);
                        mm = br.ReadByte();
                        output1 = output1 + Hex(mm, 2) + " ";
                        output2 = output2 + Hex(mm, 2);
                        break;
                    case 'i':
                        mm = br.ReadByte();
                        output1 = output1 + Hex(mm, 2) + " ";
                        output2 = output2 + "#$" + Hex(mm, 2);
                        break;
                    case 'e':
                        mm = br.ReadByte();
                        output1 = output1 + Hex(mm, 2) + " ";
                        output2 = output2 + "$" + Hex(mm, 2);
                        mm = br.ReadByte();
                        output1 = output1 + Hex(mm, 2) + " ";
                        output2 = output2 + Hex(mm, 2);
                        break;
                    case 'd':
                        mm = br.ReadByte();
                        output1 = output1 + Hex(mm, 2) + " ";
                        output2 = output2 + "$" + Hex(mm, 2);
                        break;
                    case 'o':
                        mm = br.ReadByte();
                        output1 = output1 + Hex(mm, 2) + " ";
                        output2 = output2 + SignedChar(mm);
                        break;
                    case 'O':
                        mm = br.ReadByte() << 8;
                        mm |= br.ReadByte();
                        output1 = output1 + Hex(mm, 4) + " ";
                        output2 = output2 + Signed16Bits(mm);
                        break;
                    case 'x':
                        int mmx;
                        mmx = br.ReadByte();
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
                                    mm = br.ReadByte();
                                    output1 = output1 + Hex(mm, 2) + " ";
                                    output2 += SignedChar(mm) + ",";
                                    output2 += Regx(mmx);
                                    break;
                                case 0x18:
                                    mm = br.ReadByte();
                                    output1 = output1 + Hex(mm, 2) + " ";
                                    output2 += "[" + SignedChar(mm) + ",";
                                    output2 += Regx(mmx);
                                    output2 += "]";
                                    break;
                                case 0x09:
                                    mm = br.ReadByte();
                                    mm |= br.ReadByte();
                                    output1 = output1 + Hex(mm, 4) + " ";
                                    output2 += Signed16Bits(mm) + ",";
                                    output2 += Regx(mmx);
                                    break;
                                case 0x19:
                                    mm = br.ReadByte() << 8;
                                    mm |= br.ReadByte();
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
                                    mm = br.ReadByte();
                                    output1 = output1 + Hex(mm, 2) + " ";
                                    output2 += SignedChar(mm) + ",PC";
                                    break;
                                case 0x1C:
                                    mm = br.ReadByte();
                                    output1 = output1 + Hex(mm, 2) + " ";
                                    output2 += "[" + SignedChar(mm) + ",PC";
                                    output2 += "]";
                                    break;
                                case 0x0D:
                                    mm = br.ReadByte() << 8;
                                    mm |= br.ReadByte();
                                    output1 = output1 + Hex(mm, 4) + " ";
                                    output2 += Signed16Bits(mm) + ",PC";
                                    break;
                                case 0x1D:
                                    mm = br.ReadByte() << 8;
                                    mm |= br.ReadByte();
                                    output1 = output1 + Hex(mm, 4) + " ";
                                    output2 += "[" + Signed16Bits(mm) + ",PC]";
                                    break;
                                case 0x1F:
                                    mm = br.ReadByte() << 8;
                                    mm |= br.ReadByte();
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
                        mm = br.ReadByte();
                        output1 = output1 + Hex(mm, 2) + " ";
                        output2 += r_tfr(mm);
                        break;
                    case 'R':
                        mm = br.ReadByte();
                        output1 = output1 + Hex(mm, 2) + " ";
                        output2 += r_pile(mm);
                        break;
                }

                var lll = output1.Length;
                for (var ll = 0; ll < 32 - lll; ll++) output1 += " ";
                output.Append(output1).Append(output2).AppendLine();
            } // of for ... maxLines
            return output.ToString();
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

        private string Hex(int val, int size)
        {
            return val.ToString($"X{size}");
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
    }
}
