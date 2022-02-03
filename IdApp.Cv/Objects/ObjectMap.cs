using IdApp.Cv.Basic;
using System.Collections.Generic;

namespace IdApp.Cv.Objects
{
	/// <summary>
	/// Contains an object map of contents in an image.
	/// </summary>
	public class ObjectMap : Matrix<ushort>
	{
		private readonly Dictionary<ushort, ObjectInformation> objects = new Dictionary<ushort, ObjectInformation>();
		private readonly IMatrix image;

		/// <summary>
		/// Contains an object map of contents in an image.
		/// </summary>
		/// <param name="M">Image matrix</param>
		/// <param name="Threshold">Threshold to use when identifying objects.</param>
		public ObjectMap(Matrix<float> M, float Threshold)
			: base(M.Width, M.Height)
		{
			this.image = Image;

			int y, h = M.Height;
			int x, w = M.Width;
			int SrcIndex = M.Start;
			int SrcSkip = M.Skip;
			int DestIndex = 0;
			float[] Src = M.Data;
			ushort[] Dest = this.Data;
			float v;
			ObjectInformation Info;
			ushort Obj;

			for (y = 0; y < h; y++, SrcIndex += SrcSkip)
			{
				for (x = 0; x < w; x++, DestIndex++)
				{
					v = Src[SrcIndex++];
					if (v >= Threshold)
					{
						Obj = Dest[DestIndex];
						if (Obj == 0)
						{
							Obj = (ushort)this.objects.Count;
							Info = new ObjectInformation(Obj, x, y, this);
							this.objects[Obj] = Info;
							this.FloodFill(x, y, M, Threshold, Info);
						}
					}
				}
			}
		}

		private void FloodFill(int x, int y, Matrix<float> M, float Threshold, ObjectInformation Info)
		{
			int SrcIndex = M.StartIndex(0, y);
			int DestIndex = this.StartIndex(0, y);
			float[] Src = M.Data;
			ushort[] Dest = this.Data;
			int x1 = x;
			int x2 = x;
			int w = M.Width;
			int h = M.Height - 1;
			int SrcRow = M.RowSize;
			int DestRow = this.RowSize;
			ushort Nr = (ushort)(Info.Nr + 1);
			int si, di;

			while (x1 > 0 && Src[SrcIndex + x1 - 1] >= Threshold && Dest[DestIndex + x1 - 1] == 0)
				x1--;

			while (x2 < w - 1 && Src[SrcIndex + x2 + 1] >= Threshold && Dest[DestIndex + x2 + 1] == 0)
				x2++;

			Info.AddPixels(x1, x2, y);

			x = x1;
			di = DestIndex + x;
			Dest[di] = (ushort)(Nr | 0x8000);
			x++;
			di++;

			while (x < x2)
			{
				Dest[di] = Nr;
				x++;
				di++;
			}

			Dest[di] = (ushort)(Nr | 0x8000);

			for (x = x1, si = SrcIndex + x, di = DestIndex + x; x <= x2; x++, si++, di++)
			{
				if (y > 0)
				{
					if (Src[si - SrcRow] >= Threshold)
					{
						if (Dest[di - DestRow] == 0)
							this.FloodFill(x, y - 1, M, Threshold, Info);
					}
					else
						Dest[di] |= 0x4000;
				}

				if (y < h)
				{
					if (Src[si + SrcRow] >= Threshold)
					{
						if (Dest[di + DestRow] == 0)
							this.FloodFill(x, y + 1, M, Threshold, Info);
					}
					else
						Dest[di] |= 0x4000;
				}
			}
		}


		/// <summary>
		/// Contains an object map of contents in an image.
		/// </summary>
		/// <param name="M">Image matrix</param>
		/// <param name="Threshold">Threshold to use when identifying objects.</param>
		public ObjectMap(Matrix<int> M, int Threshold)
			: base(M.Width, M.Height)
		{
			this.image = Image;

			int y, h = M.Height;
			int x, w = M.Width;
			int SrcIndex = M.Start;
			int SrcSkip = M.Skip;
			int DestIndex = 0;
			int[] Src = M.Data;
			ushort[] Dest = this.Data;
			int v;
			ObjectInformation Info;
			ushort Obj;

			for (y = 0; y < h; y++, SrcIndex += SrcSkip)
			{
				for (x = 0; x < w; x++, DestIndex++)
				{
					v = Src[SrcIndex++];
					if (v >= Threshold)
					{
						Obj = Dest[DestIndex];
						if (Obj == 0)
						{
							Obj = (ushort)this.objects.Count;
							Info = new ObjectInformation(Obj, x, y, this);
							this.objects[Obj] = Info;
							this.FloodFill(x, y, M, Threshold, Info);
						}
					}
				}
			}
		}

