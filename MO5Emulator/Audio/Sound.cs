using System;
using System.Collections.Generic;
using nMO5;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

namespace MO5Emulator.Audio
{
    public class Sound: ISound, IDisposable
    {
        private const int FrameRate = 44100; // 44100 Hz
        private const int NBytes = 1024; // Buffer size
       
        private readonly byte[] _data;
        private readonly IntPtr _device;
        private readonly ContextHandle _context;
        private int _source;
        private Queue<SoundBuffer> _queuedBuffers;

        public Sound()
        {
            _device = Alc.OpenDevice(null);
            ALHelper.CheckError("Failed to OpenDevice.");
            _context = Alc.CreateContext(_device,(int[])null);
            ALHelper.CheckError("Failed to CreateContext.");
            Alc.MakeContextCurrent(_context);
            ALHelper.CheckError("Failed to MakeContextCurrent.");

            _source = AL.GenSource();
            ALHelper.CheckError("Failed to CheckError.");

			_data = new byte[NBytes];
            _queuedBuffers = new Queue<SoundBuffer>();

            AL.Source(_source, ALSourceb.Looping, false);
			ALHelper.CheckError("Failed to set source loop state.");
            AL.SourcePlay(_source);
            ALHelper.CheckError("Failed to play the source.");
        }

        // Copie du buffer de son provenant du 6809 vers le buffer de la carte son
        // Cette fonction est lancée lorsque le buffer 6809 est plein
        public void PlaySound(byte[] soundBuffer)
        {
            for (var i = 0; i < NBytes; i++)
            {
                _data[i/4] = soundBuffer[i];
            }

            var buffer = new SoundBuffer();
            buffer.BufferData(_data, ALFormat.Mono16, NBytes/ 4, FrameRate);

            // Queue the buffer
            AL.SourceQueueBuffer(_source, buffer.Id);
			ALHelper.CheckError("Failed to queue buffer.");
			_queuedBuffers.Enqueue(buffer);

			// If the source has run out of buffers, restart it
			var sourceState = AL.GetSourceState(_source);
            if (sourceState == ALSourceState.Stopped||sourceState == ALSourceState.Initial)
			{
				AL.SourcePlay(_source);
				ALHelper.CheckError("Failed to resume source playback.");
			}
		}

		public void UpdateQueue()
		{
			// Get the completed buffers
			AL.GetError();
            AL.GetSource(_source, ALGetSourcei.BuffersProcessed, out int numBuffers);
            ALHelper.CheckError("Failed to get processed buffer count.");

			// Unqueue them
			if (numBuffers > 0)
			{
				AL.SourceUnqueueBuffers(_source, numBuffers);
				ALHelper.CheckError("Failed to unqueue buffers.");
				for (int i = 0; i < numBuffers; i++)
				{
                    var buffer = _queuedBuffers.Dequeue();
                    buffer.Dispose();
				}
			}
		}

		public void Dispose()
        {
			AL.DeleteSources(1, ref _source);

            Alc.MakeContextCurrent(ContextHandle.Zero);
            Alc.DestroyContext(_context);
            Alc.CloseDevice(_device);
        }
    }
}