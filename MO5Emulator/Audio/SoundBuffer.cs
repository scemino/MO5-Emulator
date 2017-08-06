using System;
using OpenTK.Audio.OpenAL;

namespace MO5Emulator.Audio
{
	public class SoundBuffer : IDisposable
	{
		private int _buffer;

		public int Id => _buffer;

		public SoundBuffer()
		{
			_buffer = AL.GenBuffer();
            //Console.WriteLine("Gen buf #"+_buffer);
			ALHelper.CheckError("Failed to GenBuffers.");
		}

        public void BufferData(byte[] data, ALFormat format, int size, int freq)
		{
			AL.BufferData(_buffer, format, data, size, freq);
			ALHelper.CheckError("Failed to fill buffer.");
		}

		public void Dispose()
		{
			//Console.WriteLine("Del buf #" + _buffer);
			AL.DeleteBuffer(_buffer);
            ALHelper.CheckError("Failed to DeleteBuffer buffer.");
		}
	}
}
