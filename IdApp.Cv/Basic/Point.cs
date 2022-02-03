namespace IdApp.Cv.Basic
{
	/// <summary>
	/// Represents a point in an image.
	/// </summary>
	public struct Point
	{
		/// <summary>
		/// Represents a point in an image.
		/// </summary>
		/// <param name="X">X-Coordinate</param>
		/// <param name="Y">Y-Coordinate</param>
		public Point(int X, int Y)
		{
			this.X = X;
			this.Y = Y;
		}

		/// <summary>
		/// X-Coordinate
		/// </summary>
		public int X;

		/// <summary>
		/// Y-Coordinate
		/// </summary>
		public int Y;
	}
}
