using SkiaSharp;
using System;

namespace IdApp.Cv
{
	/// <summary>
	/// Implements a Matrix, basic component for computations in Image Processing and Computer Vision.
	/// </summary>
	/// <typeparam name="T">Type of each pixel.</typeparam>
	public class Matrix<T> : IMatrix
		where T : struct
	{
		private T[] data;
		private int width;
		private int height;
		private int left;
		private int top;
		private int rowSize;
		private int dataSize;
		private int start;
		private int skip;

		private Matrix()
		{
		}

		/// <summary>
		/// Implements a Matrix, basic component for computations in Image Processing and Computer Vision.
		/// </summary>
		/// <typeparam name="T">Type of each pixel.</typeparam>
		/// <param name="Width">Width of Matrix</param>
		/// <param name="Height">Height of Matrix</param>
		public Matrix(int Width, int Height)
			: this(Width, Height, new T[Width * Height])
		{
		}

		/// <summary>
		/// Implements a Matrix, basic component for computations in Image Processing and Computer Vision.
		/// </summary>
		/// <typeparam name="T">Type of each pixel.</typeparam>
		/// <param name="Width">Width of Matrix</param>
		/// <param name="Height">Height of Matrix</param>
		/// <param name="Data">Underlying data.</param>
		public Matrix(int Width, int Height, T[] Data)
		{
			this.dataSize = Width * Height;
			if (this.dataSize != Data.Length)
				throw new ArgumentOutOfRangeException(nameof(Data));

			this.data = Data;
			this.width = Width;
			this.height = Height;
			this.left = 0;
			this.top = 0;
			this.start = 0;
			this.rowSize = Width;
			this.skip = 0;
		}

		/// <summary>
		/// Type of elements in matrix.
		/// </summary>
		public Type ElementType => typeof(T);

		/// <summary>
		/// Underlying data on which the matrix is defined.
		/// </summary>
		public T[] Data => this.data;

		/// <summary>
		/// Width of matrix (number of columns)
		/// </summary>
		public int Width => this.width;

		/// <summary>
		/// Height of matrix (number of rows)
		/// </summary>
		public int Height => this.height;

		/// <summary>
		/// Left offset of matrix in underlying data array.
		/// </summary>
		public int Left => this.left;

		/// <summary>
		/// Top offset of matrix in underlying data array.
		/// </summary>
		public int Top => this.top;

		/// <summary>
		/// Number of elements per row in underlying data array.
		/// </summary>
		public int RowSize => this.rowSize;

		/// <summary>
		/// Total number of elements in underlying data array.
		/// </summary>
		public int DataSize => this.dataSize;

		/// <summary>
		/// Start offset of matrix in underlying data.
		/// </summary>
		public int Start => this.start;

		/// <summary>
		/// Number of elements to skip from the right edge in the underlying data to the left edge of the new row.
		/// </summary>
		public int Skip => this.skip;

		/// <summary>
		/// Direct access to underlying elements in the matix.
		/// </summary>
		/// <param name="X">Zero-based X-coordinate (column)</param>
		/// <param name="Y">Zero-based Y-coordinate (row)</param>
		/// <returns>Pixel/Element</returns>
		public T this[int X, int Y]
		{
			get
			{
				if (X < 0 || X >= this.width)
					throw new ArgumentOutOfRangeException(nameof(X));

				if (Y < 0 || Y >= this.height)
					throw new ArgumentOutOfRangeException(nameof(Y));

				return this.data[(X + this.left) + (Y + this.top) * this.rowSize];
			}

			set
			{
				if (X < 0 || X >= this.width)
					throw new ArgumentOutOfRangeException(nameof(X));

				if (Y < 0 || Y >= this.height)
					throw new ArgumentOutOfRangeException(nameof(Y));

				this.data[(X + this.left) + (Y + this.top) * this.rowSize] = value;
			}
		}

		/// <summary>
		/// Returns a row vector as a matrix.
		/// </summary>
		/// <param name="RowIndex">Zero-based row index.</param>
		/// <returns>Row vector as a matrix.</returns>
		public Matrix<T> Row(int RowIndex)
		{
			if (RowIndex < 0 || RowIndex >= this.height)
				throw new ArgumentOutOfRangeException(nameof(RowIndex));

			return new Matrix<T>()
			{
				data = this.data,
				width = this.width,
				height = 1,
				left = this.left,
				top = this.top + RowIndex,
				rowSize = this.rowSize,
				dataSize = this.dataSize,
				start = this.start + (RowIndex * this.rowSize),
				skip = this.skip
			};
		}

		/// <summary>
		/// Returns a column vector as a matrix.
		/// </summary>
		/// <param name="ColumnIndex">Zero-based column index.</param>
		/// <returns>Column vector as a matrix.</returns>
		public Matrix<T> Column(int ColumnIndex)
		{
			if (ColumnIndex < 0 || ColumnIndex >= this.width)
				throw new ArgumentOutOfRangeException(nameof(ColumnIndex));

			return new Matrix<T>()
			{
				data = this.data,
				width = 1,
				height = this.height,
				left = this.left + ColumnIndex,
				top = this.top,
				rowSize = this.rowSize,
				dataSize = this.dataSize,
				start = this.start + ColumnIndex,
				skip = this.rowSize - 1
			};
		}

		/// <summary>
		/// Returns a region of the matrix, as a new matrix.
		/// </summary>
		/// <param name="Left">Zero-based left offset where the region begins.</param>
		/// <param name="Top">Zero-based top offset where the region begins.</param>
		/// <param name="Width">Width of region</param>
		/// <param name="Height">Height of region</param>
		/// <returns>Column vector as a matrix.</returns>
		public Matrix<T> Region(int Left, int Top, int Width, int Height)
		{
			if (Left < 0 || Left >= this.width)
				throw new ArgumentOutOfRangeException(nameof(Left));

			if (Top < 0 || Top >= this.height)
				throw new ArgumentOutOfRangeException(nameof(Top));

			if (Width <= 0 || Width > this.width - this.left - Left)
				throw new ArgumentOutOfRangeException(nameof(Width));

			if (Height <= 0 || Height > this.height - this.top - Top)
				throw new ArgumentOutOfRangeException(nameof(Height));

			return new Matrix<T>()
			{
				data = this.data,
				width = Width,
				height = Height,
				left = this.left + Left,
				top = this.top + Top,
				rowSize = this.rowSize,
				dataSize = this.dataSize,
				start = this.start + Left + (Top * this.rowSize),
				skip = this.rowSize - Width
			};
		}

		public static IMatrix LoadFromFile(string FileName)
		{
			using (SKBitmap Bmp = SKBitmap.Decode(FileName))
			{
				byte[] Data = Bmp.Bytes;

				switch (Bmp.BytesPerPixel)
				{
					case 1:
						return new Matrix<byte>(Bmp.Width, Bmp.Height, Data);

					case 2:
						int i, j, c = Data.Length;
						ushort[] Bin16 = new ushort[c / 2];
						ushort u;

						i = j = 0;
						while (i < c)
						{
							u = Data[i++];
							u <<= 8;
							u |= Data[i++];
							Bin16[j++] = u;
						}

						return new Matrix<ushort>(Bmp.Width, Bmp.Height, Bin16);

					case 3:
						c = Data.Length;
						uint[] Bin32 = new uint[c / 3];
						uint v;

						i = j = 0;
						while (i < c)
						{
							v = Data[i++];
							v <<= 8;
							v |= Data[i++];
							v <<= 8;
							v |= Data[i++];
							Bin32[j++] = v;
						}

						return new Matrix<uint>(Bmp.Width, Bmp.Height, Bin32);

					case 4:
						c = Data.Length;
						Bin32 = new uint[c / 4];

						i = j = 0;
						while (i < c)
						{
							v = Data[i++];
							v <<= 8;
							v |= Data[i++];
							v <<= 8;
							v |= Data[i++];
							v <<= 8;
							v |= Data[i++];
							Bin32[j++] = v;
						}

						return new Matrix<uint>(Bmp.Width, Bmp.Height, Bin32);

					default:
						throw new ArgumentException("Color type not supported.", nameof(FileName));
				}
			}
		}

	}
}
