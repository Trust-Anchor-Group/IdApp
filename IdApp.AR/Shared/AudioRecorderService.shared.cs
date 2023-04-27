using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace IdApp.AR
{
	/// <summary>
	/// A service that records audio on the device's microphone input.
	/// </summary>
	public partial class AudioRecorderService
	{
		const float nearZero = .00000000001F;
		private readonly WaveRecorder recorder = new();

		private IAudioStream? audioStream;
		private bool audioDetected;
		private Stopwatch? silenceTimer;
		private Stopwatch? startTimer;
		private TaskCompletionSource<string?> recordTask;
		private FileStream fileStream;

		/// <summary>
		/// Gets the details of the underlying audio stream.
		/// </summary>
		/// <remarks>Accessible once <see cref="StartRecording"/> has been called.</remarks>
		public AudioStreamDetails AudioStreamDetails { get; private set; }

		/// <summary>
		/// Gets/sets the desired file path. If null it will be set automatically
		/// to a temporary file.
		/// </summary>
		public string FilePath { get; set; }

		/// <summary>
		/// Gets/sets the preferred sample rate to be used during recording.
		/// </summary>
		/// <remarks>This value may be overridden by platform-specific implementations, e.g. the Android AudioManager will be asked for its preferred sample rate and may override any user-set value here.</remarks>
		public int PreferredSampleRate { get; set; } = 44100;

		/// <summary>
		/// Returns a value indicating if the <see cref="AudioRecorderService"/> is currently recording audio
		/// </summary>
		public bool IsRecording => this.audioStream?.Active ?? false;

		/// <summary>
		/// Returns a value indicating if the <see cref="AudioRecorderService"/> is currently paused
		/// </summary>
		public bool IsPaused => this.IsRecording && (this.audioStream?.Paused ?? false);

		/// <summary>
		/// Returns the total time the audio is recording
		/// </summary>
		public TimeSpan RecordingTime => this.startTimer?.Elapsed ?? TimeSpan.Zero;

		/// <summary>
		/// If <see cref="StopRecordingOnSilence"/> is set to <c>true</c>, this <see cref="TimeSpan"/> indicates the amount of 'silent' time is required before recording is stopped.
		/// </summary>
		/// <remarks>Defaults to 2 seconds.</remarks>
		public TimeSpan AudioSilenceTimeout { get; set; } = TimeSpan.FromSeconds(2);

		/// <summary>
		/// If <see cref="StopRecordingAfterTimeout"/> is set to <c>true</c>, this <see cref="TimeSpan"/> indicates the total amount of time to record audio for before recording is stopped. Defaults to 30 seconds.
		/// </summary>
		/// <seealso cref="StopRecordingAfterTimeout"/>
		public TimeSpan TotalAudioTimeout { get; set; } = TimeSpan.FromSeconds(30);

		/// <summary>
		/// Gets/sets a value indicating if the <see cref="AudioRecorderService"/> should stop recording after silence (low audio signal) is detected.
		/// </summary>
		/// <remarks>Default is `true`</remarks>
		public bool StopRecordingOnSilence { get; set; } = true;

		/// <summary>
		/// Gets/sets a value indicating if the <see cref="AudioRecorderService"/> should stop recording after a certain amount of time.
		/// </summary>
		/// <remarks>Defaults to <c>true</c></remarks>
		/// <seealso cref="TotalAudioTimeout"/>
		public bool StopRecordingAfterTimeout { get; set; } = true;

		/// <summary>
		/// Gets/sets a value indicating the signal threshold that determines silence.  If the recorder is being over or under aggressive when detecting silence, you can alter this value to achieve different results.
		/// </summary>
		/// <remarks>Defaults to .15.  Value should be between 0 and 1.</remarks>
		public float SilenceThreshold { get; set; } = .15f;

		/// <summary>
		/// Gets/sets a value indicating if headers will be written to the file/stream.
		/// </summary>
		/// <remarks>Defaults to <c>true</c></remarks>
		public bool WriteHeaders { get; set; } = true;

		/// <summary>
		/// This event is raised when audio recording is complete and delivers a full filepath to the recorded audio file.
		/// </summary>
		/// <remarks>This event will be raised on a background thread to allow for any further processing needed.  The audio file will be <c>null</c> in the case that no audio was recorded.</remarks>
		public event EventHandler<string?> AudioInputReceived;

		partial void Init();

		/// <summary>
		/// Creates a new instance of the <see cref="AudioRecorderService"/>.
		/// </summary>
		public AudioRecorderService()
		{
			this.Init();
		}

		/// <summary>
		/// Starts recording audio.
		/// </summary>
		/// <param name="recordStream"><c>null</c> (default) Optional stream to write audio data to, if null, a file will be created.</param>
		/// <param name="writeHeaders"><c>false</c> (default) Set to true, to write WAV headers to recordStream after recording. Requires a seekable stream.</param>
		/// <returns>A <see cref="Task"/> that will complete when recording is finished.
		/// The task result will be the path to the recorded audio file, or null if no audio was recorded.</returns>
		public async Task<Task<string?>> StartRecording(Stream? recordStream = null)
		{
			if(recordStream == null)
			{
				this.FilePath ??= await this.GetDefaultFilePath();
				this.fileStream = new FileStream(this.FilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
				recordStream = this.fileStream;
			}

			this.ResetAudioDetection();
			this.OnRecordingStarting();
			this.startTimer = Stopwatch.StartNew();

			await this.InitializeStream(this.PreferredSampleRate);
			await this.recorder.StartRecorder(this.audioStream, recordStream, this.WriteHeaders);

			this.AudioStreamDetails = new AudioStreamDetails
			{
				ChannelCount = this.audioStream.ChannelCount,
				SampleRate = this.audioStream.SampleRate,
				BitsPerSample = this.audioStream.BitsPerSample
			};

			this.recordTask = new TaskCompletionSource<string?>();

			Debug.WriteLine("AudioRecorderService.StartRecording() complete.  Audio is being recorded.");

			return this.recordTask.Task;
		}

		/// <summary>
		/// Gets a new <see cref="Stream"/> to the recording audio file in readonly mode.
		/// </summary>
		/// <returns>A <see cref="Stream"/> object that can be used to read the audio file from the beginning.</returns>
		public Stream GetAudioFileStream()
		{
			//return a new stream to the same audio file, in Read mode
			return new FileStream(this.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		}

		void ResetAudioDetection()
		{
			this.silenceTimer = null;
			this.startTimer = null;
			this.audioDetected = false;
		}

		void AudioStream_OnBroadcast(object sender, byte [] bytes)
		{
			float level = AudioFunctions.CalculateLevel(bytes);

			if (level < nearZero && !this.audioDetected) // discard any initial 0s so we don't jump the gun on timing out
			{
				Debug.WriteLine("level == {0} && !audioDetected", level);
				return;
			}

			if (level > this.SilenceThreshold) // did we find a signal?
			{
				this.audioDetected = true;
				this.silenceTimer = null;

				Debug.WriteLine("AudioStream_OnBroadcast :: {0} :: level > SilenceThreshold :: bytes: {1}; level: {2}", DateTime.Now, bytes.Length, level);
			}
			else // no audio detected
			{
				// see if we've detected 'near' silence for more than <audioTimeout>
				if (this.StopRecordingOnSilence && (this.silenceTimer is not null))
				{
					if (this.silenceTimer.ElapsedMilliseconds > this.AudioSilenceTimeout.TotalMilliseconds)
					{
						this.Timeout($"AudioStream_OnBroadcast :: {DateTime.Now} :: AudioSilenceTimeout exceeded, stopping recording :: Near-silence detected at: {this.silenceTimer}");
						return;
					}
				}
				else
				{
					this.silenceTimer = Stopwatch.StartNew();

					Debug.WriteLine("AudioStream_OnBroadcast :: {0} :: Near-silence detected :: bytes: {1}; level: {2}", DateTime.Now, bytes.Length, level);
				}
			}

			if (this.StopRecordingAfterTimeout && this.startTimer?.ElapsedMilliseconds >= this.TotalAudioTimeout.TotalMilliseconds)
			{
				this.Timeout("AudioStream_OnBroadcast(): TotalAudioTimeout exceeded, stopping recording");
			}
		}

		void Timeout(string reason)
		{
			Debug.WriteLine(reason);
			this.audioStream.OnBroadcast -= this.AudioStream_OnBroadcast; // need this to be immediate or we can try to stop more than once

			// since we're in the middle of handling a broadcast event when an audio timeout occurs, we need to break the StopRecording call on another thread
			//	Otherwise, Bad. Things. Happen.
			_ = Task.Run(() => this.StopRecording());
		}

		/// <summary>
		/// Stops recording audio.
		/// </summary>
		/// <param name="continueProcessing"><c>true</c> (default) to finish recording and raise the <see cref="AudioInputReceived"/> event.
		/// Use <c>false</c> here to stop recording but do nothing further (from an error state, etc.).</param>
		public async Task StopRecording(bool continueProcessing = true)
		{
			if (this.audioStream is not null)
			{
				this.audioStream.Flush(); // allow the stream to send any remaining data
				this.audioStream.OnBroadcast -= this.AudioStream_OnBroadcast;
				try
				{
					await this.audioStream.Stop();
					// WaveRecorder will be stopped as result of stream stopping
				}
				catch(Exception ex)
				{
					Debug.WriteLine("Error in StopRecording: {0}", ex);
				}
			}

			this.fileStream?.Dispose();
			this.startTimer?.Stop();
			this.OnRecordingStopped();

			string? returnedFilePath = this.GetAudioFilePath();
			// complete the recording Task for anything waiting on this
			this.recordTask.TrySetResult(returnedFilePath);

			if (continueProcessing)
			{
				Debug.WriteLine($"AudioRecorderService.StopRecording(): Recording stopped, raising AudioInputReceived event; audioDetected == {this.audioDetected}; filePath == {returnedFilePath}");

				AudioInputReceived?.Invoke(this, returnedFilePath);
			}
		}

		public Task Pause()
		{
			this.startTimer?.Stop();
			return this.audioStream?.Pause() ?? Task.CompletedTask;
		}

		public Task Resume()
		{
			this.startTimer?.Start();
			return this.audioStream?.Resume() ?? Task.CompletedTask;
		}

		private async Task InitializeStream(int sampleRate)
		{
			try
			{
				if (this.audioStream is not null)
				{
					this.audioStream.OnBroadcast -= this.AudioStream_OnBroadcast;

					if (this.audioStream.Paused)
					{
						await this.audioStream.Resume();
					}
				}
				else
				{
					this.audioStream = new AudioStream(sampleRate);
				}

				this.audioStream.OnBroadcast += this.AudioStream_OnBroadcast;
			}
			catch(Exception ex)
			{
				Debug.WriteLine("Error: {0}", ex);
			}
		}

		/// <summary>
		/// Gets the full filepath to the recorded audio file.
		/// </summary>
		/// <returns>The full filepath to the recorded audio file, or null if no audio was detected during the last record.</returns>
		public string? GetAudioFilePath()
		{
			return this.audioDetected ? this.FilePath : null;
		}
	}
}
