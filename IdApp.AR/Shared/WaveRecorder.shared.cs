using System.Diagnostics;
using System.Text;

namespace IdApp.AR
{
	internal class WaveRecorder : IDisposable
	{
		BinaryWriter writer;
		int byteCount;
		IAudioStream audioStream;
		bool writeHeadersToStream;

		/// <summary>
		/// Starts recording WAVE format audio from the audio stream.
		/// </summary>
		/// <param name="stream">A <see cref="IAudioStream"/> that provides the audio data.</param>
		/// <param name="recordStream">The stream the audio will be written to.</param>
		/// <param name="writeHeaders"><c>false</c> (default) Write WAV headers to stream at the end of recording.</param>
		public async Task StartRecorder (IAudioStream stream, Stream recordStream, bool writeHeaders = false)
		{
			if (stream == null)
			{
				throw new ArgumentNullException (nameof (stream));
			}

			if (recordStream == null)
			{
				throw new ArgumentNullException (nameof (recordStream));
			}

			this.writeHeadersToStream = writeHeaders;

			try
			{
				//if we're restarting, let's see if we have an existing stream configured that can be stopped
				if (this.audioStream != null)
				{
					await this.audioStream.Stop ();
				}

				this.audioStream = stream;
				this.writer = new BinaryWriter (recordStream, Encoding.UTF8, true);

				this.byteCount = 0;
				this.audioStream.OnBroadcast += this.OnStreamBroadcast;
				this.audioStream.OnActiveChanged += this.StreamActiveChanged;

				if (!this.audioStream.Active)
				{
					await this.audioStream.Start ();
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("Error in WaveRecorder.StartRecorder(): {0}", ex.Message);

				this.StopRecorder();
				throw;
			}
		}

		void StreamActiveChanged (object sender, bool active)
		{
			if (!active)
			{
				this.StopRecorder();
			}
		}

		void OnStreamBroadcast(object sender, byte [] bytes)
		{
			try
			{
				if ((sender is IAudioStream Stream) && !Stream.Paused && (this.writer is not null))
				{
					this.writer.Write(bytes);
					this.byteCount += bytes.Length;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("Error in WaveRecorder.OnStreamBroadcast(): {0}", ex.Message);

				this.StopRecorder();
			}
		}

		/// <summary>
		/// Stops recording WAV audio from the underlying <see cref="IAudioStream"/> and finishes writing the WAV file.
		/// </summary>
		public void StopRecorder ()
		{
			try
			{
				if (this.audioStream != null)
				{
					this.audioStream.OnBroadcast -= this.OnStreamBroadcast;
					this.audioStream.OnActiveChanged -= this.StreamActiveChanged;
				}

				if (this.writer != null)
				{
					if (this.writeHeadersToStream && this.writer.BaseStream.CanWrite && this.writer.BaseStream.CanSeek)
					{
						//now that audio is finished recording, write a WAV/RIFF header at the beginning of the file
						this.writer.Seek (0, SeekOrigin.Begin);
						AudioFunctions.WriteWavHeader(this.writer, this.audioStream.ChannelCount, this.audioStream.SampleRate, this.audioStream.BitsPerSample, this.byteCount);
					}

					this.writer.Dispose (); //this should properly close/dispose the underlying stream as well
					this.writer = null;
				}

				this.audioStream = null;
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("Error during StopRecorder: {0}", ex.Message);
				throw;
			}
		}

		public void Dispose ()
		{
			this.StopRecorder();
		}
	}
}
