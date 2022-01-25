using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IdApp.DeviceSpecific.Nfc.Extensions
{
	/// <summary>
	/// Contains NFC Extensions for Basic Access Control.
	/// 
	/// Reference:
	/// https://www.icao.int/publications/Documents/9303_p11_cons_en.pdf
	/// </summary>
	public static class BasicAccessControl
	{
		/// <summary>
		/// Derives Basic Access Control Keys from the second row of the 
		/// Machine-Readable string in passport (MRZ).
		/// </summary>
		/// <param name="MRZ">Machine-Readable text.</param>
		/// <param name="Info">Parsed Document Information.</param>
		/// <returns>If the string could be parsed.</returns>
		public static bool ParseMrz(string MRZ, out DocumentInformation Info)
		{
			Match M = td2_mrz_nr9charsplus.Match(MRZ);
			if (M.Success)
			{
				Info = AssembleInfo2(M);
				return !(Info is null);
			}

			M = td2_mrz_nr9chars.Match(MRZ);
			if (M.Success)
			{
				Info = AssembleInfo1(M);
				return !(Info is null);
			}

			M = td1_mrz_nr9charsplus.Match(MRZ);
			if (M.Success)
			{
				Info = AssembleInfo2(M);
				return !(Info is null);
			}

			M = td1_mrz_nr9chars.Match(MRZ);
			if (M.Success)
			{
				Info = AssembleInfo1(M);
				return !(Info is null);
			}

			// TODO: Checks

			Info = null;
			return false;
		}

		private static DocumentInformation AssembleInfo2(Match M)
		{
			DocumentInformation Result = AssembleInfo(M);
			Result.DocumentNumber = M.Groups["Nr1"].Value + M.Groups["Nr2"].Value;
			CalcMrzInfo(Result, M);
			return Result.Country == M.Groups["Country2"].Value ? Result : null;
		}

		private static DocumentInformation AssembleInfo1(Match M)
		{
			DocumentInformation Result = AssembleInfo(M);
			Result.DocumentNumber = M.Groups["Nr"].Value;
			CalcMrzInfo(Result, M);
			return Result.Country == M.Groups["Country2"].Value ? Result : null;
		}

		private static void CalcMrzInfo(DocumentInformation Info, Match M)
		{
			Info.MRZ_Information = Info.DocumentNumber + M.Groups["NrCheck"].Value +
				Info.DateOfBirth + M.Groups["PnrCheck"].Value +
				Info.ExpiryDate + M.Groups["ExpiryCheck"].Value;
		}

		private static DocumentInformation AssembleInfo(Match M)
		{
			return new DocumentInformation()
			{
				DocumentType = M.Groups["DocType"].Value,
				Country = M.Groups["Country1"].Value,
				LastNames = M.Groups["LNames"].Value.Split('<'),
				FirstNames = M.Groups["FNames"].Value.Split('<'),
				Gender = M.Groups["Gender"].Value,
				DocumentNumber = M.Groups["Nr"].Value,
				DateOfBirth = M.Groups["Pnr"].Value,
				ExpiryDate = M.Groups["Expires"].Value
			};
		}

		private static readonly Regex td2_mrz_nr9charsplus = new Regex(@"^(?'DocType'.)<(?'Country1'\w{3})(?'LNames'[^<]+(<[^<]+)*)<<(?'FNames'[^<]+(<[^<]+)*)<*\n(?'Nr1'[^<]{9})<(?'Country2'\w{3})(?'Pnr'[^<]*)(?'PnrCheck'[^<])(?'Gender'[MF])(?'Expires'[^<]{6})(?'ExpiryCheck'[^<])(?'Nr2'[^<]*)(?'NrCheck'[^<])<.*$", RegexOptions.Multiline);
		private static readonly Regex td2_mrz_nr9chars = new Regex(@"^(?'DocType'.)<(?'Country1'\w{3})(?'LNames'[^<]+(<[^<]+)*)<<(?'FNames'[^<]+(<[^<]+)*)<*\n(?'Nr'.{9})(?'NrCheck'[^<])(?'Country2'\w{3})(?'Pnr'[^<]{6})(?'PnrCheck'[^<])(?'Gender'[MF])(?'Expires'[^<]{6})(?'ExpiryCheck'[^<])<.*$", RegexOptions.Multiline);
		private static readonly Regex td1_mrz_nr9charsplus = new Regex(@"^(?'DocType'.)<(?'Country1'\w{3})(?'Nr1'[^<]{9})<(?'Nr2'.{3})(?'NrCheck'[^<])<+\n(?'Pnr'[^<]{6})(?'PnrCheck'[^<])(?'Gender'[MF])(?'Expires'[^<]{6})(?'ExpiryCheck'[^<])(?'Country2'\w{3})<*.\n(?'LNames'[^<]+(<[^<]+)*)<<(?'FNames'[^<]+(<[^<]+)*)<*$", RegexOptions.Multiline);
		private static readonly Regex td1_mrz_nr9chars = new Regex(@"^(?'DocType'.)<(?'Country1'\w{3})(?'Nr'.{9})(?'NrCheck'.)<+\n(?'Pnr'[^<]{6})(?'PnrCheck'[^<])(?'Gender'[MF])(?'Expires'[^<]{6})(?'ExpiryCheck'[^<])(?'Country2'\w{3})<*.\n(?'LNames'[^<]+(<[^<]+)*)<<(?'FNames'[^<]+(<[^<]+)*)<*$", RegexOptions.Multiline);

		/// <summary>
		/// Request Challenge (§D.3)
		/// </summary>
		/// <param name="TagInterface"></param>
		/// <returns>Challenge</returns>
		public static async Task<byte[]> RequestChallenge(this IIsoDepInterface TagInterface)
		{
			byte[] Command = new byte[]
			{
				0x00,	// CLA
				0x84,	// INS
				0x00,	// P1
				0x00,	// P2
				0x08	// Le
			};

			byte[] Response = await TagInterface.ExecuteCommand(Command);
			if (Response.Length != 10 || Response[8] != 0x90 || Response[9] != 0x00)
				return null;

			byte[] Challenge = new byte[8];
			Array.Copy(Response, 0, Challenge, 0, 8);

			return Response;
		}
	}
}
