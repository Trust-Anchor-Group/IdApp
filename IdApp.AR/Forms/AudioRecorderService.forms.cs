using System.IO;
using System.Threading.Tasks;

namespace IdApp.AR
{
	public partial class AudioRecorderService
	{
		Task<string> GetDefaultFilePath ()
		{
			return Task.FromResult(string.Empty);
		}

		void OnRecordingStarting ()
		{
		}

		void OnRecordingStopped ()
		{
		}
	}
}
