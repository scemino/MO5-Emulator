using Moq;
using Xunit;

namespace nMO5.Testing
{
    public class CpuTest
    {
        private readonly Mock<IMemory> _mockMem;
        private readonly Mock<ISound> _mockSound;
        private readonly M6809 _cpu;

        public CpuTest()
        {
            _mockMem = new Mock<IMemory>();
            _mockSound = new Mock<ISound>();
            _cpu = new M6809(_mockMem.Object, _mockSound.Object);
        }

        [Fact]
        public void Opcode0x86()
        {
            // LDA
            SetupOpcodeToExecute(0x86);
            _mockMem.Setup(o => o.Read(1)).Returns(42);
            var clock = _cpu.Fetch();
            CheckRegisters(a: 42, d:42<<8, pc: 2);
        }

        [Fact]
        public void Opcode0x96()
        {
            // LDA
            SetupOpcodeToExecute(0x96);
            _mockMem.Setup(o => o.Read(1)).Returns(42);
            _mockMem.Setup(o => o.Read(42)).Returns(0xBE);
            var clock = _cpu.Fetch();
            CheckRegisters(a: 0xBE, d: 0xBE << 8, pc: 2);
        }

        [Fact]
        public void Opcode0xB6()
        {
            // LDA
            SetupOpcodeToExecute(0xB6);
            _mockMem.Setup(o => o.Read16(1)).Returns(0xBEEF);
            _mockMem.Setup(o => o.Read(0xBEEF)).Returns(42);
            var clock = _cpu.Fetch();
            CheckRegisters(a: 42, d: 42 << 8, pc: 3);
        }


        [Fact]
        public void Opcode0xC6()
        {
            // LDB
            SetupOpcodeToExecute(0xC6);
            _mockMem.Setup(o => o.Read(1)).Returns(42);
            var clock = _cpu.Fetch();
            CheckRegisters(b: 42, d: 42, pc: 2);
        }

        [Fact]
        public void Opcode0xD6()
        {
            // LDB
            SetupOpcodeToExecute(0xD6);
            _mockMem.Setup(o => o.Read(1)).Returns(42);
            _mockMem.Setup(o => o.Read(42)).Returns(0xBE);
            var clock = _cpu.Fetch();
            CheckRegisters(b: 0xBE, d: 0xBE, pc: 2);
        }

        [Fact]
        public void Opcode0xF6()
        {
            // LDB
            SetupOpcodeToExecute(0xF6);
            _mockMem.Setup(o => o.Read16(1)).Returns(0xBEEF);
            _mockMem.Setup(o => o.Read(0xBEEF)).Returns(42);
            var clock = _cpu.Fetch();
            CheckRegisters(b: 42, d: 42, pc: 3);
        }


        [Fact]
        public void Opcode0xCC()
        {
            // LDD
            SetupOpcodeToExecute(0xCC);
            _mockMem.Setup(o => o.Read16(1)).Returns(0xBEEF);
            var clock = _cpu.Fetch();
            CheckRegisters(a: 0xBE, b: 0xEF, d: 0xBEEF, pc: 3);
        }

        [Fact]
        public void Opcode0xDC()
        {
            // LDD
            SetupOpcodeToExecute(0xDC);
            _mockMem.Setup(o => o.Read(1)).Returns(42);
            _mockMem.Setup(o => o.Read16(42)).Returns(0xBEEF);
            var clock = _cpu.Fetch();
            CheckRegisters(a: 0xBE, b: 0xEF, d: 0xBEEF, pc: 2);
        }

        [Fact]
        public void Opcode0xFC()
        {
            // LDD
            SetupOpcodeToExecute(0xFC);
            _mockMem.Setup(o => o.Read16(1)).Returns(0xDEAD);
            _mockMem.Setup(o => o.Read16(0xDEAD)).Returns(0xBEEF);
            var clock = _cpu.Fetch();
            CheckRegisters(a: 0xBE, b: 0xEF, d: 0xBEEF, pc: 3);
        }


        [Fact]
        public void Opcode0xCE()
        {
            // LDU
            SetupOpcodeToExecute(0xCE);
            _mockMem.Setup(o => o.Read16(1)).Returns(0xBEEF);
            var clock = _cpu.Fetch();
            CheckRegisters(u: 0xBEEF, pc: 3);
        }

