using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Runtime.Profiling;

namespace IdApp.Services.Storage
{
	[Singleton]
	internal sealed class StorageService : ServiceReferences, IStorageService
	{
		private const string objectNamespace = "http://waher.se/Schema/ObjectSerialization.xsd";

		private readonly LinkedList<TaskCompletionSource<bool>> tasksWaiting = new();
		private readonly string dataFolder;
		private FilesProvider databaseProvider;
		private bool? initialized = null;
		private bool started = false;

		/// <summary>
		/// Creates a new instance of the <see cref="StorageService"/> class.
		/// </summary>
		public StorageService()
		{
			string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			this.dataFolder = Path.Combine(appDataFolder, "Data");
		}

		/// <summary>
		/// Folder for database.
		/// </summary>
		public string DataFolder => this.dataFolder;

		#region LifeCycle management

		/// <inheritdoc />
		public async Task Init(ProfilerThread Thread, CancellationToken? cancellationToken)
		{
			lock (this.tasksWaiting)
			{
				if (this.started)
					return;

				this.started = true;
			}

			Thread?.Start();
			try
			{
				Thread?.NewState("Provider");

				if (Database.HasProvider)
					this.databaseProvider = Database.Provider as FilesProvider;

				if (this.databaseProvider is null)
				{
					this.databaseProvider = await this.CreateDatabaseFile(Thread);

					Thread?.NewState("CheckDB");
					await this.databaseProvider.RepairIfInproperShutdown(string.Empty);

					await this.databaseProvider.Start();
				}

				Thread?.NewState("Register");

				if (this.databaseProvider is not null)
				{
					Database.Register(this.databaseProvider, false);
					this.InitDone(true);
					Thread?.Stop();
					return;
				}
			}
			catch (Exception e1)
			{
				e1 = Log.UnnestException(e1);
				Thread?.Exception(e1);
				this.LogService.LogException(e1);
			}

			try
			{
				/* On iOS the UI is not initialised at this point, need to fuind another solution
				Thread?.NewState("UI");
				if (await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["DatabaseIssue"], LocalizationResourceManager.Current["DatabaseCorruptInfoText"], LocalizationResourceManager.Current["RepairAndContinue"], LocalizationResourceManager.Current["ContinueAnyway"]))
				*/
				//TODO: when UI is ready, show an alert that the database was reset due to unrecoverable error
				//TODO: say to close the application in a controlled manner
				{
					try
					{
						Thread?.NewState("Delete");
						Directory.Delete(this.dataFolder, true);

						Thread?.NewState("Recreate");
						this.databaseProvider = await this.CreateDatabaseFile(Thread);

						Thread?.NewState("Repair");
						await this.databaseProvider.RepairIfInproperShutdown(string.Empty);

						await this.databaseProvider.Start();

						if (!Database.HasProvider)
						{
							Thread?.NewState("Register");
							Database.Register(this.databaseProvider, false);
							this.InitDone(true);
							Thread?.Stop();
							return;
						}
					}
					catch (Exception e3)
					{
						e3 = Log.UnnestException(e3);
						Thread?.Exception(e3);
						this.LogService.LogException(e3);

						await App.Stop();
						/*
						Thread?.NewState("UI");
						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["DatabaseIssue"], LocalizationResourceManager.Current["DatabaseRepairFailedInfoText"], LocalizationResourceManager.Current["Ok"]);
						*/
					}
				}

				this.InitDone(false);
			}
			finally
			{
				Thread?.Stop();
			}
		}

		private void InitDone(bool Result)
		{
			lock (this.tasksWaiting)
			{
				this.initialized = Result;

				foreach (TaskCompletionSource<bool> Wait in this.tasksWaiting)
					Wait.TrySetResult(Result);

				this.tasksWaiting.Clear();
			}
		}

