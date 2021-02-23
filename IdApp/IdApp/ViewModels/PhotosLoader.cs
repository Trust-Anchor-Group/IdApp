using IdApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Temporary;
using Xamarin.Forms;

namespace IdApp.ViewModels
{
    /// <summary>
    /// This is a helper class for downloading photos via http requests.
    /// It loads photos in the background, typically photo attachments connected to a
    /// digital identity. When the photos are loaded, they are added to an <see cref="ObservableCollection{T}"/> on the main thread.
    /// This class also handles errors when trying to load photos, and internally it uses a <see cref="IImageCacheService"/>.
    /// </summary>
    public class PhotosLoader
    {
        private readonly ILogService logService;
        private readonly INetworkService networkService;
        private readonly INeuronService neuronService;
        private readonly IUiDispatcher uiDispatcher;
        private readonly IImageCacheService imageCacheService;
        private readonly ObservableCollection<ImageSource> photos;
        private readonly List<string> attachmentIds;
        private DateTime loadPhotosTimestamp;

        /// <summary>
        /// Creates a new instance of the <see cref="PhotosLoader"/> class.
        /// </summary>
        /// <param name="logService">The log service to use if and when logging errors.</param>
        /// <param name="networkService">The network service to use for checking connectivity.</param>
        /// <param name="neuronService">The neuron service to know which XMPP server to connect to.</param>
        /// <param name="uiDispatcher">The UI dispatcher to use for alerts and context switching.</param>
        /// <param name="imageCacheService">The image cache service to use for optimizing requests.</param>
        /// <param name="photos">The collection the photos should be added to when downloaded.</param>
        public PhotosLoader(
            ILogService logService, 
            INetworkService networkService, 
            INeuronService neuronService,
            IUiDispatcher uiDispatcher,
            IImageCacheService imageCacheService,
            ObservableCollection<ImageSource> photos)
        {
            this.logService = logService;
            this.networkService = networkService;
            this.neuronService = neuronService;
            this.uiDispatcher = uiDispatcher;
            this.imageCacheService = imageCacheService;
            this.photos = photos;
            this.attachmentIds = new List<string>();
        }

        /// <summary>
        /// Loads photos from the specified list of attachments.
        /// </summary>
        /// <param name="attachments">The attachments whose files to download.</param>
        /// <param name="signWith">How the requests are signed. For identity attachments, especially for attachments to an identity being created, <see cref="SignWith.CurrentKeys"/> should be used. For requesting attachments relating to a contract, <see cref="SignWith.LatestApprovedId"/> should be used.</param>
        /// <returns></returns>
        public Task LoadPhotos(Attachment[] attachments, SignWith signWith)
        {
            return LoadPhotos(attachments, signWith, DateTime.UtcNow);
        }

        /// <summary>
        /// Cancels any ongoing download of photos in progress. Also
        /// clears the collection of photos from its content.
        /// </summary>
        public void CancelLoadPhotos()
        {
            this.loadPhotosTimestamp = DateTime.UtcNow;
            this.photos.Clear();
            this.attachmentIds.Clear();
        }

        /// <summary>
        /// Downloads one photo and stores in cache.
        /// </summary>
        /// <param name="attachment">The attachment to download.</param>
        /// <param name="signWith">How the requests are signed. For identity attachments, especially for attachments to an identity being created, <see cref="SignWith.CurrentKeys"/> should be used. For requesting attachments relating to a contract, <see cref="SignWith.LatestApprovedId"/> should be used.</param>
        /// <returns></returns>
        public async Task<MemoryStream> LoadOnePhoto(Attachment attachment, SignWith signWith)
        {
            try
            {
                if (!this.networkService.IsOnline || !this.neuronService.Contracts.IsOnline)
                    return null;

                MemoryStream stream = await GetPhoto(attachment, signWith, DateTime.UtcNow);
                stream?.Reset();
                return stream;
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
            }

            return null;
        }

        private async Task LoadPhotos(Attachment[] attachments, SignWith signWith, DateTime now)
        {
            if (attachments == null || attachments.Length <= 0)
                return;

            List<Attachment> attachmentsList = attachments.GetImageAttachments().ToList();
            List<string> newAttachmentIds = attachmentsList.Select(x => x.Id).ToList();

            if (this.attachmentIds.HasSameContentAs(newAttachmentIds))
                return;

            this.attachmentIds.Clear();
            this.attachmentIds.AddRange(newAttachmentIds);

            foreach (Attachment attachment in attachmentsList)
            {
                if (this.loadPhotosTimestamp > now)
                {
                    return;
                }

                try
                {
                    if (!this.networkService.IsOnline || !this.neuronService.Contracts.IsOnline)
                        continue;

                    Stream stream = await GetPhoto(attachment, signWith, now);
                    if (stream != null)
                    {
                        stream.Reset();
                        ImageSource imageSource = ImageSource.FromStream(() => stream); // Disposes the stream
                        this.uiDispatcher.BeginInvokeOnMainThread(() => photos.Add(imageSource));
                    }
                }
                catch (Exception ex)
                {
                    this.logService.LogException(ex);
                }
            }
        }

        private async Task<MemoryStream> GetPhoto(Attachment attachment, SignWith signWith, DateTime now)
        {
            // 1. Found in cache?
            if (this.imageCacheService.TryGet(attachment.Url, out MemoryStream stream))
            {
                return stream;
            }

            // 2. Needs download
            KeyValuePair<string, TemporaryFile> pair = await this.neuronService.Contracts.GetAttachment(attachment.Url, signWith, Constants.Timeouts.DownloadFile);
            if (this.loadPhotosTimestamp > now)
            {
                pair.Value.Dispose();
                return null;
            }

            using (TemporaryFile file = pair.Value)
            {
                file.Reset();
                MemoryStream ms = new MemoryStream();
                await file.CopyToAsync(ms);
                // 3. Save to cache
                ms.Reset();
                await this.imageCacheService.Add(attachment.Url, ms);
                ms.Reset();
                return ms;
            }
        }
    }
}