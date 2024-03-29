﻿using Waher.Networking.XMPP.DataForms;
using Waher.Networking.XMPP.DataForms.FieldTypes;
using Layout = Waher.Networking.XMPP.DataForms.Layout;
using Xamarin.Forms;
using Waher.Content;
using System;
using System.Collections.Generic;
using System.IO;

namespace IdApp.Pages.Main.XmppForm.Model
{
	/// <summary>
	/// Page model
	/// </summary>
	public class PageModel
	{
		private readonly XmppFormViewModel model;
		private readonly DataForm form;
		private readonly Layout.Page page;
		private readonly object content;

		/// <summary>
		/// Page model
		/// </summary>
		/// <param name="Model">Parent view model</param>
		/// <param name="Page">Page object</param>
		public PageModel(XmppFormViewModel Model, Layout.Page Page)
		{
			this.model = Model;
			this.page = Page;
			this.form = this.page.Form;
			this.content = this.BuildContent(Page.Elements);
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

		private StackLayout BuildContent(Layout.LayoutElement[] Elements)
		{
			StackLayout Result = new()
			{
				HorizontalOptions = LayoutOptions.StartAndExpand,
				VerticalOptions = LayoutOptions.StartAndExpand,
				Orientation = StackOrientation.Vertical
			};

			foreach (Layout.LayoutElement Element in Elements)
			{
				object Content = this.BuildContent(Element);

				if (Content is View View)
					Result.Children.Add(View);
			}

			return Result;
		}

		private object BuildContent(Layout.LayoutElement Element)
		{
			if (Element is Layout.FieldReference FieldRef)
			{
				Field Field = FieldRef.Form[FieldRef.Var];
				if (Field is null)
					return null;

				if (Field is BooleanField)
				{
					StackLayout Layout = new()
					{
						Orientation = StackOrientation.Horizontal
					};

					CheckBox CheckBox = new()
					{
						IsChecked = CommonTypes.TryParse(Field.ValueString, out bool b) && b,
						IsEnabled = !Field.ReadOnly,
						StyleId = Field.Var,
						VerticalOptions = LayoutOptions.Center,
						BackgroundColor = BackgroundColor(Field)
					};

					CheckBox.CheckedChanged += this.CheckBox_CheckedChanged;

					Layout.Children.Add(CheckBox);
					Layout.Children.Add(new Label()
					{
						Text = Field.Label,
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
				else if (Field is TextSingleField TextSingleField || Field is JidSingleField || Field is TextPrivateField)
				{
					StackLayout Layout = new()
					{
						Orientation = StackOrientation.Vertical
					};

					Layout.Children.Add(new Label()
					{
						Text = Field.Label,
						Style = (Style)App.Current.Resources["KeyLabel"],
						LineBreakMode = LineBreakMode.WordWrap
					});

					Entry Entry = new()
					{
						Text = Field.ValueString,
						IsEnabled = !Field.ReadOnly,
						StyleId = Field.Var,
						BackgroundColor = BackgroundColor(Field),
						IsPassword = Field is TextPrivateField
					};

					Entry.TextChanged += this.Entry_TextChanged;

					Layout.Children.Add(Entry);

					return Layout;
				}
				else if (Field is TextMultiField || Field is JidMultiField)
				{
					StackLayout Layout = new()
					{
						Orientation = StackOrientation.Vertical
					};

					Layout.Children.Add(new Label()
					{
						Text = Field.Label,
						Style = (Style)App.Current.Resources["KeyLabel"],
						LineBreakMode = LineBreakMode.WordWrap
					});

					Editor Editor = new()
					{
						Text = Field.ValueString,
						IsEnabled = !Field.ReadOnly,
						StyleId = Field.Var,
						BackgroundColor = BackgroundColor(Field)
					};

					Editor.TextChanged += this.Editor_TextChanged;

					Layout.Children.Add(Editor);

					return Layout;
				}
				else if (Field is ListSingleField ListSingleField)
				{
					StackLayout Layout = new()
					{
						Orientation = StackOrientation.Vertical
					};

					Layout.Children.Add(new Label()
					{
						Text = Field.Label,
						Style = (Style)App.Current.Resources["KeyLabel"],
						LineBreakMode = LineBreakMode.WordWrap
					});

					Picker Picker = new()
					{
						Title = Field.Description,
						IsEnabled = !Field.ReadOnly,
						StyleId = Field.Var,
						BackgroundColor = BackgroundColor(Field)
					};

					if (Field?.Options is not null)
					{
						foreach (KeyValuePair<string, string> Option in Field.Options)
							Picker.Items.Add(Option.Key);
					}

					Picker.SelectedItem = Field.ValueString;
					Picker.SelectedIndexChanged += this.Picker_SelectedIndexChanged;

					Layout.Children.Add(Picker);

					return Layout;
				}
				else if (Field is ListMultiField)
				{
					StackLayout Layout = new()
					{
						Orientation = StackOrientation.Vertical
					};

					Layout.Children.Add(new Label()
					{
						Text = Field.Label,
						Style = (Style)App.Current.Resources["KeyLabel"],
						LineBreakMode = LineBreakMode.WordWrap
					});

					if (Field.Options is not null)
					{
						Dictionary<string, bool> Selected = new();

						if (Field.ValueStrings is not null)
						{
							foreach (string s in Field.ValueStrings)
								Selected[s] = true;
						}

						foreach (KeyValuePair<string, string> Option in Field.Options)
						{
							StackLayout Layout2 = new()
							{
								Orientation = StackOrientation.Horizontal
							};

							CheckBox CheckBox = new()
							{
								IsChecked = Selected.ContainsKey(Option.Value),
								IsEnabled = !Field.ReadOnly,
								StyleId = Field.Var + " | " + Option.Value,
								VerticalOptions = LayoutOptions.Center,
								BackgroundColor = BackgroundColor(Field)
							};

							CheckBox.CheckedChanged += this.MultiCheckBox_CheckedChanged;

							Layout2.Children.Add(CheckBox);
							Layout2.Children.Add(new Label()
							{
								Text = Option.Key,
								Style = (Style)App.Current.Resources["KeyLabel"],
								LineBreakMode = LineBreakMode.WordWrap,
								VerticalOptions = LayoutOptions.Center
							});

							Layout.Children.Add(Layout2);
						}
					}

					return Layout;
				}
				else if (Field is MediaField MediaField)
				{
					Media Media = MediaField.Media;

					if (string.IsNullOrEmpty(Media.ContentType) || Media.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
					{
						StackLayout Layout = new()
						{
							Orientation = StackOrientation.Vertical
						};

						Layout.Children.Add(new Label()
						{
							Text = Field.Label,
							Style = (Style)App.Current.Resources["KeyLabel"],
							LineBreakMode = LineBreakMode.WordWrap
						});

						ImageSource Source;

						if (Media.Binary is not null)
							Source = ImageSource.FromStream(() => new MemoryStream(Media.Binary));
						else if (!string.IsNullOrEmpty(Media.URL) && Uri.TryCreate(Media.URL, UriKind.Absolute, out Uri ParsedUrl))
							Source = ImageSource.FromUri(ParsedUrl);
						else
							Source = null;

						if (Source is not null)
						{
							Image Image = new()
							{
								Source = Source,
								HorizontalOptions = LayoutOptions.Center,
								VerticalOptions = LayoutOptions.Start
							};

							Layout.Children.Add(Image);
						}

						return Layout;
					}
					else
						return null;    // TODO: audio, video
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
					object Content = this.BuildContent(Element2);

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

		private static Color BackgroundColor(Field F)
		{
			if (F.HasError)
				return Color.Salmon;
			else if (F.NotSame)
				return Color.LightGray;
			else
				return Color.Default;
		}

		private void CheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
		{
			if (sender is not CheckBox CheckBox)
				return;

			string Var = CheckBox.StyleId;
			Field Field = this.form[Var];
			if (Field is null)
				return;

			Field.SetValue(CommonTypes.Encode(e.Value));

			CheckBox.BackgroundColor = BackgroundColor(Field);

			this.model.ValidateForm();
		}

		private void Entry_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (sender is not Entry Entry)
				return;

			string Var = Entry.StyleId;
			Field Field = this.form[Var];
			if (Field is null)
				return;

			Field.SetValue(e.NewTextValue);

			Entry.BackgroundColor = BackgroundColor(Field);

			this.model.ValidateForm();
		}

		private void Editor_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (sender is not Editor Editor)
				return;

			string Var = Editor.StyleId;
			Field Field = this.form[Var];
			if (Field is null)
				return;

			Field.SetValue(e.NewTextValue);

			Editor.BackgroundColor = BackgroundColor(Field);

			this.model.ValidateForm();
		}

		private void Picker_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (sender is not Picker Picker)
				return;

			string Var = Picker.StyleId;
			Field Field = this.form[Var];
			if (Field is null)
				return;

			string s = Picker.SelectedItem?.ToString() ?? string.Empty;

			if (Field.Options is not null)
			{
				foreach (KeyValuePair<string, string> P in Field.Options)
				{
					if (s == P.Key)
					{
						s = P.Value;
						break;
					}
				}
			}

			Field.SetValue(s);

			Picker.BackgroundColor = BackgroundColor(Field);

			this.model.ValidateForm();
		}

		private void MultiCheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
		{
			if (sender is not CheckBox CheckBox)
				return;

			string Var = CheckBox.StyleId;
			int i = Var.IndexOf(" | ");
			if (i < 0)
				return;

			string Value = Var[(i + 3)..];
			Var = Var.Substring(0, i);

			Field Field = this.form[Var];
			if (Field is null)
				return;

			List<string> Values = new();
			if (Field.ValueStrings is not null)
				Values.AddRange(Field.ValueStrings);

			if (e.Value)
			{
				i = Values.IndexOf(Value);
				if (i < 0)
					Values.Add(Value);
			}
			else
				Values.Remove(Value);

			Field.SetValue(Values.ToArray());

			CheckBox.BackgroundColor = BackgroundColor(Field);

			this.model.ValidateForm();
		}

	}
}