		/// <inheritdoc />
		public Task<bool> WaitInitDone()
		{
			lock (this.tasksWaiting)
			{
				if (this.initialized.HasValue)
					return Task.FromResult<bool>(this.initialized.Value);

				TaskCompletionSource<bool> Wait = new();
				this.tasksWaiting.AddLast(Wait);

				return Wait.Task;
			}
		}

		/// <inheritdoc />
		public async Task Shutdown()
		{
			lock (this.tasksWaiting)
			{
				this.initialized = null;
				this.started = false;
			}

			try
			{
				if (this.databaseProvider is not null)
				{
					Database.Register(new NullDatabaseProvider(), false);
					await this.databaseProvider.Flush();
					await this.databaseProvider.Stop();
					this.databaseProvider = null;
				}
			}
			catch (Exception e)
			{
				this.LogService.LogException(e);
			}
		}

		private Task<FilesProvider> CreateDatabaseFile(ProfilerThread Thread)
		{
			FilesProvider.AsyncFileIo = false;  // Asynchronous file I/O induces a long delay during startup on mobile platforms. Why??

			return FilesProvider.CreateAsync(this.dataFolder, "Default", 8192, 10000, 8192, Encoding.UTF8,
				(int)Constants.Timeouts.Database.TotalMilliseconds, this.CryptoService.GetCustomKey, Thread);
		}

		#endregion

		public async Task Insert(object obj)
		{
			await Database.Insert(obj);
			await Database.Provider.Flush();
		}

		public async Task Update(object obj)
		{
			await Database.Update(obj);
			await Database.Provider.Flush();
		}

		public Task<T> FindFirstDeleteRest<T>() where T : class
		{
			return Database.FindFirstDeleteRest<T>();
		}

		public Task<T> FindFirstIgnoreRest<T>() where T : class
		{
			return Database.FindFirstIgnoreRest<T>();
		}

		public Task Export(IDatabaseExport exportOutput)
		{
			return Database.Export(exportOutput);
		}

		/// <summary>
		/// Flags the database for repair, so that the next time the app is opened, the database will be repaired.
		/// </summary>
		public void FlagForRepair()
		{
			this.DeleteFile("Start.txt");
			this.DeleteFile("Stop.txt");
		}

		private void DeleteFile(string FileName)
		{
			try
			{
				FileName = Path.Combine(this.dataFolder, FileName);

				if (File.Exists(FileName))
					File.Delete(FileName);
			}
			catch (Exception)
			{
				// Ignore, to avoid infinite loops if event log has an inconsistency.
			}
		}

		/// <summary>
		/// Serializes an object to an XML string.
		/// </summary>
		/// <param name="Object">Object to serialize.</param>
		/// <returns>XML Serialization.</returns>
		public async Task<string> Serialize(object Object)
		{
			GenericObject GenObject = await Database.Generalize(Object);
			StringBuilder Xml = new();

			Xml.Append("<Object xmlns=");
			Xml.Append(objectNamespace);

			Xml.Append("' type='");
			Xml.Append(XML.Encode(GenObject.TypeName));
			Xml.Append("' collection='");
			Xml.Append(XML.Encode(GenObject.CollectionName));
			Xml.Append("' id='");
			Xml.Append(XML.Encode(GenObject.ObjectId.ToString()));
			Xml.Append("'>");

			foreach (KeyValuePair<string, object> Property in GenObject.Properties)
				await this.SerializeProperty(Property.Key, Property.Value, Xml);

			Xml.Append("</Object>");

			return Xml.ToString();
		}

