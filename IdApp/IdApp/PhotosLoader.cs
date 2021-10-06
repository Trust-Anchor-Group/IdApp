using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using IdApp.Extensions;
using IdApp.Pages;
using IdApp.Services.AttachmentCache;
using IdApp.Services.EventLog;
using IdApp.Services.Network;
using IdApp.Services.Neuron;
using IdApp.Services.UI;
using Waher.Content.Images;
using Waher.Content.Images.Exif;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Temporary;
using Xamarin.Forms;

namespace IdApp
{
	/// <summary>
	/// This is a helper class for downloading photos via http requests.
	/// It loads photos in the background, typically photo attachments connected to a
	/// digital identity. When the photos are loaded, they are added to an <see cref="ObservableCollection{T}"/> on the main thread.
	/// This class also handles errors when trying to load photos, and internally it uses a <see cref="IAttachmentCacheService"/>.
	/// </summary>
	public class PhotosLoader
	{
		private readonly ILogService logService;
		private readonly INetworkService networkService;
		private readonly INeuronService neuronService;
		private readonly IUiSerializer uiSerializer;
		private readonly IAttachmentCacheService attachmentCacheService;
		private readonly ObservableCollection<Photo> photos;
		private readonly List<string> attachmentIds;
		private DateTime loadPhotosTimestamp;

		/// <summary>
		/// Creates a new instance of the <see cref="PhotosLoader"/> class.
		/// Use this constructor for when you want to load a a <b>single photo</b>.
		/// </summary>
		/// <param name="logService">The log service to use if and when logging errors.</param>
		/// <param name="networkService">The network service to use for checking connectivity.</param>
		/// <param name="neuronService">The neuron service to know which XMPP server to connect to.</param>
		/// <param name="uiSerializer">The UI dispatcher to use for alerts and context switching.</param>
		/// <param name="attachmentCacheService">The attachment cache service to use for optimizing requests.</param>
		public PhotosLoader(
			ILogService logService,
			INetworkService networkService,
			INeuronService neuronService,
			IUiSerializer uiSerializer,
			IAttachmentCacheService attachmentCacheService)
			: this(logService, networkService, neuronService, uiSerializer, attachmentCacheService, new ObservableCollection<Photo>())
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="PhotosLoader"/> class.
		/// Use this constructor for when you want to load a <b>list of photos</b>.
		/// </summary>
		/// <param name="logService">The log service to use if and when logging errors.</param>
		/// <param name="networkService">The network service to use for checking connectivity.</param>
		/// <param name="neuronService">The neuron service to know which XMPP server to connect to.</param>
		/// <param name="uiSerializer">The UI dispatcher to use for alerts and context switching.</param>
		/// <param name="attachmentCacheService">The attachment cache service to use for optimizing requests.</param>
		/// <param name="photos">The collection the photos should be added to when downloaded.</param>
		public PhotosLoader(
			ILogService logService,
			INetworkService networkService,
			INeuronService neuronService,
			IUiSerializer uiSerializer,
			IAttachmentCacheService attachmentCacheService,
			ObservableCollection<Photo> photos)
		{
			this.logService = logService;
			this.networkService = networkService;
			this.neuronService = neuronService;
			this.uiSerializer = uiSerializer;
			this.attachmentCacheService = attachmentCacheService;
			this.photos = photos;
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
				this.logService.LogException(e);
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
				if (!this.networkService.IsOnline || !this.neuronService.IsOnline)
					return (null, string.Empty, 0);

				if (attachment is null)
					return (null, string.Empty, 0);

				return await GetPhoto(attachment, signWith, DateTime.UtcNow);
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
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
					if (!this.networkService.IsOnline || !this.neuronService.IsOnline)
						continue;

					(byte[] Bin, string ContentType, int Rotation) = await GetPhoto(attachment, signWith, now);
					Photo Photo = new Photo(Bin, Rotation);

					if (!(Bin is null))
						this.uiSerializer.BeginInvokeOnMainThread(() => photos.Add(Photo));
				}
				catch (Exception ex)
				{
					this.logService.LogException(ex);
				}
			}

			whenDoneAction?.Invoke();
		}

		private async Task<(byte[], string, int)> GetPhoto(Attachment attachment, SignWith signWith, DateTime now)
		{
			(byte[] Bin, string ContentType) = await this.attachmentCacheService.TryGet(attachment.Url);
			if (!(Bin is null))
				return (Bin, ContentType, GetImageRotation(Bin));

			KeyValuePair<string, TemporaryFile> pair = await this.neuronService.Contracts.GetAttachment(attachment.Url, signWith, Constants.Timeouts.DownloadFile);

			using (TemporaryFile file = pair.Value)
			{
				if (this.loadPhotosTimestamp > now)     // If download has been cancelled any time _during_ download, stop here.
					return (null, string.Empty, 0);

				if (pair.Value.Length > int.MaxValue)   // Too large
					return (null, string.Empty, 0);

				file.Reset();

				ContentType = pair.Key;
				Bin = new byte[file.Length];
				if (file.Length != file.Read(Bin, 0, (int)file.Length))
					return (null, string.Empty, 0);

				bool IsContact = await this.neuronService.Contracts.IsContact(attachment.LegalId);

				await this.attachmentCacheService.Add(attachment.Url, attachment.LegalId, IsContact, Bin, ContentType);

				return (Bin, ContentType, GetImageRotation(Bin));
			}
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

			foreach (ExifTag Tag in Tags)
			{
				if (Tag.Name == ExifTagName.Orientation)
				{
					if (Tag.Value is ushort Orientation)
					{
						switch (Orientation)
						{
							case 1: return 0;       // Top left. Default orientation.
							case 2: return 0;       // Top right. Horizontally reversed.
							case 3: return 180;     // Bottom right. Rotated by 180 degrees.
							case 4: return 180;     // Bottom left. Rotated by 180 degrees and then horizontally reversed.
							case 5: return -90;     // Left top. Rotated by 90 degrees counterclockwise and then horizontally reversed.
							case 6: return 90;      // Right top. Rotated by 90 degrees clockwise.
							case 7: return 90;      // Right bottom. Rotated by 90 degrees clockwise and then horizontally reversed.
							case 8: return -90;     // Left bottom. Rotated by 90 degrees counterclockwise.
							default: return 0;
						}
					}
				}
			}

			return 0;
		}

	}
}