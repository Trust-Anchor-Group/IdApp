using System;
using Waher.Content;
using Waher.Events;
using Waher.Persistence;
using Waher.Persistence.Attributes;
using Xamarin.Forms.Xaml;

namespace IdApp.Test.Serialization
{
	[TypeName(TypeNameSerialization.FullName)]
	public class TestClass
	{
		[ObjectId]
		public Guid ObjectId { get; set; }

		public byte UI8 { get; set; }
		public ushort UI16 { get; set; }
		public uint UI32 { get; set; }
		public ulong UI64 { get; set; }
		public sbyte I8 { get; set; }
		public short I16 { get; set; }
		public int I32 { get; set; }
		public long I64 { get; set; }
		public string S { get; set; }
		public CaseInsensitiveString Cis { get; set; }
		public char Ch { get; set; }
		public byte[] Bin { get; set; }
		public Guid Id { get; set; }
		public TimeSpan TS { get; set; }
		public Duration D { get; set; }
		public object Null { get; set; }
		public bool B { get; set; }
		public float Fl { get; set; }
		public double Db { get; set; }
		public decimal Dc { get; set; }
		public EventType E { get; set; }
		public DateTime TP { get; set; }
		public DateTimeOffset TPO { get; set; }
		public string[] A { get; set; }
		public TestClass Nested { get; set; }

		public override bool Equals(object obj)
		{
			return obj is TestClass Obj &&
				this.ObjectId == Obj.ObjectId &&
				this.UI8 == Obj.UI8 &&
				this.UI16 == Obj.UI16 &&
				this.UI32 == Obj.UI32 &&
				this.UI64 == Obj.UI64 &&
				this.I8 == Obj.I8 &&
				this.I16 == Obj.I16 &&
				this.I32 == Obj.I32 &&
				this.I64 == Obj.I64 &&
				this.S == Obj.S &&
				this.Cis == Obj.Cis &&
				this.Ch == Obj.Ch &&
				Convert.ToBase64String(this.Bin) == Convert.ToBase64String(Obj.Bin) &&
				this.Id == Obj.Id &&
				this.TS == Obj.TS &&
				this.D == Obj.D &&
				this.Null == Obj.Null &&
				this.B == Obj.B &&
				this.Fl == Obj.Fl &&
				this.Db == Obj.Db &&
				this.Dc == Obj.Dc &&
				this.E == Obj.E &&
				this.TP == Obj.TP &&
				this.TPO == Obj.TPO &&
				this.A.Length == Obj.A.Length &&
				(this.Nested?.Equals(Obj.Nested) ?? (Obj.Nested is null));
		}

		private static bool AreEqual<T>(T[] A1, T[] A2)
		{
			int i, c;

			if ((c = A1?.Length ?? 0) != (A2?.Length ?? 0))
				return false;

			for (i = 0; i < c; i++)
			{
				if (!(A1[i]?.Equals(A2[i]) ?? A2[i] is null))
					return false;
			}

			return true;
		}
	}
}
