using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MO5Emulator.Cheats
{
	class CheatSerializer
	{
		private Regex regex = new Regex(@"([^:]+):([0-9A-F]+);([0-9]);([0-9A-F]+)");

		public List<Cheat> Load(string path)
		{
			var cheats = new List<Cheat>();
			using (var file = File.OpenRead(path))
			using (var reader = new StreamReader(file))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					var m = regex.Match(line);
					if (m.Success)
					{
						var desc = m.Groups[1].Value;
						var address = int.Parse(m.Groups[2].Value, System.Globalization.NumberStyles.HexNumber);
						var format = int.Parse(m.Groups[3].Value) == 2 ? ByteFormat.Two : ByteFormat.One;
						var value = int.Parse(m.Groups[4].Value, System.Globalization.NumberStyles.HexNumber);
						cheats.Add(new Cheat(desc, address, value, format));
					}
				}
			}
			return cheats;
		}

		public void Save(string path, List<Cheat> cheats)
		{
			using (var file = File.OpenWrite(path))
			using (var writer = new StreamWriter(file))
			{
				foreach (var cheat in cheats)
				{
					writer.WriteLine("{0}:{1:X4};{2};{3:X}", cheat.Description, cheat.Address, cheat.Format == ByteFormat.One ? 1 : 2, cheat.Value);
				}
			}
		}
	}
}