        [Fact]
        public void Opcode0xDE()
        {
            // LDU
            SetupOpcodeToExecute(0xDE);
            _mockMem.Setup(o => o.Read(1)).Returns(42);
            _mockMem.Setup(o => o.Read16(42)).Returns(0xBEEF);
            var clock = _cpu.Fetch();
            CheckRegisters(u: 0xBEEF, pc: 2);
        }

        [Fact]
        public void Opcode0xFE()
        {
            // LDU
            SetupOpcodeToExecute(0xFE);
            _mockMem.Setup(o => o.Read16(1)).Returns(0xDEAD);
            _mockMem.Setup(o => o.Read16(0xDEAD)).Returns(0xBEEF);
            var clock = _cpu.Fetch();
            CheckRegisters(u: 0xBEEF, pc: 3);
        }


        [Fact]
        public void Opcode0x8E()
        {
            // LDX
            SetupOpcodeToExecute(0x8E);
            _mockMem.Setup(o => o.Read16(1)).Returns(0xBEEF);
            var clock = _cpu.Fetch();
            CheckRegisters(x: 0xBEEF, pc: 3);
        }

        [Fact]
        public void Opcode0x9E()
        {
            // LDX
            SetupOpcodeToExecute(0x9E);
            _mockMem.Setup(o => o.Read(1)).Returns(42);
            _mockMem.Setup(o => o.Read16(42)).Returns(0xBEEF);
            var clock = _cpu.Fetch();
            CheckRegisters(x: 0xBEEF, pc: 2);
        }

        [Fact]
        public void Opcode0xBE()
        {
            // LDX
            SetupOpcodeToExecute(0xBE);
            _mockMem.Setup(o => o.Read16(1)).Returns(0xDEAD);
            _mockMem.Setup(o => o.Read16(0xDEAD)).Returns(0xBEEF);
            var clock = _cpu.Fetch();
            CheckRegisters(x: 0xBEEF, pc: 3);
        }


        [Fact]
        public void Opcode0x97()
        {
            // STA
            SetupOpcodeToExecute(0x97);
            _cpu.A = 42;
            _mockMem.Setup(o => o.Read(1)).Returns(0xBE);
            var clock = _cpu.Fetch();
            CheckRegisters(a: 42, d: 42 << 8, pc: 2);
            _mockMem.Verify(o => o.Write(0xBE, 42));
        }

        [Fact]
        public void Opcode0xB7()
        {
            // STA
            SetupOpcodeToExecute(0xB7);
            _cpu.A = 42;
            _mockMem.Setup(o => o.Read16(1)).Returns(0xBEEF);
            var clock = _cpu.Fetch();
            CheckRegisters(a: 42, d: 42 << 8, pc: 3);
            _mockMem.Verify(o => o.Write(0xBEEF, 42));
        }


        [Fact]
        public void Opcode0xD7()
        {
            // STB
            SetupOpcodeToExecute(0xD7);
            _cpu.B = 42;
            _mockMem.Setup(o => o.Read(1)).Returns(0xBE);
            var clock = _cpu.Fetch();
            CheckRegisters(b: 42, d: 42, pc: 2);
            _mockMem.Verify(o => o.Write(0xBE, 42));
        }

        [Fact]
        public void Opcode0xF7()
        {
            // STB
            SetupOpcodeToExecute(0xF7);
            _cpu.B = 42;
            _mockMem.Setup(o => o.Read16(1)).Returns(0xBEEF);
            var clock = _cpu.Fetch();
            CheckRegisters(b: 42, d: 42, pc: 3);
            _mockMem.Verify(o => o.Write(0xBEEF, 42));
        }


        [Fact]
        public void Opcode0xDD()
        {
            // STD
            SetupOpcodeToExecute(0xDD);
            _cpu.D = 0xDEAD;
            _mockMem.Setup(o => o.Read(1)).Returns(0xBE);
            var clock = _cpu.Fetch();
            CheckRegisters(a: 0xDE, b: 0xAD, d: 0xDEAD, pc: 2);
            _mockMem.Verify(o => o.Write16(0xBE, 0xDEAD));
        }

