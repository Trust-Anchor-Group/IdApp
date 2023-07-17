using System.Text;

namespace IdApp.AR
{
	/// <summary>
	/// Contains functions used to work with audio recording.
	/// </summary>
	public static class AudioFunctions
	{
		private const float max8BitsSigned = byte.MaxValue;
		private const float max8BitsUnsigned = 0xff;
		private const float max16BitsSigned = short.MaxValue;
		private const float max16BitsUnsigned = 0xffff;

		/// <summary>
		/// Writes a WAV file header using the specified details.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to write the WAV header to.</param>
		/// <param name="channelCount">The number of channels in the recorded audio.</param>
		/// <param name="sampleRate">The sample rate of the recorded audio.</param>
		/// <param name="bitsPerSample">The bits per sample of the recorded audio.</param>
		/// <param name="audioLength">The length/byte count of the recorded audio, or -1 if recording is still in progress.</param>
		public static void WriteWavHeader (Stream stream, int channelCount, int sampleRate, int bitsPerSample, int audioLength = -1)
		{
			using BinaryWriter writer = new(stream, Encoding.UTF8);
			WriteWavHeader(writer, channelCount, sampleRate, bitsPerSample, audioLength);
		}

		internal static void WriteWavHeader (BinaryWriter writer, int channelCount, int sampleRate, int bitsPerSample, int audioLength = -1)
		{
			short blockAlign = (short) (channelCount * (bitsPerSample / 8));
			int averageBytesPerSecond = sampleRate * blockAlign;

			if (writer.BaseStream.CanSeek)
			{
				writer.Seek (0, SeekOrigin.Begin);
			}

			//chunk ID
			writer.Write (Encoding.UTF8.GetBytes ("RIFF"));

			if (audioLength > -1)
			{
				writer.Write ((Int32)(audioLength - 8)); // Size of the overall file - 8 bytes, in bytes (32-bit integer).
			}
			else
			{
				writer.Write (audioLength); // -1 (Unkown size)
			}

			//format
			writer.Write (Encoding.UTF8.GetBytes ("WAVE"));

			//subchunk 1 ID
			writer.Write (Encoding.UTF8.GetBytes ("fmt "));

			writer.Write (16); //subchunk 1 (fmt) size
			writer.Write ((short) 1); //PCM audio format

			writer.Write ((short) channelCount);
			writer.Write (sampleRate);
			writer.Write (averageBytesPerSecond);
			writer.Write (blockAlign);
			writer.Write ((short) bitsPerSample);

			//subchunk 2 ID
			writer.Write (Encoding.UTF8.GetBytes ("data"));

			//subchunk 2 (data) size
			writer.Write (audioLength-44);
		}

		// Adapted from http://stackoverflow.com/questions/5800649/detect-silence-when-recording
		internal static float CalculateLevel (byte [] buffer, int readPoint = 0, int leftOver = 0, bool use16Bit = true, bool signed = true, bool bigEndian = false)
		{
			float level;
			int max = 0;

			//bool signed = (RECORDER_AUDIO_ENCODING == Android.Media.Encoding. AudioFormat. Encoding.PCM_SIGNED);
			//bool bigEndian = false;// (format.isBigEndian());

			if (use16Bit)
			{
				for (int i = readPoint; i < buffer.Length - leftOver; i += 2)
				{
					int value;

					// deal with endianness
					int hiByte = (bigEndian ? buffer [i] : buffer [i + 1]);
					int loByte = (bigEndian ? buffer [i + 1] : buffer [i]);

					if (signed)
					{
						short shortVal = (short) hiByte;
						shortVal = (short) ((shortVal << 8) | (byte) loByte);
						value = shortVal;
					}
					else
					{
						value = (hiByte << 8) | loByte;
					}
					max = Math.Max (max, value);
				} // for
			}
			else
			{
				// 8 bit - no endianness issues, just sign
				for (int i = readPoint; i < buffer.Length - leftOver; i++)
				{
					int value;

					if (signed)
					{
						value = buffer [i];
					}
					else
					{
						short shortVal = 0;
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
						shortVal = (short) (shortVal | buffer [i]);
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
						value = shortVal;
					}

					max = Math.Max (max, value);
				} // for
			} // 8 bit
			  // express max as float of 0.0 to 1.0 of max value
			  // of 8 or 16 bits (signed or unsigned)
			if (signed)
			{
				if (use16Bit) { level = (float) max / max16BitsSigned; } else { level = (float) max / max8BitsSigned; }
			}
			else
			{
				if (use16Bit) { level = (float) max / max16BitsUnsigned; } else { level = (float) max / max8BitsUnsigned; }
			}

			return level;
		}

		/// <summary>
		/// The bytes that we get from audiograph is in IEEE float, we need to covert that to 16 bit
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		internal static short FloatToInt16 (float value)
		{
			float f = value * short.MaxValue;
			if (f > short.MaxValue) f = short.MaxValue;
			if (f < short.MinValue) f = short.MinValue;

			return (short) f;
		}
	}
}