		private void FloodFill(int x, int y, Matrix<int> M, int Threshold, ObjectInformation Info)
		{
			int SrcIndex = M.StartIndex(0, y);
			int DestIndex = this.StartIndex(0, y);
			int[] Src = M.Data;
			ushort[] Dest = this.Data;
			int x1 = x;
			int x2 = x;
			int w = M.Width;
			int h = M.Height - 1;
			int SrcRow = M.RowSize;
			int DestRow = this.RowSize;
			ushort Nr = (ushort)(Info.Nr + 1);
			int si, di;

			while (x1 > 0 && Src[SrcIndex + x1 - 1] >= Threshold && Dest[DestIndex + x1 - 1] == 0)
				x1--;

			while (x2 < w - 1 && Src[SrcIndex + x2 + 1] >= Threshold && Dest[DestIndex + x2 + 1] == 0)
				x2++;

			Info.AddPixels(x1, x2, y);

			x = x1;
			di = DestIndex + x;
			Dest[di] = (ushort)(Nr | 0x8000);
			x++;
			di++;

			while (x < x2)
			{
				Dest[di] = Nr;
				x++;
				di++;
			}

			Dest[di] = (ushort)(Nr | 0x8000);

			for (x = x1, si = SrcIndex + x, di = DestIndex + x; x <= x2; x++, si++, di++)
			{
				if (y > 0)
				{
					if (Src[si - SrcRow] >= Threshold)
					{
						if (Dest[di - DestRow] == 0)
							this.FloodFill(x, y - 1, M, Threshold, Info);
					}
					else
						Dest[di] |= 0x4000;
				}

				if (y < h)
				{
					if (Src[si + SrcRow] >= Threshold)
					{
						if (Dest[di + DestRow] == 0)
							this.FloodFill(x, y + 1, M, Threshold, Info);
					}
					else
						Dest[di] |= 0x4000;
				}
			}
		}

		/// <summary>
		/// Underlying image.
		/// </summary>
		public IMatrix Image => this.image;

		/// <summary>
		/// Number of objects in map.
		/// </summary>
		public int NrObjects => this.objects.Count;

		/// <summary>
		/// Gets a reference to information about an object in the object map.
		/// </summary>
		/// <param name="Nr">Zero-based object index.</param>
		/// <returns>Object information.</returns>
		public ObjectInformation this[ushort Nr] => this.objects[Nr];

		internal Point[] FindContour(int X0, int Y0, int Nr)
		{
			List<Point> Result = new List<Point>();
			LinkedList<Rec> History = new LinkedList<Rec>();
			Point P;
			int Dir = 0;    // Right
			int x = X0;
			int y = Y0;
			int i;
			int Index = this.StartIndex(X0, Y0);
			int Index1;
			int RowSize = this.RowSize;
			int w = this.Width;
			int h = this.Height;
			ushort[] Data = this.Data;
			ushort v;
			int dx, dy;
			int x1, y1;

			Nr++;

			Result.Add(P = new Point(X0, Y0));
			History.AddLast(new Rec(P, Dir));
			Data[Index] &= 0x3fff;

			while (true)
			{
				for (i = 0; i < 8; i++)
				{
					dx = DirectionX[Dir];
					dy = DirectionY[Dir];

					x1 = x + dx;
					y1 = y + dy;

					if (x1 >= 0 && x1 < w && y1 >= 0 && y1 < h)
					{
						Index1 = Index + dx + dy * RowSize;
						v = Data[Index1];
						if ((v & 0x3fff) == Nr && (v & 0xc000) > 0)
						{
							x = x1;
							y = y1;
							Index = Index1;
							Result.Add(P = new Point(x, y));
							History.AddLast(new Rec(P, Dir));
							Data[Index] &= 0x3fff;
							break;
						}
					}

					Dir = (Dir + 1) & 7;
				}

				if (i >= 8)
				{
					if (History.Last is null)
						break;
					else
					{
						Rec Rec = History.Last.Value;
						History.RemoveLast();
						x = Rec.P.X;
						y = Rec.P.Y;
						Dir = Rec.Dir;
						Index = this.StartIndex(x, y);
					}
				}
			}

			return Result.ToArray();
		}

		private static readonly int[] DirectionX = new int[] { 1,  1,  0, -1, -1, -1, 0, 1 };
		private static readonly int[] DirectionY = new int[] { 0, -1, -1, -1,  0,  1, 1, 1 };

		private class Rec
		{
			public Rec(Point P, int Dir)
			{
				this.P = P;
				this.Dir = Dir;
			}

			public Point P;
			public int Dir;
		}

	}
}
