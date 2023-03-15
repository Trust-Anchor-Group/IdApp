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

		public async Task<UIImage> LoadImageAsync(ImageSource Imagesource, CancellationToken CancelationToken = default(CancellationToken), float Scale = 1f)
		{
			UIImage Image = null;
			AesImageSource ImageLoader = Imagesource as AesImageSource;
			if (ImageLoader?.Uri != null)
			{
				using (Stream StreamImage = await ImageLoader.GetStreamAsync(CancelationToken).ConfigureAwait(false))
				{
					if (StreamImage != null)
						Image = UIImage.LoadFromData(NSData.FromStream(StreamImage), Scale);
				}
			}

			if (Image == null)
			{
				Log.Warning(nameof(ImageLoaderSourceHandler), "Could not load image: {0}", ImageLoader);
			}

			return Image;
		}

		public async Task<FormsCAKeyFrameAnimation> LoadImageAnimationAsync(ImageSource imagesource, CancellationToken cancelationToken = default(CancellationToken), float scale = 1)
		{
			FormsCAKeyFrameAnimation animation = await ImageAnimationHelper.CreateAnimationFromUriImageSourceAsync(imagesource as UriImageSource, cancelationToken).ConfigureAwait(false);
			if (animation == null)
			{
				Log.Warning(nameof(FileImageSourceHandler), "Could not find image: {0}", imagesource);
			}

			return animation;
		}
	}

	internal class ImageAnimationHelper
	{
		class ImageDataHelper : IDisposable
		{
			NSObject[] _keyFrames = null;
			NSNumber[] _keyTimes = null;
			double[] _delayTimes = null;
			int _imageCount = 0;
			double _totalAnimationTime = 0.0f;
			bool _disposed = false;

			public ImageDataHelper(nint imageCount)
			{
				if (imageCount <= 0)
					throw new ArgumentException();

				_keyFrames = new NSObject[imageCount];
				_keyTimes = new NSNumber[imageCount + 1];
				_delayTimes = new double[imageCount];
				_imageCount = (int)imageCount;
				Width = 0;
				Height = 0;
			}

			public int Width { get; set; }
			public int Height { get; set; }

			public void AddFrameData(int index, CGImageSource imageSource)
			{
				if (index < 0 || index >= _imageCount || index >= imageSource.ImageCount)
					throw new ArgumentException();

				double delayTime = 0.1f;

				var imageProperties = imageSource.GetProperties(index, null);
				using (var gifImageProperties = imageProperties?.Dictionary[ImageIO.CGImageProperties.GIFDictionary])
				using (var unclampedDelayTimeValue = gifImageProperties?.ValueForKey(ImageIO.CGImageProperties.GIFUnclampedDelayTime))
				using (var delayTimeValue = gifImageProperties?.ValueForKey(ImageIO.CGImageProperties.GIFDelayTime))
				{
					if (unclampedDelayTimeValue != null)
						double.TryParse(unclampedDelayTimeValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out delayTime);
					else if (delayTimeValue != null)
						double.TryParse(delayTimeValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out delayTime);

					// Frame delay compability adjustment.
					if (delayTime <= 0.02f)
						delayTime = 0.1f;

					using (var image = imageSource.CreateImage(index, null))
					{
						if (image != null)
						{
							Width = Math.Max(Width, (int)image.Width);
							Height = Math.Max(Height, (int)image.Height);

							_keyFrames[index]?.Dispose();
							_keyFrames[index] = null;

							_keyFrames[index] = NSObject.FromObject(image);
							_delayTimes[index] = delayTime;
							_totalAnimationTime += delayTime;
						}
					}
				}
			}

			public FormsCAKeyFrameAnimation CreateKeyFrameAnimation()
			{
				if (_totalAnimationTime <= 0.0f)
					return null;

				double currentTime = 0.0f;
				for (int i = 0; i < _imageCount; i++)
				{
					_keyTimes[i]?.Dispose();
					_keyTimes[i] = null;

					_keyTimes[i] = new NSNumber(currentTime);
					currentTime += _delayTimes[i] / _totalAnimationTime;
				}

				// When using discrete animation there should be one more keytime
				// than values, with 1.0f as value.
				_keyTimes[_imageCount] = new NSNumber(1.0f);

				return new FormsCAKeyFrameAnimation
				{
					Values = _keyFrames,
					KeyTimes = _keyTimes,
					Duration = _totalAnimationTime,
					CalculationMode = CAAnimation.AnimationDiscrete
				};
			}

			protected virtual void Dispose(bool disposing)
			{
				if (_disposed)
					return;

				for (int i = 0; i < _imageCount; i++)
				{
					_keyFrames[i]?.Dispose();
					_keyFrames[i] = null;

					_keyTimes[i]?.Dispose();
					_keyTimes[i] = null;
				}

				_keyTimes[_imageCount]?.Dispose();
				_keyTimes[_imageCount] = null;

				_disposed = true;
			}

			public void Dispose()
			{
				Dispose(true);
			}
		}

		static public FormsCAKeyFrameAnimation CreateAnimationFromCGImageSource(CGImageSource imageSource)
		{
			FormsCAKeyFrameAnimation animation = null;
			float repeatCount = float.MaxValue;
			var imageCount = imageSource.ImageCount;

			if (imageCount <= 0)
				return null;

			using (var imageData = new ImageDataHelper(imageCount))
			{
				if (imageSource.TypeIdentifier == "com.compuserve.gif")
				{
					var imageProperties = imageSource.GetProperties(null);
					using (var gifImageProperties = imageProperties?.Dictionary[ImageIO.CGImageProperties.GIFDictionary])
					using (var repeatCountValue = gifImageProperties?.ValueForKey(ImageIO.CGImageProperties.GIFLoopCount))
					{
						if (repeatCountValue != null)
							float.TryParse(repeatCountValue.ToString(), out repeatCount);
						else
							repeatCount = 1;

						if (repeatCount == 0)
							repeatCount = float.MaxValue;
					}
				}

				for (int i = 0; i < imageCount; i++)
				{
					imageData.AddFrameData(i, imageSource);
				}

				animation = imageData.CreateKeyFrameAnimation();
				if (animation != null)
				{
					animation.RemovedOnCompletion = false;
					animation.KeyPath = "contents";
					animation.RepeatCount = repeatCount;
					animation.Width = imageData.Width;
					animation.Height = imageData.Height;

					if (imageCount == 1)
					{
						animation.Duration = double.MaxValue;
						animation.KeyTimes = null;
					}
				}
			}

			return animation;
		}

		static public async Task<FormsCAKeyFrameAnimation> CreateAnimationFromStreamImageSourceAsync(StreamImageSource imageSource, CancellationToken cancelationToken = default(CancellationToken))
		{
			FormsCAKeyFrameAnimation animation = null;

			if (imageSource?.Stream != null)
			{
				using (var streamImage = await ((IStreamImageSource)imageSource).GetStreamAsync(cancelationToken).ConfigureAwait(false))
				{
					if (streamImage != null)
					{
						using (var parsedImageSource = CGImageSource.FromData(NSData.FromStream(streamImage)))
						{
							animation = ImageAnimationHelper.CreateAnimationFromCGImageSource(parsedImageSource);
						}
					}
				}
			}

			return animation;
		}

		static public async Task<FormsCAKeyFrameAnimation> CreateAnimationFromUriImageSourceAsync(UriImageSource imageSource, CancellationToken cancelationToken = default(CancellationToken))
		{
			FormsCAKeyFrameAnimation animation = null;

			if (imageSource?.Uri != null)
			{
				using (var streamImage = await imageSource.GetStreamAsync(cancelationToken).ConfigureAwait(false))
				{
					if (streamImage != null)
					{
						using (var parsedImageSource = CGImageSource.FromData(NSData.FromStream(streamImage)))
						{
							animation = ImageAnimationHelper.CreateAnimationFromCGImageSource(parsedImageSource);
						}
					}
				}
			}

			return animation;
		}

		static public FormsCAKeyFrameAnimation CreateAnimationFromFileImageSource(FileImageSource imageSource)
		{
			FormsCAKeyFrameAnimation animation = null;
			string file = imageSource?.File;
			if (!string.IsNullOrEmpty(file))
			{
				using (var parsedImageSource = CGImageSource.FromUrl(NSUrl.CreateFileUrl(file, null)))
				{
					animation = ImageAnimationHelper.CreateAnimationFromCGImageSource(parsedImageSource);
				}
			}

			return animation;
		}
	}
}