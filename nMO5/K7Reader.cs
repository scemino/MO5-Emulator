using System;
using System.IO;

namespace nMO5
{
	internal class K7Reader
	{
		Stream _k7Fis;

		private K7Reader(Stream stream)
		{
			_k7Fis = stream;
		}

		public static void Read(Stream stream)
		{
			var reader = new K7Reader(stream);
			reader.Read();
		}

		private void Read()
		{
			while (_k7Fis.Position < _k7Fis.Length)
			{
				// synchro bytes
				var synchro = new byte[18];
				_k7Fis.Read(synchro, 0, synchro.Length);
				if (synchro[16] != 0x3C || synchro[17] != 0x5A)
				{
					Console.Error.WriteLine("Invalid synchro: {0:X2}{1:X2}", synchro[16], synchro[17]);
					break;
				}

				var blockType = _k7Fis.ReadByte();
				var blockLength = _k7Fis.ReadByte();

				switch (blockType)
				{
					case 0: ReadBasicData(blockLength); break;
					case 1: ReadBinData(blockLength); break;
					case 0xFF:
						{
							var data = new byte[blockLength - 1];
							_k7Fis.Read(data, 0, data.Length);
							break;
						}
					default:
						Console.Error.WriteLine("Unknown block type: {0:X2}", blockType);
						_k7Fis.Seek(0, SeekOrigin.End);
						break;
				}
			}
		}

		private void ReadBasicData(int blockLength)
		{
			var data = new byte[11];
			_k7Fis.Read(data, 0, 11);
			var filename = System.Text.Encoding.ASCII.GetString(data);
			Console.WriteLine("file: {0}", filename);
			var fileType = _k7Fis.ReadByte();
			var fileMode = _k7Fis.ReadByte();
			var tmp = _k7Fis.ReadByte();
			var checksum = _k7Fis.ReadByte();

			if (blockLength > 16)
			{
				data = new byte[blockLength - 16];
				_k7Fis.Read(data, 0, data.Length);
			}
		}

		private void ReadBinData(int blockLength)
		{
			if (blockLength == 0) blockLength = 256;
			var data = new byte[blockLength - 1];
			_k7Fis.Read(data, 0, data.Length);

			using (var ms = new MemoryStream(data, 0, data.Length - 1))
			{
				while (ms.Position < ms.Length)
				{
					var br = new BinaryReader(ms);
					var type = br.ReadByte();

					if (type == 0)
					{
						var len = br.ReadByte() << 8 | br.ReadByte();
						var addr = br.ReadByte() << 8 | br.ReadByte();
						var prg = new byte[len];
						br.Read(prg, 0, prg.Length);

						var dsm = new Disassembler();
						using (var ms2 = new MemoryStream(prg))
						{
							var msg = dsm.Disassemble(ms2);
							Console.Write(msg);
						}
					}
					else if (type == 0xFF)
					{
						var len = br.ReadByte() << 8 | br.ReadByte();
						var addr = br.ReadByte() << 8 | br.ReadByte();
					}
					else
					{
						break;
					}
				}
			}
		}
	}
}
