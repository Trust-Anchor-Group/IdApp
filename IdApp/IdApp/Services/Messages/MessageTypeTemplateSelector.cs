using Xamarin.Forms;

namespace IdApp.Services.Messages
{
	/// <summary>
	/// Data Template Selector, based on Message Type.
	/// </summary>
	public class MessageTypeTemplateSelector : DataTemplateSelector
	{
		/// <summary>
		/// Template to use for sent messages.
		/// </summary>
		public DataTemplate SentTemplate { get; set; }

		/// <summary>
		/// Template to use for received messages.
		/// </summary>
		public DataTemplate ReceivedTemplate { get; set; }

		/// <summary>
		/// Template to use for other items.
		/// </summary>
		public DataTemplate DefaultTemplate { get; set; }

		/// <inheritdoc/>
		protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
		{
			if (item is ChatMessage Message)
			{
				switch (Message.MessageType)
				{
					case MessageType.Received: return this.ReceivedTemplate;
					case MessageType.Sent: return this.SentTemplate;
					default: return this.DefaultTemplate;
				}
			}

			return this.DefaultTemplate;
		}
	}
}