		private async Task SerializeProperty(string Name, object Value, StringBuilder Xml)
		{
			string Tag;
			string ValueStr;

			if (Value is sbyte)
			{
				Tag = "i8";
				ValueStr = Value.ToString();
			}
			else if (Value is short)
			{
				Tag = "i16";
				ValueStr = Value.ToString();
			}
			else if (Value is int)
			{
				Tag = "i32";
				ValueStr = Value.ToString();
			}
			else if (Value is long)
			{
				Tag = "i64";
				ValueStr = Value.ToString();
			}
			else if (Value is byte)
			{
				Tag = "ui8";
				ValueStr = Value.ToString();
			}
			else if (Value is ushort)
			{
				Tag = "ui16";
				ValueStr = Value.ToString();
			}
			else if (Value is uint)
			{
				Tag = "ui32";
				ValueStr = Value.ToString();
			}
			else if (Value is ulong)
			{
				Tag = "ui64";
				ValueStr = Value.ToString();
			}
			else if (Value is bool b)
			{
				Tag = "b";
				ValueStr = CommonTypes.Encode(b);
			}
			else if (Value is float Single)
			{
				Tag = "fl";
				ValueStr = CommonTypes.Encode(Single);
			}
			else if (Value is double Double)
			{
				Tag = "db";
				ValueStr = CommonTypes.Encode(Double);
			}
			else if (Value is decimal Decimal)
			{
				Tag = "dc";
				ValueStr = CommonTypes.Encode(Decimal);
			}
			else if (Value is Enum e)
			{
				Xml.Append("<e");

				if (!string.IsNullOrEmpty(Name))
				{
					Xml.Append(" name='");
					Xml.Append(XML.Encode(Name));
					Xml.Append('\'');
				}

				Xml.Append(" value='");
				Xml.Append(XML.Encode(e.ToString()));
				Xml.Append("' type='");
				Xml.Append(XML.Encode(e.GetType().FullName));
				Xml.Append("'/>");

				return;
			}
			else if (Value is string s)
			{
				Tag = "s";
				ValueStr = s;
			}
			else if (Value is CaseInsensitiveString cis)
			{
				Tag = "cis";
				ValueStr = cis.Value;
			}
			else if (Value is char ch)
			{
				Tag = "ch";
				ValueStr = new string(ch, 1);
			}
			else if (Value is byte[] Bin)
			{
				Tag = "b64";
				ValueStr = Convert.ToBase64String(Bin);
			}
			else if (Value is Guid Id)
			{
				Tag = "id";
				ValueStr = Id.ToString();
			}
			else if (Value is DateTime TP)
			{
				Tag = "dt";
				ValueStr = XML.Encode(TP);
			}
			else if (Value is DateTimeOffset TPO)
			{
				Tag = "dto";
				ValueStr = XML.Encode(TPO);
			}
			else if (Value is TimeSpan TS)
			{
				Tag = "t";
				ValueStr = TS.ToString();
			}
			else if (Value is Duration D)
			{
				Tag = "d";
				ValueStr = D.ToString();
			}
			else if (Value is null)
			{
				Xml.Append("<n");

				if (!string.IsNullOrEmpty(Name))
				{
					Xml.Append(" name='");
					Xml.Append(XML.Encode(Name));
					Xml.Append('\'');
				}

				Xml.Append("/>");

				return;
			}
			else if (Value is Array A)
			{
				Xml.Append("<a");

				if (!string.IsNullOrEmpty(Name))
				{
					Xml.Append(" name='");
					Xml.Append(XML.Encode(Name));
					Xml.Append('\'');
				}

				Xml.Append(" type='");
				Xml.Append(XML.Encode(A.GetType().GetElementType().FullName));
				Xml.Append("'>");

				foreach (object Item in A)
					await this.SerializeProperty(string.Empty, Item, Xml);

				Xml.Append("</a>");

				return;
			}
			else
			{
				GenericObject GenObject = await Database.Generalize(Value);

				Xml.Append("<o");

				if (!string.IsNullOrEmpty(Name))
				{
					Xml.Append(" name='");
					Xml.Append(XML.Encode(Name));
					Xml.Append('\'');
				}

				Xml.Append("' type='");
				Xml.Append(XML.Encode(GenObject.TypeName));
				Xml.Append("'>");

				foreach (KeyValuePair<string, object> Property in GenObject.Properties)
					await this.SerializeProperty(Property.Key, Property.Value, Xml);

				Xml.Append("</o>");

				return;
			}

			Xml.Append('<');
			Xml.Append(Tag);

			if (!string.IsNullOrEmpty(Name))
			{
				Xml.Append(" name='");
				Xml.Append(XML.Encode(Name));
				Xml.Append('\'');
			}

			Xml.Append(" value='");
			Xml.Append(XML.Encode(ValueStr));
			Xml.Append("'/>");
		}

