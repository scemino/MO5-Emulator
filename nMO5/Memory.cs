using System;
using System.Collections.Generic;
using System.IO;

namespace nMO5
{
	public class Memory
	{
		private bool[] _dirty;

		private int _k7Bit;
		private int _k7Char;

		private Stream _k7Fis;
		private Stream _k7Fos;
		private bool IsInFileOpened => _k7Fis != null;
		private bool IsOutFileOpened => _k7Fos != null;
		private string _k7OutName;
		private bool[] _key;
		private int[] _mapper;

		// 0 1          POINT   2
		// 2 3          COLOR   2
		// 4 5 6 7      RAM1    4
		// 8 9 10 11    RAM2    4
		// 12           LINEA   1
		// 13           LINEB   1
		// 14 15 16 17  ROM     4
		private int[][] _mem;

		/* Registres du 6821 */
		public int Ora;
		public int Orb;
		public int Ddra;
		public int Ddrb;
		public int Cra;
		public int Crb;
		public int SoundMem { get; private set; }

		// Lightpen parameters  
		public bool LightPenClick { get; set; }
		public int LightPenX { get; set; }
		public int LightPenY { get; set; }

		public int ShowLed { get; set; }
		public int Led { get; private set; }

		public Memory()
		{
			_mem = new int[18][];
			for (var j = 0; j < _mem.Length; j++)
			{
				_mem[j] = new int[0x1000];
			}
			_mapper = new int[] {
				0,1,4,5,6,7,8,9,10,11,12,13,14,15,16,17
			};
			_key = new bool[256];
			_dirty = new bool[200];

			Reset();
		}

		// read with io
		public int Read(int address)
		{
			var page = (address & 0xF000) >> 12;
			return _mem[_mapper[page]][address & 0xFFF];
		}

		public int Read16(int address)
		{
			return Read(address) << 8 | Read(address + 1);
		}

		public List<int> Find8(int value)
		{
			var adresses = new List<int>();
			for (int addr = 0x2200; addr <= 0x9FFF; addr++)
			{
				var memValue = Read(addr);
				if (memValue == value)
				{
					adresses.Add(addr);
				}
			}
			return adresses;
		}

		public List<int> Find16(int value)
		{
			var adresses = new List<int>();
			for (int addr = 0x2200; addr <= 0x9FFF; addr++)
			{
				var memValue = Read(addr);
				memValue <<= 8;
				memValue |= Read(addr + 1);
				if (memValue == value)
				{
					adresses.Add(addr);
				}
			}
			return adresses;
		}

		// write with io
		public void Write(int address, int value)
		{
			var page = (address & 0xF000) >> 12;

			if (_mapper[page] >= 14 && _mapper[page] <= 17)
				return; // Protection en écriture de la ROM

			if (address < 0x1F40) _dirty[address / 40] = true;
			if (page == 0x0A) Hardware(address, value);
			else
				_mem[_mapper[page]][address & 0xFFF] = value & 0xFF;
		}

		public void Set(int address, int value)
		{
			var page = (address & 0xF000) >> 12;
			_mem[_mapper[page]][address & 0xFFF] = value & 0xFF;
		}

		public void Set16(int address, int value)
		{
			Set(address, value >> 8);
			Set(address + 1, value & 0xFF);
		}

		public int Point(int address)
		{
			var page = (address & 0xF000) >> 12;
			return _mem[page][address & 0xFFF];
		}

		public int Color(int address)
		{
			var page = (address & 0xF000) >> 12;
			return _mem[page + 2][address & 0xFFF];
		}

		public bool IsDirty(int line)
		{
			var ret = _dirty[line];
			_dirty[line] = false;
			return ret;
		}

		public void SetKey(int i)
		{
			_key[i] = true;
		}

		public void RemKey(int i)
		{
			_key[i] = false;
		}

		public void SetK7File(string k7)
		{
			Console.WriteLine("opening: {0}", k7);
			try
			{
				_k7Fis?.Dispose();
				_k7Fis = File.OpenRead(k7);

				var indexMax = _k7Fis.Length >> 9;
				Console.WriteLine("Max index: {0}", indexMax);

				//K7Reader.Read(_k7Fis);
				_k7Fis.Seek(0, SeekOrigin.Begin);

			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Error : file is missing " + e);
				return;
			}

			_k7Bit = 0;
			_k7Char = 0;
		}

		public void Rewind()
		{
			_k7Fis?.Seek(0, SeekOrigin.Begin);
		}

		public void Periph(int pc, int s, int a)
		{
			var dataOut = new byte[1];

			if (pc == 0xF169)
				ReadBit();
			/* Write K7 byte */
			/* Merci 
     Olivier Tardieu pour le dsassemblage de la routine en ROM */
			if (pc == 0xF1B0)
			{
				CreateK7File(); // To do if necessary

				if (!IsOutFileOpened) return;

				dataOut[0] = (byte)a;
				try
				{
					_k7Fos.Write(dataOut, 0, dataOut.Length);
				}
				catch (IOException e)
				{
					Console.Error.WriteLine(e.StackTrace);
				}
			}

			// Motor On/Off/Test
			if (pc == 0xF18C)
			{
				// Mise à 0 du bit C
				int c = Read(s);
				c &= 0xFE;
				Write(s, c);
				//Console.WriteLine("motor ");
			}
			if (pc == 0xf549)
			{
				Write(s + 6, LightPenX >> 8);
				Write(s + 7, LightPenX & 255);
				Write(s + 8, LightPenY >> 8);
				Write(s + 9, LightPenY & 255);
			}
		}

