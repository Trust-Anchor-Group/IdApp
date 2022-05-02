using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using IdApp.Extensions;
using IdApp.Pages;
using IdApp.Services.AttachmentCache;
using SkiaSharp;
using Waher.Content.Images;
using Waher.Content.Images.Exif;
using Waher.Content.Markdown.Model.Multimedia;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Temporary;
using Xamarin.Forms;

namespace IdApp.Services.UI.Photos
{
	/// <summary>
	/// This is a helper class for downloading photos via http requests.
	/// It loads photos in the background, typically photo attachments connected to a
	/// digital identity. When the photos are loaded, they are added to an <see cref="ObservableCollection{T}"/> on the main thread.
	/// This class also handles errors when trying to load photos, and internally it uses a <see cref="IAttachmentCacheService"/>.
	/// </summary>
	public class PhotosLoader : BaseViewModel
	{
		private readonly ObservableCollection<Photo> photos;
		private readonly List<string> attachmentIds;
		private DateTime loadPhotosTimestamp;

		/// <summary>
		/// Creates a new instance of the <see cref="PhotosLoader"/> class.
		/// Use this constructor for when you want to load a a <b>single photo</b>.
		/// </summary>
		public PhotosLoader()
			: this(new ObservableCollection<Photo>())
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="PhotosLoader"/> class.
		/// Use this constructor for when you want to load a <b>list of photos</b>.
		/// </summary>
		/// <param name="Photos">The collection the photos should be added to when downloaded.</param>
		public PhotosLoader(ObservableCollection<Photo> Photos)
		{
			this.photos = Photos;
			this.attachmentIds = new List<string>();
		}

		/// <summary>
		/// Loads photos from the specified list of attachments.
		/// </summary>
		/// <param name="attachments">The attachments whose files to download.</param>
		/// <param name="signWith">How the requests are signed. For identity attachments, especially for attachments to an identity being created, <see cref="SignWith.CurrentKeys"/> should be used. For requesting attachments relating to a contract, <see cref="SignWith.LatestApprovedId"/> should be used.</param>
		/// <param name="whenDoneAction">A callback that is called when the photo load operation is done.</param>
		public Task LoadPhotos(Attachment[] attachments, SignWith signWith, Action whenDoneAction = null)
		{
			return LoadPhotos(attachments, signWith, DateTime.UtcNow, whenDoneAction);
		}

		/// <summary>
		/// Cancels any ongoing download of photos in progress. Also
		/// clears the collection of photos from its content.
		/// </summary>
		public void CancelLoadPhotos()
		{
			try
			{
				this.loadPhotosTimestamp = DateTime.UtcNow;
				this.attachmentIds.Clear();
				this.photos.Clear();
			}
			catch (Exception e)
			{
				this.LogService.LogException(e);
			}
		}

		/// <summary>
		/// Downloads one photo and stores in cache.
		/// </summary>
		/// <param name="attachment">The attachment to download.</param>
		/// <param name="signWith">How the requests are signed. For identity attachments, especially for attachments to an identity being created, <see cref="SignWith.CurrentKeys"/> should be used. For requesting attachments relating to a contract, <see cref="SignWith.LatestApprovedId"/> should be used.</param>
		/// <returns>Binary content (or null), Content-Type, Rotation</returns>
		public async Task<(byte[], string, int)> LoadOnePhoto(Attachment attachment, SignWith signWith)
		{
			try
			{
				return await GetPhoto(attachment, signWith, DateTime.UtcNow);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}

			return (null, string.Empty, 0);
		}

