using IdApp.Services.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Events;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;
using Waher.Security.JWT;

namespace IdApp.Test.Serialization
{
	[TestClass]
	public class SerializationTests
	{
		private IStorageService storageService;

		[AssemblyInitialize]
		public static async Task AssemblyInitialize(TestContext _)
		{
			Types.Initialize(
				typeof(App).Assembly,
				typeof(SerializationTests).Assembly,
				typeof(Database).Assembly,
				typeof(ObjectSerializer).Assembly,
				typeof(FilesProvider).Assembly,
				typeof(RuntimeSettings).Assembly,
				typeof(JwtFactory).Assembly);

			await Types.StartAllModules(10000);
		}

		[AssemblyCleanup]
		public static async Task AssemblyCleanup()
		{
			await Types.StopAllModules();
		}

		[TestInitialize]
		public async Task TestInitialize()
		{
			this.storageService = Types.Instantiate<IStorageService>(false);
			await this.storageService.Init(null, null);
		}

		[TestCleanup]
		public async Task TestCleanup()
		{
			await this.storageService.Shutdown();
			this.storageService = null;
		}
		
		[TestMethod]
		public async Task Test_01_Serialization()
		{
			TestClass Obj = this.CreateTestClass();
			string Xml = await this.storageService.Serialize(Obj);

			Console.Out.WriteLine(Xml);
		}

		private TestClass CreateTestClass()
		{
			return new TestClass()
			{
				ObjectId = Guid.NewGuid(),
				UI8 = byte.MaxValue,
				UI16 = ushort.MaxValue,
				UI32 = uint.MaxValue,
				UI64 = ulong.MaxValue,
				I8 = sbyte.MaxValue,
				I16 = short.MaxValue,
				I32 = int.MaxValue,
				I64 = long.MaxValue,
				S = "Kilroy was here",
				Cis = new CaseInsensitiveString("Hello WORLD"),
				Ch = 'a',
				Bin = Encoding.UTF8.GetBytes("Hello World"),
				Id = Guid.NewGuid(),
				TS = TimeSpan.FromHours(12),
				D = Duration.Parse("P1Y2M3DT4H5M6S"),
				Null = null,
				B = true,
				Fl = float.MaxValue,
				Db = double.MaxValue,
				Dc = decimal.MaxValue,
				E = EventType.Notice,
				TP = DateTime.Now,
				TPO = DateTimeOffset.Now,
				A = new string[] { "Kilroy", "was", "here" },
				Nested = new TestClass()
				{
					ObjectId = Guid.NewGuid(),
					UI8 = byte.MinValue,
					UI16 = ushort.MinValue,
					UI32 = uint.MinValue,
					UI64 = ulong.MinValue,
					I8 = sbyte.MinValue,
					I16 = short.MinValue,
					I32 = int.MinValue,
					I64 = long.MinValue,
					S = "Kilroy was here",
					Cis = new CaseInsensitiveString("Hello WORLD"),
					Ch = 'a',
					Bin = Encoding.UTF8.GetBytes("Hello World"),
					Id = Guid.NewGuid(),
					TS = TimeSpan.FromHours(12),
					D = Duration.Parse("P1Y2M3DT4H5M6S"),
					Null = null,
					B = true,
					Fl = float.MinValue,
					Db = double.MinValue,
					Dc = decimal.MinValue,
					E = EventType.Notice,
					TP = DateTime.Now,
					TPO = DateTimeOffset.Now,
					A = new string[] { "Kilroy", "was", "here" },
					Nested = null,
				},
			};
		}
	}
}
