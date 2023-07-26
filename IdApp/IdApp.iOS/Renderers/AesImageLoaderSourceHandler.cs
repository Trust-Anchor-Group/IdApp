using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using Xamarin.Forms.Internals;
using PreserveAttribute = Foundation.PreserveAttribute;
using Xamarin.Forms;
using IdApp.Helpers;
using Xamarin.Forms.Platform.iOS;
using CoreAnimation;
using ImageIO;
using System.Globalization;
using System;
using IdApp.iOS.Renderers;

[assembly: ExportImageSourceHandler(typeof(AesImageSource), typeof(AesImageLoaderSourceHandler))]
namespace IdApp.iOS.Renderers
{
	public class AesImageLoaderSourceHandler : IImageSourceHandler, IAnimationSourceHandler
	{
		[Preserve(Conditional = true)]
		public AesImageLoaderSourceHandler()
		{
		}

		public async Task<UIImage> LoadImageAsync(ImageSource Imagesource, CancellationToken CancelationToken = default, float Scale = 1f)
		{
			UIImage Image = null;
			AesImageSource ImageLoader = Imagesource as AesImageSource;

			if (ImageLoader?.Uri is not null)
			{
				using Stream StreamImage = await ImageLoader.GetStreamAsync(CancelationToken).ConfigureAwait(false);

				if (StreamImage is not null)
				{
					Image = UIImage.LoadFromData(NSData.FromStream(StreamImage), Scale);
				}
			}

			if (Image is null)
			{
				Log.Warning(nameof(ImageLoaderSourceHandler), "Could not load image: {0}", ImageLoader);
			}

			return Image;
		}

		public async Task<FormsCAKeyFrameAnimation> LoadImageAnimationAsync(ImageSource ImageSource, CancellationToken CancelationToken = default, float Scale = 1)
		{
			FormsCAKeyFrameAnimation Animation = await ImageAnimationHelper.CreateAnimationFromUriImageSourceAsync(ImageSource as UriImageSource, CancelationToken).ConfigureAwait(false);

			if (Animation is null)
			{
				Log.Warning(nameof(FileImageSourceHandler), "Could not find image: {0}", ImageSource);
			}

			return Animation;
		}
	}

	internal class ImageAnimationHelper
	{
		class ImageDataHelper : IDisposable
		{
			private readonly NSObject[] keyFrames = null;
			private readonly NSNumber[] keyTimes = null;
			private readonly double[] delayTimes = null;
			private readonly int imageCount = 0;
			private double totalAnimationTime = 0.0f;
			private bool disposed = false;

			public ImageDataHelper(nint ImageCount)
			{
				if (ImageCount <= 0)
				{
					throw new ArgumentException();
				}

				this.keyFrames = new NSObject[ImageCount];
				this.keyTimes = new NSNumber[ImageCount + 1];
				this.delayTimes = new double[ImageCount];
				this.imageCount = (int)ImageCount;
				this.Width = 0;
				this.Height = 0;
			}

			public int Width { get; set; }
			public int Height { get; set; }

			public void AddFrameData(int index, CGImageSource imageSource)
			{
				if (index < 0 || index >= this.imageCount || index >= imageSource.ImageCount)
				{
					throw new ArgumentException();
				}

				double DelayTime = 0.1f;
				CoreGraphics.CGImageProperties ImageProperties = imageSource.GetProperties(index, null);

				using NSObject GifImageProperties = ImageProperties?.Dictionary[ImageIO.CGImageProperties.GIFDictionary];
				using NSObject UnclampedDelayTimeValue = GifImageProperties?.ValueForKey(ImageIO.CGImageProperties.GIFUnclampedDelayTime);
				using NSObject DelayTimeValue = GifImageProperties?.ValueForKey(ImageIO.CGImageProperties.GIFDelayTime);

				if (UnclampedDelayTimeValue is not null)
				{
					double.TryParse(UnclampedDelayTimeValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out DelayTime);
				}
				else if (DelayTimeValue is not null)
				{
					double.TryParse(DelayTimeValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out DelayTime);
				}

				// Frame delay compability adjustment.
				if (DelayTime <= 0.02f)
				{
					DelayTime = 0.1f;
				}

				using CoreGraphics.CGImage Image = imageSource.CreateImage(index, null);

				if (Image is not null)
				{
					this.Width = Math.Max(this.Width, (int)Image.Width);
					this.Height = Math.Max(this.Height, (int)Image.Height);

					this.keyFrames[index]?.Dispose();
					this.keyFrames[index] = null;

					this.keyFrames[index] = NSObject.FromObject(Image);
					this.delayTimes[index] = DelayTime;
					this.totalAnimationTime += DelayTime;
				}
			}