		private async Task LoadPhotos(Attachment[] attachments, SignWith signWith, DateTime now, Action whenDoneAction)
		{
			if (attachments is null || attachments.Length <= 0)
			{
				whenDoneAction?.Invoke();
				return;
			}

			List<Attachment> attachmentsList = attachments.GetImageAttachments().ToList();
			List<string> newAttachmentIds = attachmentsList.Select(x => x.Id).ToList();

			if (this.attachmentIds.HasSameContentAs(newAttachmentIds))
			{
				whenDoneAction?.Invoke();
				return;
			}

			this.attachmentIds.Clear();
			this.attachmentIds.AddRange(newAttachmentIds);

			foreach (Attachment attachment in attachmentsList)
			{
				if (this.loadPhotosTimestamp > now)
				{
					whenDoneAction?.Invoke();
					return;
				}

				try
				{
					(byte[] Bin, string ContentType, int Rotation) = await GetPhoto(attachment, signWith, now);
					if (Bin is null)
						continue;

					Photo Photo = new(Bin, Rotation);

					if (!(Bin is null))
						this.UiSerializer.BeginInvokeOnMainThread(() => photos.Add(Photo));
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			}

			whenDoneAction?.Invoke();
		}

		private async Task<(byte[], string, int)> GetPhoto(Attachment attachment, SignWith signWith, DateTime now)
		{
			if (attachment is null)
				return (null, string.Empty, 0);

			(byte[] Bin, string ContentType) = await this.AttachmentCacheService.TryGet(attachment.Url);
			if (!(Bin is null))
				return (Bin, ContentType, GetImageRotation(Bin));

			if (!this.NetworkService.IsOnline || !this.XmppService.IsOnline)
				return (null, string.Empty, 0);

			KeyValuePair<string, TemporaryFile> pair = await this.XmppService.Contracts.GetAttachment(attachment.Url, signWith, Constants.Timeouts.DownloadFile);

			using TemporaryFile file = pair.Value;
			if (this.loadPhotosTimestamp > now)     // If download has been cancelled any time _during_ download, stop here.
				return (null, string.Empty, 0);

			if (pair.Value.Length > int.MaxValue)   // Too large
				return (null, string.Empty, 0);

			file.Reset();

			ContentType = pair.Key;
			Bin = new byte[file.Length];
			if (file.Length != file.Read(Bin, 0, (int)file.Length))
				return (null, string.Empty, 0);

			bool IsContact = await this.XmppService.Contracts.IsContact(attachment.LegalId);

			await this.AttachmentCacheService.Add(attachment.Url, attachment.LegalId, IsContact, Bin, ContentType);

			return (Bin, ContentType, GetImageRotation(Bin));
		}

		/// <summary>
		/// Gets the rotation angle to use, to display the image correctly in Xamarin Forms.
		/// </summary>
		/// <param name="JpegImage">Binary representation of JPEG image.</param>
		/// <returns>Rotation angle (degrees).</returns>
		public static int GetImageRotation(byte[] JpegImage)
		{
			//!!! This rotation in xamarin is limited to Android
			if (Device.RuntimePlatform == Device.iOS)
				return 0;

			if (JpegImage is null)
				return 0;

			if (!EXIF.TryExtractFromJPeg(JpegImage, out ExifTag[] Tags))
				return 0;

			return GetImageRotation(Tags);
		}

		/// <summary>
		/// Gets the rotation angle to use, to display the image correctly in Xamarin Forms.
		/// </summary>
		/// <param name="Tags">EXIF Tags encoded in image.</param>
		/// <returns>Rotation angle (degrees).</returns>
		public static int GetImageRotation(ExifTag[] Tags)
		{
			foreach (ExifTag Tag in Tags)
			{
				if (Tag.Name == ExifTagName.Orientation)
				{
					if (Tag.Value is ushort Orientation)
					{
						return Orientation switch
						{
							1 => 0,// Top left. Default orientation.
							2 => 0,// Top right. Horizontally reversed.
							3 => 180,// Bottom right. Rotated by 180 degrees.
							4 => 180,// Bottom left. Rotated by 180 degrees and then horizontally reversed.
							5 => -90,// Left top. Rotated by 90 degrees counterclockwise and then horizontally reversed.
							6 => 90,// Right top. Rotated by 90 degrees clockwise.
							7 => 90,// Right bottom. Rotated by 90 degrees clockwise and then horizontally reversed.
							8 => -90,// Left bottom. Rotated by 90 degrees counterclockwise.
							_ => 0,
						};
					}
				}
			}

			return 0;
		}

		/// <summary>
		/// Loads a photo attachment.
		/// </summary>
		/// <param name="Attachment">Attachment containing photo.</param>
		/// <returns>Photo, Content-Type, Rotation</returns>
		public static async Task<(byte[], string, int)> LoadPhoto(Attachment Attachment)
		{
			PhotosLoader Loader = new();

			(byte[], string, int) Image = await Loader.LoadOnePhoto(Attachment, SignWith.LatestApprovedIdOrCurrentKeys);

			return Image;
		}

		/// <summary>
		/// Tries to load a photo from a set of attachments.
		/// </summary>
		/// <param name="Attachments">Attachments</param>
		/// <param name="MaxWith">Maximum width when displaying photo.</param>
		/// <param name="MaxHeight">Maximum height when displaying photo.</param>
		/// <returns>Filename, Width, Height, if loaded, (null,0,0) if not.</returns>
		public static Task<(string, int, int)> LoadPhotoAsTemporaryFile(Attachment[] Attachments, int MaxWith, int MaxHeight)
		{
			Attachment Photo = null;

			foreach (Attachment Attachment in Attachments)
			{
				if (Attachment.ContentType.StartsWith("image/"))
				{
					if (Attachment.ContentType == Constants.MimeTypes.Png)
					{
						Photo = Attachment;
						break;
					}
					else if (Photo is null)
						Photo = Attachment;
				}
			}

			if (Photo is null)
				return Task.FromResult<(string, int, int)>((null, 0, 0));
			else
				return LoadPhotoAsTemporaryFile(Photo, MaxWith, MaxHeight);
		}

		/// <summary>
		/// Tries to load a photo from an attachments.
		/// </summary>
		/// <param name="Attachment">Attachment</param>
		/// <param name="MaxWith">Maximum width when displaying photo.</param>
		/// <param name="MaxHeight">Maximum height when displaying photo.</param>
		/// <returns>Filename, Width, Height, if loaded, (null,0,0) if not.</returns>
		public static async Task<(string, int, int)> LoadPhotoAsTemporaryFile(Attachment Attachment, int MaxWith, int MaxHeight)
		{
			(byte[] Data, string _, int _) = await LoadPhoto(Attachment);
			
			if (!(Data is null))
			{
				string FileName = await ImageContent.GetTemporaryFile(Data);
				int Width;
				int Height;

				using (SKBitmap Bitmap = SKBitmap.Decode(Data))
				{
					Width = Bitmap.Width;
					Height = Bitmap.Height;
				}

				double ScaleWidth = ((double)MaxWith) / Width;
				double ScaleHeight = ((double)MaxHeight) / Height;
				double Scale = Math.Min(ScaleWidth, ScaleHeight);

				if (Scale < 1)
				{
					Width = (int)(Width * Scale + 0.5);
					Height = (int)(Height * Scale + 0.5);
				}

				return (FileName, Width, Height);
			}
			else 
				return (null, 0, 0);
		}

	}
}