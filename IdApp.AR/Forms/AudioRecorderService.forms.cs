using System.IO;
using System.Threading.Tasks;

namespace IdApp.AR
{
	public partial class AudioRecorderService
	{
		Task<string> GetDefaultFilePath ()
		{
			return Task.FromResult (Path.Combine (Path.GetTempPath (), DefaultFileName));
		}

		void OnRecordingStarting ()
		{
		}

		void OnRecordingStopped ()
		{
		}
	}
}
