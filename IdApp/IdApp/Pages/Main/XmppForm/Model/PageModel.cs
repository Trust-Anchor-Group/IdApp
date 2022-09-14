using Waher.Networking.XMPP.DataForms;
using Waher.Networking.XMPP.DataForms.FieldTypes;
using Layout = Waher.Networking.XMPP.DataForms.Layout;
using Xamarin.Forms;
using Waher.Content;

namespace IdApp.Pages.Main.XmppForm.Model
{
	/// <summary>
	/// Page model
	/// </summary>
	public class PageModel
	{
		private readonly Layout.Page page;
		private readonly object content;

		/// <summary>
		/// Page model
		/// </summary>
		/// <param name="Page">Page object</param>
		public PageModel(Layout.Page Page)
		{
			this.page = Page;
			this.content = BuildContent(Page.Elements);
		}

		/// <summary>
		/// Page label
		/// </summary>
		public string Label => this.page.Label;

		/// <summary>
		/// If the page has a label.
		/// </summary>
		public bool HasLabel => !string.IsNullOrEmpty(this.page.Label);

		/// <summary>
		/// Page content
		/// </summary>
		public object Content => this.content;

		private static StackLayout BuildContent(Layout.LayoutElement[] Elements)
		{
			StackLayout Result = new()
			{
				HorizontalOptions = LayoutOptions.StartAndExpand,
				VerticalOptions = LayoutOptions.StartAndExpand,
				Orientation = StackOrientation.Vertical
			};

			foreach (Layout.LayoutElement Element in Elements)
			{
				object Content = BuildContent(Element);

				if (Content is View View)
					Result.Children.Add(View);
			}

			return Result;
		}

		private static object BuildContent(Layout.LayoutElement Element)
		{
			if (Element is Layout.FieldReference FieldRef)
			{
				// TODO: Error
				// TODO: NotUsed

				Field Field = FieldRef.Form[FieldRef.Var];
				if (Field is null)
					return null;

				if (Field is BooleanField BooleanField)
				{
					StackLayout Layout = new()
					{
						Orientation = StackOrientation.Horizontal
					};

					CheckBox CheckBox = new()
					{
						IsChecked = CommonTypes.TryParse(BooleanField.ValueString, out bool b) && b,
						IsEnabled = Field.ReadOnly,
						StyleId = Field.Var,
						VerticalOptions = LayoutOptions.Center
					};

					CheckBox.CheckedChanged += CheckBox_CheckedChanged;

					Layout.Children.Add(CheckBox);
					Layout.Children.Add(new Label()
					{
						Text = BooleanField.Label,
						Style = (Style)App.Current.Resources["KeyLabel"],
						LineBreakMode = LineBreakMode.WordWrap,
						VerticalOptions = LayoutOptions.Center
					});

					return Layout;
				}
				else if (Field is FixedField FixedField)
				{
					return new Label()
					{
						Text = FixedField.ValueString,
						Style = (Style)App.Current.Resources["InfoText"],
						LineBreakMode = LineBreakMode.WordWrap
					};
				}
				else if (Field is JidMultiField JidMultiField)
				{
					return null;    // TODO
				}
				else if (Field is JidSingleField JidSingleField)
				{
					return null;    // TODO
				}
				else if (Field is ListMultiField ListMultiField)
				{
					return null;    // TODO
				}
				else if (Field is ListSingleField ListSingleField)
				{
					return null;    // TODO
				}
				else if (Field is MediaField MediaField)
				{
					return null;    // TODO
				}
				else if (Field is TextMultiField TextMultiField)
				{
					return null;    // TODO
				}
				else if (Field is TextPrivateField TextPrivateField)
				{
					return null;    // TODO
				}
				else if (Field is TextSingleField TextSingleField)
				{
					return null;    // TODO
				}
				else
					return null;
			}
			else if (Element is Layout.Section Section)
			{
				StackLayout Layout = new()
				{
					HorizontalOptions = LayoutOptions.StartAndExpand,
					VerticalOptions = LayoutOptions.StartAndExpand,
					Orientation = StackOrientation.Vertical
				};

				if (!string.IsNullOrEmpty(Section.Label))
				{
					Layout.Children.Add(new Label()
					{
						Text = Section.Label,
						Style = (Style)App.Current.Resources["Heading"],
						LineBreakMode = LineBreakMode.WordWrap
					});
				}

				foreach (Layout.LayoutElement Element2 in Section.Elements)
				{
					object Content = BuildContent(Element2);

					if (Content is View View)
						Layout.Children.Add(View);
				}

				Frame Frame = new()
				{
					BorderColor = (Color)App.Current.Resources["ForegroundColor"],
					Padding = new Thickness(10),
					CornerRadius = 5,
					Content = Layout
				};

				return Frame;
			}
			else if (Element is Layout.TextElement Text)
			{
				return new Label()
				{
					Text = Text.Text,
					Style = (Style)App.Current.Resources["InfoText"],
					LineBreakMode = LineBreakMode.WordWrap
				};
			}
			else
				return null;
		}

		private static void CheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
		{
			// TODO
		}
	}
}
