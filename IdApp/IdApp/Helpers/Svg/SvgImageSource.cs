using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IdApp.Helpers.Svg
{
	/// <summary>
	/// Svg image source.
	/// </summary>
	public class SvgImageSource : ImageSource
	{
		private static Assembly assemblyCache;

		/// <summary>
		/// Initialize native specific
		/// </summary>
		public static float ScreenScale = 1;

		/// <summary>
		/// The stream func property.
		/// </summary>
		public static BindableProperty StreamFuncProperty =
			BindableProperty.Create(
				nameof(StreamFunc),
				typeof(Func<CancellationToken, Task<Stream>>),
				typeof(SvgImageSource),
				default(Func<CancellationToken, Task<Stream>>),
				defaultBindingMode: BindingMode.OneWay
			);

		/// <summary>
		/// Gets or sets the stream func.
		/// </summary>
		/// <value>The stream func.</value>
		public Func<CancellationToken, Task<Stream>> StreamFunc
		{
			get { return (Func<CancellationToken, Task<Stream>>)this.GetValue(StreamFuncProperty); }
			set { this.SetValue(StreamFuncProperty, value); }
		}

		/// <summary>
		/// The source property.
		/// </summary>
		public static BindableProperty SourceProperty =
			BindableProperty.Create(
				nameof(Source),
				typeof(string),
				typeof(SvgImageSource),
				default(string),
				defaultBindingMode: BindingMode.OneWay
			);

		/// <summary>
		/// Gets or sets the source.
		/// </summary>
		/// <value>The source.</value>
		public string Source
		{
			get { return (string)this.GetValue(SourceProperty); }
			set { this.SetValue(SourceProperty, value); }
		}

		/// <summary>
		/// The width property.
		/// </summary>
		public static BindableProperty WidthProperty =
			BindableProperty.Create(
				nameof(Width),
				typeof(double),
				typeof(SvgImageSource),
				default(double),
				defaultBindingMode: BindingMode.OneWay
			);

		/// <summary>
		/// Gets or sets the width.
		/// </summary>
		/// <value>The width.</value>
		public double Width
		{
			get { return (double)this.GetValue(WidthProperty); }
			set { this.SetValue(WidthProperty, value); }
		}

		/// <summary>
		/// The height property.
		/// </summary>
		public static BindableProperty HeightProperty =
			BindableProperty.Create(
				nameof(Height),
				typeof(double),
				typeof(SvgImageSource),
				default(double),
				defaultBindingMode: BindingMode.OneWay
			);

		/// <summary>
		/// Gets or sets the height.
		/// </summary>
		/// <value>The height.</value>
		public double Height
		{
			get { return (double)this.GetValue(HeightProperty); }
			set { this.SetValue(HeightProperty, value); }
		}

		/// <summary>
		/// The color property.
		/// </summary>
		public static BindableProperty ColorProperty =
			BindableProperty.Create(
				nameof(Color),
				typeof(Color),
				typeof(SvgImageSource),
				default(Color),
				defaultBindingMode: BindingMode.OneWay
			);

		/// <summary>
		/// Gets or sets the color.
		/// </summary>
		/// <value>The color.</value>
		public Color Color
		{
			get { return (Color)this.GetValue(ColorProperty); }
			set { this.SetValue(ColorProperty, value); }
		}

		/// <summary>
		/// Registers the assembly.
		/// </summary>
		/// <param name="TypeHavingResource">Type having resource.</param>
		public static void RegisterAssembly(Type TypeHavingResource = null)
		{
			if (TypeHavingResource is null)
			{
                assemblyCache = Assembly.GetCallingAssembly();
			}
			else
			{
				assemblyCache = TypeHavingResource.GetTypeInfo().Assembly;
			}
		}

		/// <summary>
		/// Froms the svg.
		/// </summary>
		/// <returns>The svg.</returns>
		/// <param name="Resource">Resource.</param>
		/// <param name="Width">Width.</param>
		/// <param name="Height">Height.</param>
		/// <param name="Color">Color.</param>
		public static ImageSource FromSvgResource(string Resource, double Width, double Height, Color Color = default)
		{
            assemblyCache ??= Assembly.GetCallingAssembly();

			if (assemblyCache is null)
			{
				return null;
			}

			return new SvgImageSource { StreamFunc = GetResourceStreamFunc(Resource), Source = Resource, Width = Width, Height = Height, Color = Color };
		}

		/// <summary>
		/// Froms the svg.
		/// </summary>
		/// <returns>The svg.</returns>
		/// <param name="Resource">Resource.</param>
		/// <param name="Color">Color.</param>
		public static ImageSource FromSvgResource(string Resource, Color Color = default)
		{
            assemblyCache ??= Assembly.GetCallingAssembly();

			if (assemblyCache is null)
			{
				return null;
			}

			return new SvgImageSource { StreamFunc = GetResourceStreamFunc(Resource), Source = Resource, Color = Color };

		}

		/// <summary>
		/// Froms the svg stream.
		/// </summary>
		/// <returns>The svg stream.</returns>
		/// <param name="StreamFunc">Stream func.</param>
		/// <param name="Width">Width.</param>
		/// <param name="Height">Height.</param>
		/// <param name="Color">Color.</param>
		/// <param name="Key">Key.</param>
		public static ImageSource FromSvgStream(Func<Stream> StreamFunc, double Width, double Height, Color Color, string Key = null)
		{
			Key ??= StreamFunc.GetHashCode().ToString();
			return new SvgImageSource { StreamFunc = Token => Task.Run(StreamFunc), Width = Width, Height = Height, Color = Color };
		}

		static Func<CancellationToken, Task<Stream>> GetResourceStreamFunc(string Resource)
		{
			if (Resource is null)
			{
				return null;
			}

			return Token => Task.Run(() => assemblyCache.GetManifestResourceStream(Resource), Token);
		}

		/// <summary>
		/// Ons the property changed.
		/// </summary>
		/// <param name="PropertyName">Property name.</param>
		protected override void OnPropertyChanged(string PropertyName)
		{
			if (PropertyName == StreamFuncProperty.PropertyName)
			{
				this.OnSourceChanged();
			}
			else if (PropertyName == SourceProperty.PropertyName)
			{
				if (string.IsNullOrEmpty(this.Source))
				{
					return;
				}

				this.StreamFunc = GetResourceStreamFunc(this.Source);
			}

			base.OnPropertyChanged(PropertyName);
		}

		/// <summary>
		/// Creates a stream that contains an image
		/// </summary>
		/// <param name="UserToken">Cancellation token</param>
		public virtual async Task<Stream> GetImageStreamAsync(CancellationToken UserToken)
		{
			this.OnLoadingStarted();
			UserToken.Register(this.CancellationTokenSource.Cancel);

			Stream ImageStream = null;
			try
			{
				using (Stream Stream = await this.StreamFunc(this.CancellationTokenSource.Token).ConfigureAwait(false))
				{
					if (Stream is null)
					{
						this.OnLoadingCompleted(false);
						return null;
					}

					ImageStream = await SvgUtility.CreateImage(Stream, this.Width, this.Height, this.Color);
				}

				this.OnLoadingCompleted(false);
			}
			catch (OperationCanceledException)
			{
				this.OnLoadingCompleted(true);
				throw;
			}

			return ImageStream;
		}
	}
}
