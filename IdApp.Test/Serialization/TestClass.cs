using System;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Persistence;
using Waher.Persistence.Attributes;

namespace IdApp.Test.Serialization
{
	[TypeName(TypeNameSerialization.FullName)]
	public class TestClass
	{
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
			if (obj is not TestClass Obj)
				return false;

			if (this.UI8 != Obj.UI8)
				return false;

			if (this.UI16 != Obj.UI16)
				return false;

			if (this.UI32 != Obj.UI32)
				return false;

			if (this.UI64 != Obj.UI64)
				return false;

			if (this.I8 != Obj.I8)
				return false;

			if (this.I16 != Obj.I16)
				return false;

			if (this.I32 != Obj.I32)
				return false;

			if (this.I64 != Obj.I64)
				return false;

			if (this.S != Obj.S)
				return false;

			if (this.Cis != Obj.Cis)
				return false;

			if (this.Ch != Obj.Ch)
				return false;

			if (Convert.ToBase64String(this.Bin) != Convert.ToBase64String(Obj.Bin))
				return false;

			if (this.Id != Obj.Id)
				return false;

			if (this.TS != Obj.TS)
				return false;

			if (this.D != Obj.D)
				return false;

			if (this.Null != Obj.Null)
				return false;

			if (this.B != Obj.B)
				return false;

			if (this.Fl != Obj.Fl)
				return false;

			if (this.Db != Obj.Db)
				return false;

			if (this.Dc != Obj.Dc)
				return false;

			if (this.E != Obj.E)
				return false;

			if (XML.Encode(this.TP) != XML.Encode(Obj.TP))
				return false;

			if (XML.Encode(this.TPO) != XML.Encode(Obj.TPO))
				return false;

			if (!AreEqual(this.A, Obj.A))
				return false;

			if ((this.Nested is null) ^ (Obj.Nested is null))
				return false;

			if (this.Nested is null)
				return true;

			return this.Nested.Equals(Obj.Nested);
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

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}
