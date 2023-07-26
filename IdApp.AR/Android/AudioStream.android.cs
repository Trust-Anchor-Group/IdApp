using Android.Media;
using Waher.Events;

namespace IdApp.AR
{
	internal class AudioStream : IAudioStream
	{
		readonly int bufferSize;
		readonly ChannelIn channels = ChannelIn.Mono;
		readonly Encoding audioFormat = Encoding.Pcm16bit;

		/// <summary>
		/// The audio source.
		/// </summary>
		private AudioRecord? audioSource;

		/// <summary>
		/// Occurs when new audio has been streamed.
		/// </summary>
		public event EventHandler<byte[]>? OnBroadcast;

		/// <summary>
		/// Occurs when the audio stream active status changes.
		/// </summary>
		public event EventHandler<bool>? OnActiveChanged;

		/// <summary>
		/// Occurs when there's an error while capturing audio.
		/// </summary>
		public event EventHandler<Exception>? OnException;

		/// <summary>
		/// The default device.
		/// </summary>
		public static readonly AudioSource DefaultDevice = AudioSource.Mic;

		/// <summary>
		/// Gets the sample rate.
		/// </summary>
		/// <value>
		/// The sample rate.
		/// </value>
		public int SampleRate { get; private set; } = 44100;

		/// <summary>
		/// Gets bits per sample.
		/// </summary>
		public int BitsPerSample => (this.audioSource?.AudioFormat == Encoding.Pcm16bit) ? 16 : 8;

		/// <summary>
		/// Gets the channel count.
		/// </summary>
		/// <value>
		/// The channel count.
		/// </value>
		public int ChannelCount => this.audioSource?.ChannelCount ?? 0;

		/// <summary>
		/// Gets a value indicating if the audio stream is active.
		/// </summary>
		public bool Active => this.audioSource?.RecordingState == RecordState.Recording;

		public bool Paused
		{
			get;
			private set;
		}

		void Init()
		{
			this.Stop(); // just in case

			this.audioSource = new AudioRecord(DefaultDevice, this.SampleRate, this.channels, this.audioFormat, this.bufferSize);

			if (this.audioSource.State == State.Uninitialized)
			{
				throw new Exception("Unable to successfully initialize AudioStream; reporting State.Uninitialized.  If using an emulator, make sure it has access to the system microphone.");
			}
		}

		/// <summary>
		/// Starts the audio stream.
		/// </summary>
		public Task Start()
		{
			try
			{
				if ((this.audioSource is not null) && !this.Active)
				{
					// not sure this does anything or if should be here... inherited via copied code ¯\_(ツ)_/¯
					Android.OS.Process.SetThreadPriority(Android.OS.ThreadPriority.UrgentAudio);

					this.Init();

					this.audioSource.StartRecording();

					OnActiveChanged?.Invoke(this, true);

					Task.Run(this.Record);
				}

				return Task.FromResult(true);
			}
			catch (Exception ex)
			{
				Log.Critical(ex, "Error in AudioStream.Start");

				this.Stop();
				throw;
			}
		}

		/// <summary>
		/// Stops the audio stream.
		/// </summary>
		public Task Stop()
		{
			if ((this.audioSource is not null) && this.Active)
			{
				this.audioSource.Stop();
				this.audioSource.Release();

				OnActiveChanged?.Invoke(this, false);
			}
			else // just in case
			{
				this.audioSource?.Release();
			}

			return Task.FromResult(true);
		}

		/// <summary>
		/// Pauses the audio stream.
		/// </summary>
		public Task Pause()
		{
			this.Flush();
			this.Paused = true;
			return Task.CompletedTask;
		}

		/// <summary>
		/// Resumes the audio stream.
		/// </summary>
		public Task Resume()
		{
			this.Flush();
			this.Paused = false;
			return Task.CompletedTask;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AudioStream"/> class.
		/// </summary>
		/// <param name="SampleRate">Sample rate.</param>
		/// <param name="Channels">The <see cref="ChannelIn"/> value representing the number of channels to record.</param>
		/// <param name="AudioFormat">The format of the recorded audio.</param>
		public AudioStream(int SampleRate = 44100, ChannelIn Channels = ChannelIn.Mono, Encoding AudioFormat = Encoding.Pcm16bit)
		{
			this.bufferSize = AudioRecord.GetMinBufferSize(SampleRate, Channels, AudioFormat);

			if (this.bufferSize < 0)
			{
				throw new Exception("Invalid buffer size calculated; audio settings used may not be supported on this device");
			}

			this.SampleRate = SampleRate;
			this.channels = Channels;
			this.audioFormat = AudioFormat;
		}

		/// <summary>
		/// Record from the microphone and broadcast the buffer.
		/// </summary>
		async Task Record()
		{
			byte [] data = new byte [this.bufferSize];
			int readFailureCount = 0;
			int readResult = 0;

			while (this.Active)
			{
				try
				{
					// not sure if this is even a good idea, but we'll try to allow a single bad read, and past that shut it down
					if (readFailureCount > 1)
					{
						await this.Stop();
						break;
					}

					readResult = this.audioSource?.Read(data, 0, this.bufferSize) ?? 0; // this can block if there are no bytes to read

					// readResult should == the # bytes read, except a few special cases
					if (readResult > 0)
					{
						readFailureCount = 0;
						OnBroadcast?.Invoke(this, data);
					}
					else
					{
						switch (readResult)
						{
							case (int)TrackStatus.ErrorInvalidOperation:
							case (int)TrackStatus.ErrorBadValue:
							case (int)TrackStatus.ErrorDeadObject:
								await this.Stop();
								break;
							//case (int)TrackStatus.Error:
							default:
								readFailureCount++;
								break;
						}
					}
				}
				catch (Exception ex)
				{
					Log.Critical(ex, "Error in Android AudioStream.Record");

					readFailureCount++;
					OnException?.Invoke (this, ex);
				}
			}
		}

		/// <summary>
		/// Flushes any audio bytes in memory but not yet broadcast out to any listeners.
		/// </summary>
		public void Flush()
		{
			// not needed for this implementation
		}
	}
}
