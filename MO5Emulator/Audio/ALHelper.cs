using System;
using OpenTK.Audio.OpenAL;

namespace MO5Emulator.Audio
{
	internal static class ALHelper
	{
		[System.Diagnostics.Conditional("DEBUG")]
		[System.Diagnostics.DebuggerHidden]
		public static void CheckError(string message = "", params object[] args)
		{
			ALError error;
			if ((error = AL.GetError()) != ALError.NoError)
			{
				if (args != null && args.Length > 0)
					message = string.Format(message, args);

				throw new InvalidOperationException(message + " (Reason: " + AL.GetErrorString(error) + ")");
			}
		}
	}
}