		/// <summary>
		/// Deserializes an object from XML
		/// </summary>
		/// <param name="Xml">XML String</param>
		/// <returns>Generic object representation</returns>
		public Task<GenericObject> Deserialize(string Xml)
		{
			XmlDocument Doc = new();
			Doc.LoadXml(Xml);
			return this.Deserialize(Doc);
		}

		/// <summary>
		/// Deserializes an object from XML
		/// </summary>
		/// <param name="Xml">XML</param>
		/// <returns>Generic object representation</returns>
		public Task<GenericObject> Deserialize(XmlDocument Xml)
		{
			return this.Deserialize(Xml.DocumentElement);
		}

		/// <summary>
		/// Deserializes an object from XML
		/// </summary>
		/// <param name="Xml">XML</param>
		/// <returns>Generic object representation</returns>
		public async Task<GenericObject> Deserialize(XmlElement Xml)
		{
			if (Xml.LocalName != "Object" || Xml.NamespaceURI != objectNamespace)
				throw new Exception("Invalid Object XML.");

			string TypeName = XML.Attribute(Xml, "type");
			string CollectionName = XML.Attribute(Xml, "collection");
			Guid Id = Guid.Parse(XML.Attribute(Xml, "id"));
			IEnumerable<KeyValuePair<string, object>> Properties = await this.DeserializeProperties(Xml);

			GenericObject Result = new(CollectionName, TypeName, Id, Properties);

			return Result;
		}

		private async Task<IEnumerable<KeyValuePair<string, object>>> DeserializeProperties(XmlElement Xml)
		{
			LinkedList<KeyValuePair<string, object>> Properties = new();

			foreach (XmlNode N in Xml.ChildNodes)
			{
				if (N is XmlElement E)
				{
					string Name = XML.Attribute(E, "name");
					object Value = await this.ParseValue(E);

					Properties.AddLast(new KeyValuePair<string, object>(Name, Value));
				}
			}

			return Properties;
		}