			public FormsCAKeyFrameAnimation CreateKeyFrameAnimation()
			{
				if (this.totalAnimationTime <= 0.0f)
				{
					return null;
				}

				double CurrentTime = 0.0f;

				for (int i = 0; i < this.imageCount; i++)
				{
					this.keyTimes[i]?.Dispose();
					this.keyTimes[i] = null;

					this.keyTimes[i] = new NSNumber(CurrentTime);
					CurrentTime += this.delayTimes[i] / this.totalAnimationTime;
				}

				// When using discrete animation there should be one more keytime
				// than values, with 1.0f as value.
				this.keyTimes[this.imageCount] = new NSNumber(1.0f);

				return new FormsCAKeyFrameAnimation
				{
					Values = this.keyFrames,
					KeyTimes = this.keyTimes,
					Duration = this.totalAnimationTime,
					CalculationMode = CAAnimation.AnimationDiscrete
				};
			}

			protected virtual void Dispose(bool disposing)
			{
				if (this.disposed)
				{
					return;
				}

				for (int i = 0; i < this.imageCount; i++)
				{
					this.keyFrames[i]?.Dispose();
					this.keyFrames[i] = null;

					this.keyTimes[i]?.Dispose();
					this.keyTimes[i] = null;
				}

				this.keyTimes[this.imageCount]?.Dispose();
				this.keyTimes[this.imageCount] = null;

				this.disposed = true;
			}

			public void Dispose()
			{
				this.Dispose(true);
			}
		}

		static public FormsCAKeyFrameAnimation CreateAnimationFromCGImageSource(CGImageSource ImageSource)
		{
			float RepeatCount = float.MaxValue;
			nint ImageCount = ImageSource.ImageCount;

			if (ImageCount <= 0)
			{
				return null;
			}

			using ImageDataHelper ImageData = new(ImageCount);

			if (ImageSource.TypeIdentifier == "com.compuserve.gif")
			{
				CoreGraphics.CGImageProperties imageProperties = ImageSource.GetProperties(null);
				using NSObject gifImageProperties = imageProperties?.Dictionary[ImageIO.CGImageProperties.GIFDictionary];
				using NSObject repeatCountValue = gifImageProperties?.ValueForKey(ImageIO.CGImageProperties.GIFLoopCount);

				if (repeatCountValue is not null)
				{
					float.TryParse(repeatCountValue.ToString(), out RepeatCount);
				}
				else
				{
					RepeatCount = 1;
				}

				if (RepeatCount == 0)
				{
					RepeatCount = float.MaxValue;
				}
			}

			for (int i = 0; i < ImageCount; i++)
			{
				ImageData.AddFrameData(i, ImageSource);
			}

			FormsCAKeyFrameAnimation Animation = ImageData.CreateKeyFrameAnimation();

			if (Animation is not null)
			{
				Animation.RemovedOnCompletion = false;
				Animation.KeyPath = "contents";
				Animation.RepeatCount = RepeatCount;
				Animation.Width = ImageData.Width;
				Animation.Height = ImageData.Height;

				if (ImageCount == 1)
				{
					Animation.Duration = double.MaxValue;
					Animation.KeyTimes = null;
				}
			}

			return Animation;
		}

		static public async Task<FormsCAKeyFrameAnimation> CreateAnimationFromStreamImageSourceAsync(StreamImageSource ImageSource, CancellationToken CancelationToken = default)
		{
			FormsCAKeyFrameAnimation Animation = null;

			if (ImageSource?.Stream is not null)
			{
				using Stream StreamImage = await ((IStreamImageSource)ImageSource).GetStreamAsync(CancelationToken).ConfigureAwait(false);

				if (StreamImage is not null)
				{
					using CGImageSource ParsedImageSource = CGImageSource.FromData(NSData.FromStream(StreamImage));
					Animation = CreateAnimationFromCGImageSource(ParsedImageSource);
				}
			}

			return Animation;
		}

		static public async Task<FormsCAKeyFrameAnimation> CreateAnimationFromUriImageSourceAsync(UriImageSource ImageSource, CancellationToken CancelationToken = default)
		{
			FormsCAKeyFrameAnimation Animation = null;

			if (ImageSource?.Uri is not null)
			{
				using Stream StreamImage = await ImageSource.GetStreamAsync(CancelationToken).ConfigureAwait(false);

				if (StreamImage is not null)
				{
					using CGImageSource ParsedImageSource = CGImageSource.FromData(NSData.FromStream(StreamImage));
					Animation = CreateAnimationFromCGImageSource(ParsedImageSource);
				}
			}

			return Animation;
		}

		static public FormsCAKeyFrameAnimation CreateAnimationFromFileImageSource(FileImageSource ImageSource)
		{
			FormsCAKeyFrameAnimation Animation = null;
			string File = ImageSource?.File;

			if (!string.IsNullOrEmpty(File))
			{
				using CGImageSource ParsedImageSource = CGImageSource.FromUrl(NSUrl.CreateFileUrl(File, null));
				Animation = CreateAnimationFromCGImageSource(ParsedImageSource);
			}

			return Animation;
		}
	}
}