		// write with io without Protection
		private void WriteP(int address, int value)
		{
			var page = (address & 0xF000) >> 12;
			if (address < 0x1F40) _dirty[address / 40] = true;
			if (page == 0x0A) Hardware(address, value);
			else
				_mem[_mapper[page]][address & 0xFFF] = value & 0xFF;
		}

		private void Reset()
		{
			for (var i = 0; i < 0xFFFF; i++)
			{
				Set(i, 0x00);
			}
			LoadRom();
			Cra = 0x00;
			Crb = 0x00;
			Ddra = 0x5F;
			Ddrb = 0x7F;

			_mem[0xA + 2][0x7CC] = 0xFF;
			_mem[0xA + 2][0x7CD] = 0xFF;
			_mem[0xA + 2][0x7CE] = 0xFF;
			_mem[0xA + 2][0x7CF] = 0xFF;

			PatchK7();
		}

		private void LoadRom()
		{
			const int startingAddress = 0xC000;
			try
			{
				using (var fis = File.OpenRead("./bios/mo5.rom"))
				{
					for (var i = startingAddress; i < 0x10000; i++)
						WriteP(i, fis.ReadByte());
				}
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Error : mo5.rom file is missing {0}", e);
			}
		}

		private void Hardware(int adr, int op)
		{
			/* 6821 système */
			/* acces à ORA ou DDRA */
			switch (adr)
			{
				case 0xA7C0:
					if ((Cra & 0x04) == 0x04)
					/* Accès à ORA */
					{
						if ((op & 0x01) == 0x01)
						{
							_mapper[0] = 0;
							_mapper[1] = 1;
						}
						else
						{
							_mapper[0] = 2;
							_mapper[1] = 3;
						}
						/* Mise à jour de ORA selon le masque DDRA */
						op |= 0x80 + 0x20; // gestion de ,l'inter optique 
						Ora = (Ora & (Ddra ^ 0xFF)) | (op & Ddra);
						if (LightPenClick)
							_mem[0xA + 2][0x7C0] = Ora | 0x20;
						else
							_mem[0xA + 2][0x7C0] = Ora & ~0x20;
					}
					else
					{
						Ddra = op;
						_mem[0xA + 2][0x7C0] = op;
					}
					break;
				case 0xA7C1:
					if ((Crb & 0x04) == 0x04)
					/* Accès à ORB */
					{
						Orb = (Orb & (Ddrb ^ 0xFF)) | (op & Ddrb);

						/* GESTION HARD DU CLAVIER */

						if (_key[Orb & 0x7E])
							Orb = Orb & 0x7F;
						else
							Orb = Orb | 0x80;

						_mem[0xA + 2][0x7C1] = Orb;
						SoundMem = (Orb & 1) << 5;
					}
					else
					{
						Ddrb = op;
						_mem[0xA + 2][0x7C1] = op;
					}
					break;
				case 0xA7C2:
					Cra = (Cra & 0xD0) | (op & 0x3F);
					_mem[0xA + 2][0x7C2] = Cra;
					break;
				case 0xA7C3:
					Crb = (Crb & 0xD0) | (op & 0x3F);
					_mem[0xA + 2][0x7C3] = Crb;
					break;
			}
		}

		private void CreateK7File()
		{
			if (_k7OutName != null)
				return;

			var today = DateTime.Now;

			_k7OutName = today.ToString("yyyy-MM-dd-HH_mm_ss") + ".k7";

			Console.WriteLine("Creating:" + _k7OutName);
			try
			{
				_k7Fos?.Dispose();
				_k7Fos = File.OpenWrite(_k7OutName);
				Console.Error.WriteLine("Information : new file {0}", _k7OutName);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Error : file not created {0}", e);
				return;
			}

			_k7Bit = 0;
			_k7Char = 0;
		}

        internal void ReadByte(M6809 machine)
        {
            if (!IsInFileOpened) return;

            int data = 0;
            _k7Char = _k7Fis.ReadByte();

            machine.A = _k7Char;
            Set(0x2045, data); 
            _k7Bit = 0;
        }

        public void ReadBit()
		{
			if (!IsInFileOpened) return;

			/* doit_on lire un caractere ? */
			if (_k7Bit == 0x00)
			{
				try
				{
					_k7Char = _k7Fis.ReadByte();
				}
				catch (Exception)
				{
				}

				_k7Bit = 0x80;
			}
			var octet = Read(0x2045);

			if ((_k7Char & _k7Bit) == 0)
			{
				octet = octet << 1;
				// A=0x00;
				Set(0xF16A, 0x00);
			}
			else
			{
				octet = (octet << 1) | 0x01;
				// A=0xFF;
				Set(0xF16A, 0xFF);
			}
			/* positionne l'octet dans la page 0 du moniteur */
			Set(0x2045, octet & 0xFF);
			Led = octet & 0xff;
			ShowLed = 10;
			_k7Bit = _k7Bit >> 1;
		}

		private void PatchK7()
		{
			/*

                PATCH une partie des fonctions du moniteur
                la squence 02 39 correspond 

                Illegal (instruction)
                NOP
                le TRAP active la gestion des
                périphriques, la valeur du
                PC 
                ce moment permet de determiner
                la fonction à effectuer

            */
			// Crayon optique
			Set(0xf548, 0x02); // PER instruction émulateur
			Set(0xf549, 0x39); // RTS


			Set(0xF1AF, 0x02);
			Set(0xF1B0, 0x39);

			Set(0xF18B, 0x02);
			Set(0xF18C, 0x39);

			Set(0xF168, 0x02);

			// LDA immediate for return
			Set(0xF169, 0x86); //RTS
			Set(0xF16A, 0x00); // no opcode

			Set(0xF16B, 0x39);
		}
	}
}