		private async Task<object> ParseValue(XmlElement E)
		{
			string ValueStr = XML.Attribute(E, "value");

			switch (E.LocalName)
			{
				case "i8": return sbyte.Parse(ValueStr);
				case "i16": return short.Parse(ValueStr);
				case "i32": return int.Parse(ValueStr);
				case "i64": return long.Parse(ValueStr);
				case "ui8": return byte.Parse(ValueStr);
				case "ui16": return ushort.Parse(ValueStr);
				case "ui32": return uint.Parse(ValueStr);
				case "ui64": return ulong.Parse(ValueStr);
				case "s": return ValueStr;
				case "cis": return new CaseInsensitiveString(ValueStr);
				case "ch": return ValueStr[0];
				case "b64": return Convert.FromBase64String(ValueStr);
				case "id": return Guid.Parse(ValueStr);
				case "t": return TimeSpan.Parse(ValueStr);
				case "d": return Duration.Parse(ValueStr);
				case "n": return null;

				case "b":
					if (!CommonTypes.TryParse(ValueStr, out bool b))
						throw new FormatException("Invalid boolean property.");

					return b;

				case "fl":
					if (!CommonTypes.TryParse(ValueStr, out float fl))
						throw new FormatException("Invalid Single property.");

					return fl;

				case "db":
					if (!CommonTypes.TryParse(ValueStr, out double db))
						throw new FormatException("Invalid Double property.");

					return db;

				case "dc":
					if (!CommonTypes.TryParse(ValueStr, out decimal dc))
						throw new FormatException("Invalid Decimal property.");

					return dc;

				case "e":
					string Type = XML.Attribute(E, "type");
					Type T = Types.GetType(Type)
						?? throw new Exception("Type not found:" + Type);

					return Enum.Parse(T, ValueStr);

				case "dt":
					if (!XML.TryParse(ValueStr, out DateTime TP))
						throw new FormatException("Invalid DateTime property.");

					return TP;

				case "dto":
					if (!XML.TryParse(ValueStr, out DateTimeOffset TPO))
						throw new FormatException("Invalid DateTimeOffset property.");

					return TPO;

				case "a":
					Type = XML.Attribute(E, "type");
					T = Types.GetType(Type)
						?? throw new Exception("Type not found:" + Type);

					Type ListType = typeof(List<>);
					Type GenericListType = ListType.MakeGenericType(T);
					System.Collections.IList List = (System.Collections.IList)Types.Instantiate(ListType);

					foreach (XmlNode N in E.ChildNodes)
					{
						if (N is XmlElement E2)
							List.Add(await this.ParseValue(E2));
					}

					MethodInfo ToArray = GenericListType.GetMethod("ToArray");
					return ToArray.Invoke(List, Types.NoParameters);

				case "o":
					Type = XML.Attribute(E, "type");
					_ = Types.GetType(Type)
						?? throw new Exception("Type not found:" + Type);

					return new GenericObject(string.Empty, Type, Guid.Empty, await this.DeserializeProperties(E));

				default:
					throw new Exception("Unrecognized type element: " + E.NamespaceURI + "#" + E.LocalName);
			}
		}

		/// <summary>
		/// Deserializes a serialized object in XML, into an object instance.
		/// </summary>
		/// <typeparam name="T">Type of object to deserialize.</typeparam>
		/// <param name="Xml">XML String</param>
		/// <returns>Object instance.</returns>
		public Task<T> Deserialize<T>(string Xml)
		{
		}

		/// <summary>
		/// Deserializes a serialized object in XML, into an object instance.
		/// </summary>
		/// <typeparam name="T">Type of object to deserialize.</typeparam>
		/// <param name="Xml">XML</param>
		/// <returns>Object instance.</returns>
		public Task<T> Deserialize<T>(XmlDocument Xml)
		{
			return this.Deserialize<T>(Xml.DocumentElement);
		}

		/// <summary>
		/// Deserializes a serialized object in XML, into an object instance.
		/// </summary>
		/// <typeparam name="T">Type of object to deserialize.</typeparam>
		/// <param name="Xml">XML</param>
		/// <returns>Object instance.</returns>
		public async Task<T> Deserialize<T>(XmlElement Xml)
		{
			GenericObject Obj = await this.Deserialize(Xml);
			T Result = (T)this.Ungeneralize(Obj);

			return Result;
		}

		private object Ungeneralize(GenericObject Obj)
		{
			Type T2 = Types.GetType(Obj.TypeName) ?? throw new Exception("Type not found: " + Obj.TypeName);
			object Result = Types.Instantiate(T2, false);

			this.SetProperties(Result, Obj);

			return Result;
		}

		private void SetProperties(object Object, GenericObject GenObj)
		{
			Type ObjectType = Object.GetType();

			foreach (KeyValuePair<string, object> P in GenObj.Properties)
			{
				object Value = P.Value;

				if (Value is GenericObject ChildGenObj)
					Value = this.Ungeneralize(ChildGenObj);

				PropertyInfo PI = ObjectType.GetRuntimeProperty(P.Key);
				if (PI is not null)
				{
					PI.SetValue(Object, Value);
					continue;
				}

				FieldInfo FI = ObjectType.GetRuntimeField(P.Key);
				if (FI is not null)
				{
					FI.SetValue(Object, Value);
					continue;
				}

				throw new Exception("Property not found: " + P.Key);
			}
		}

	}
}