        [Fact]
        public void Opcode0xFD()
        {
            // STD
            SetupOpcodeToExecute(0xFD);
            _cpu.D = 0xDEAD;
            _mockMem.Setup(o => o.Read16(1)).Returns(0xBEEF);
            var clock = _cpu.Fetch();
            CheckRegisters(a: 0xDE, b: 0xAD, d: 0xDEAD, pc: 3);
            _mockMem.Verify(o => o.Write16(0xBEEF, 0xDEAD));
        }


        [Fact]
        public void Opcode0xDF()
        {
            // STU
            SetupOpcodeToExecute(0xDF);
            _cpu.U = 0xDEAD;
            _mockMem.Setup(o => o.Read(1)).Returns(0xBE);
            var clock = _cpu.Fetch();
            CheckRegisters(u: 0xDEAD, pc: 2);
            _mockMem.Verify(o => o.Write16(0xBE, 0xDEAD));
        }

        [Fact]
        public void Opcode0xFF()
        {
            // STU
            SetupOpcodeToExecute(0xFF);
            _cpu.U = 0xDEAD;
            _mockMem.Setup(o => o.Read16(1)).Returns(0xBEEF);
            var clock = _cpu.Fetch();
            CheckRegisters(u: 0xDEAD, pc: 3);
            _mockMem.Verify(o => o.Write16(0xBEEF, 0xDEAD));
        }


        [Fact]
        public void Opcode0x9F()
        {
            // STX
            SetupOpcodeToExecute(0x9F);
            _cpu.X = 0xDEAD;
            _mockMem.Setup(o => o.Read(1)).Returns(0xBE);
            var clock = _cpu.Fetch();
            CheckRegisters(x: 0xDEAD, pc: 2);
            _mockMem.Verify(o => o.Write16(0xBE, 0xDEAD));
        }

        [Fact]
        public void Opcode0xBF()
        {
            // STX
            SetupOpcodeToExecute(0xBF);
            _cpu.X = 0xDEAD;
            _mockMem.Setup(o => o.Read16(1)).Returns(0xBEEF);
            var clock = _cpu.Fetch();
            CheckRegisters(x: 0xDEAD, pc: 3);
            _mockMem.Verify(o => o.Write16(0xBEEF, 0xDEAD));
        }

        [Fact]
        public void Opcode0x4F()
        {
            // CLRA
            SetupOpcodeToExecute(0x4F);
            _cpu.A = 42;
            var clock = _cpu.Fetch();
            CheckRegisters(pc: 1);
        }

        [Fact]
        public void Opcode0x5F()
        {
            // CLRB
            SetupOpcodeToExecute(0x5F);
            _cpu.B = 42;
            var clock = _cpu.Fetch();
            CheckRegisters(pc: 1);
        }

        [Fact]
        public void Opcode0x0F()
        {
            // CLR
            SetupOpcodeToExecute(0x0F);
            _mockMem.Setup(o => o.Read(1)).Returns(0xBE);
            var clock = _cpu.Fetch();
            CheckRegisters(pc: 2);
            _mockMem.Verify(o => o.Write(0xBE, 0));
        }

        [Fact]
        public void Opcode0x7F()
        {
            // CLR
            SetupOpcodeToExecute(0x7F);
            _mockMem.Setup(o => o.Read16(1)).Returns(0xBEEF);
            var clock = _cpu.Fetch();
            CheckRegisters(pc: 3);
            _mockMem.Verify(o => o.Write(0xBEEF, 0));
        }

        private void CheckRegisters(int a = 0, int b = 0, int dp = 0, int cc = 0x10, 
                                    int x = 0, int y = 0, int u = 0, int s = 0x8000,
                                    int d = 0, int pc = 0)
        {
            Assert.Equal(a, _cpu.A);
            Assert.Equal(b, _cpu.B);
            Assert.Equal(dp, _cpu.Dp);
            Assert.Equal(cc, _cpu.Cc);
            Assert.Equal(x, _cpu.X);
            Assert.Equal(y, _cpu.Y);
            Assert.Equal(u, _cpu.U);
            Assert.Equal(s, _cpu.S);
            Assert.Equal(d, _cpu.D);
            Assert.Equal(pc, _cpu.Pc);
        }

        private void SetupOpcodeToExecute(int opcode)
        {
            _mockMem.Setup(o => o.Read16(0xFFFE)).Returns(0);
            _mockMem.Setup(o => o.Read(0)).Returns(opcode);
        }
    }
}
