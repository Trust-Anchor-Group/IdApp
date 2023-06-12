using AudioToolbox;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace IdApp.AR
{
	internal class AudioStream : IAudioStream
	{
		private const int countAudioBuffers = 3;
		private const int maxBufferSize = 0x50000; // 320 KB
		private const float targetMeasurementTime = 100F; // milliseconds

		private InputAudioQueue? audioQueue;

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
		/// Gets the sample rate.
		/// </summary>
		/// <value>
		/// The sample rate.
		/// </value>
		public int SampleRate
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the channel count.  Currently always 1 (Mono).
		/// </summary>
		/// <value>
		/// The channel count.
		/// </value>
		public int ChannelCount => 1;

		/// <summary>
		/// Gets bits per sample.  Currently always 16 (bits).
		/// </summary>
		public int BitsPerSample => 16;

		/// <summary>
		/// Gets a value indicating if the audio stream is active.
		/// </summary>
		public bool Active => this.audioQueue?.IsRunning ?? false;

		public bool Paused
		{
			get;
			private set;
		}

		/// <summary>
		/// Wrapper function to run success/failure callbacks from an operation that returns an AudioQueueStatus.
		/// </summary>
		/// <param name="bufferFn">The function that returns AudioQueueStatus.</param>
		/// <param name="successAction">The Action to run if the result is AudioQueueStatus.Ok.</param>
		/// <param name="failAction">The Action to run if the result is anything other than AudioQueueStatus.Ok.</param>
		void BufferOperation(Func<AudioQueueStatus> BufferFn, Action? SuccessAction = null, Action<AudioQueueStatus>? FailAction = null)
		{
			AudioQueueStatus Status = BufferFn();

			if (Status == AudioQueueStatus.Ok)
			{
				SuccessAction?.Invoke();
			}
			else
			{
				if (FailAction is not null)
				{
					FailAction(Status);
				}
				else
				{
					throw new Exception($"AudioStream buffer error :: buffer operation returned non - Ok status:: {Status}");
				}
			}
		}

		/// <summary>
		/// Starts the audio stream.
		/// </summary>
		public Task Start()
		{
			try
			{
				if ((this.audioQueue is not null) && !this.Active)
				{
					this.InitAudioQueue();

					this.BufferOperation(this.audioQueue.Start,
						() => OnActiveChanged?.Invoke(this, true),
						Status => throw new Exception($"audioQueue.Start() returned non-OK status: {Status}"));
				}

				return Task.FromResult(true);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Error in AudioStream.Start(): {0}", ex.Message);

				this.Stop();
				throw;
			}
		}

		/// <summary>
		/// Stops the audio stream.
		/// </summary>
		public Task Stop()
		{
			if (this.audioQueue != null)
			{
				this.audioQueue.InputCompleted -= this.QueueInputCompleted;

				if (this.audioQueue.IsRunning)
				{
					this.BufferOperation(() => this.audioQueue.Stop(true),
						() => OnActiveChanged?.Invoke(this, false),
						Status => Debug.WriteLine("AudioStream.Stop() :: audioQueue.Stop returned non OK result: {0}", Status));
				}

				this.audioQueue.Dispose();
				this.audioQueue = null;
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
		/// <param name="sampleRate">Sample rate.</param>
		public AudioStream(int SampleRate)
		{
			this.SampleRate = SampleRate;
		}

		void InitAudioQueue()
		{
			// create our audio queue & configure buffers
			AudioStreamBasicDescription AudioFormat = AudioStreamBasicDescription.CreateLinearPCM(this.SampleRate, (uint)this.ChannelCount, (uint)this.BitsPerSample);

			this.audioQueue = new InputAudioQueue(AudioFormat);
			this.audioQueue.InputCompleted += this.QueueInputCompleted;

			// calculate our buffer size and make sure it's not too big
			int BufferByteSize = (int)(targetMeasurementTime / 1000F/*ms to sec*/ * this.SampleRate * AudioFormat.BytesPerPacket);
			BufferByteSize = BufferByteSize < maxBufferSize ? BufferByteSize : maxBufferSize;

			for (int index = 0; index < countAudioBuffers; index++)
			{
				IntPtr BufferPtr = IntPtr.Zero;

				this.BufferOperation(() => this.audioQueue.AllocateBuffer(BufferByteSize, out BufferPtr), () =>
				{
					AudioStreamPacketDescription[] Description = { };
					this.BufferOperation(() => this.audioQueue.EnqueueBuffer(BufferPtr, BufferByteSize, Description), () => Debug.WriteLine("AudioQueue buffer enqueued :: {0} of {1}", index + 1, countAudioBuffers));
				});
			}
		}

		/// <summary>
		/// Handles iOS audio buffer queue completed message.
		/// </summary>
		/// <param name='Sender'>Sender object</param>
		/// <param name='e'> Input completed parameters.</param>
		void QueueInputCompleted(object Sender, InputCompletedEventArgs InputCompletedArgs)
		{
			try
			{
				// we'll only broadcast if we're actively monitoring audio packets
				if (!this.Active)
				{
					return;
				}

				if (InputCompletedArgs.Buffer.AudioDataByteSize > 0)
				{
					byte[] AudioBytes = new byte [InputCompletedArgs.Buffer.AudioDataByteSize];
					Marshal.Copy(InputCompletedArgs.Buffer.AudioData, AudioBytes, 0, (int)InputCompletedArgs.Buffer.AudioDataByteSize);

					// broadcast the audio data to any listeners
					OnBroadcast?.Invoke(this, AudioBytes);

					// check if active again, because the auto stop logic may stop the audio queue from within this handler!
					if ((this.audioQueue is not null) && this.Active)
					{
						AudioStreamPacketDescription[] Description = { };

						this.BufferOperation(() => this.audioQueue.EnqueueBuffer(InputCompletedArgs.IntPtrBuffer, Description), null,
							Status => {
								Debug.WriteLine("AudioStream.QueueInputCompleted() :: audioQueue.EnqueueBuffer returned non-Ok status :: {0}", Status);
								OnException?.Invoke(this, new Exception($"audioQueue.EnqueueBuffer returned non-Ok status :: {Status}"));
							});
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("AudioStream.QueueInputCompleted() :: Error: {0}", ex.Message);

				OnException?.Invoke(this, new Exception($"AudioStream.QueueInputCompleted() :: Error: {ex.Message}"));
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
