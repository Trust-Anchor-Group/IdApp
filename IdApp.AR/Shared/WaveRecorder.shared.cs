using System.Text;
using Waher.Events;

namespace IdApp.AR
{
	internal class WaveRecorder : IDisposable
	{
		private IAudioStream? audioStream;
		private BinaryWriter? writer;
		private bool writeHeadersToStream;
		private int byteCount;

		/// <summary>
		/// Starts recording WAVE format audio from the audio stream.
		/// </summary>
		/// <param name="AudioStream">A <see cref="IAudioStream"/> that provides the audio data.</param>
		/// <param name="RecordStream">The stream the audio will be written to.</param>
		/// <param name="WriteHeaders"><c>false</c> (default) Write WAV headers to stream at the end of recording.</param>
		public async Task StartRecorder(IAudioStream AudioStream, Stream RecordStream, bool WriteHeaders = false)
		{
			if (AudioStream is null)
			{
				throw new ArgumentNullException(nameof(AudioStream));
			}

			if (RecordStream is null)
			{
				throw new ArgumentNullException(nameof(RecordStream));
			}

			this.writeHeadersToStream = WriteHeaders;

			try
			{
				//if we're restarting, let's see if we have an existing stream configured that can be stopped
				if (this.audioStream is not null)
				{
					await this.audioStream.Stop();
				}

				this.audioStream = AudioStream;
				this.audioStream.OnBroadcast += this.OnStreamBroadcast;
				this.audioStream.OnActiveChanged += this.StreamActiveChanged;

				this.writer = new BinaryWriter(RecordStream, Encoding.UTF8, true);
				this.byteCount = 0;

				if (!this.audioStream.Active)
				{
					await this.audioStream.Start();
				}
			}
			catch (Exception ex)
			{
				Log.Critical(ex, "Error in WaveRecorder.StartRecorder");

				this.StopRecorder();
				throw;
			}
		}

		private void StreamActiveChanged(object Sender, bool Active)
		{
			if (!Active)
			{
				this.StopRecorder();
			}
		}

		private void OnStreamBroadcast(object Sender, byte[] Bytes)
		{
			try
			{
				if ((Sender is IAudioStream Stream) && !Stream.Paused && (this.writer is not null))
				{
					this.writer.Write(Bytes);
					this.byteCount += Bytes.Length;
				}
			}
			catch (Exception ex)
			{
				Log.Critical(ex, "Error in WaveRecorder.OnStreamBroadcast");

				this.StopRecorder();
			}
		}

		/// <summary>
		/// Stops recording WAV audio from the underlying <see cref="IAudioStream"/> and finishes writing the WAV file.
		/// </summary>
		public void StopRecorder()
		{
			try
			{
				if (this.audioStream is not null)
				{
					this.audioStream.OnBroadcast -= this.OnStreamBroadcast;
					this.audioStream.OnActiveChanged -= this.StreamActiveChanged;

					if (this.writer is not null)
					{
						if (this.writeHeadersToStream && this.writer.BaseStream.CanWrite && this.writer.BaseStream.CanSeek)
						{
							//now that audio is finished recording, write a WAV/RIFF header at the beginning of the file
							this.writer.Seek(0, SeekOrigin.Begin);
							AudioFunctions.WriteWavHeader(this.writer, this.audioStream.ChannelCount, this.audioStream.SampleRate, this.audioStream.BitsPerSample, this.byteCount);
						}

						this.writer.Dispose(); //this should properly close/dispose the underlying stream as well
						this.writer = null;
					}

					this.audioStream = null;
				}
			}
			catch (Exception ex)
			{
				Log.Critical(ex, "Error during StopRecorder");
				throw;
			}
		}

		public void Dispose()
		{
			this.StopRecorder();
		}
	}
}
