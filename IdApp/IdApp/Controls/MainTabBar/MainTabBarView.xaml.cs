using System;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Services.EventLog;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Controls.MainTabBar
{
	/// <summary>
	/// Represents the main tab bar in the Main page of the application.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MainTabBarView
	{
		private const uint DurationInMs = 250;
		private readonly ILogService logService;
		private bool isShowing;

		/// <summary>
		/// Creates a new instance of the <see cref="MainTabBarView"/> class.
		/// </summary>
		public MainTabBarView()
		{
			this.logService = App.Instantiate<ILogService>();
			// Set default values here.
			OnPlatform<string> fontFamily = (OnPlatform<string>)Application.Current.Resources["FontAwesomeSolid"];
			this.LeftButton1FontFamily = fontFamily;
			this.LeftButton2FontFamily = fontFamily;
			this.CenterButtonFontFamily = fontFamily;
			this.RightButton1FontFamily = fontFamily;
			this.RightButton2FontFamily = fontFamily;

			InitializeComponent();

			LeftButton1.SetBinding(Button.CommandProperty, new Binding(nameof(LeftButton1Command), source: this));
			LeftButton2.SetBinding(Button.CommandProperty, new Binding(nameof(LeftButton2Command), source: this));
			CenterButton.SetBinding(Button.CommandProperty, new Binding(nameof(CenterButtonCommand), source: this));
			RightButton1.SetBinding(Button.CommandProperty, new Binding(nameof(RightButton1Command), source: this));
			RightButton2.SetBinding(Button.CommandProperty, new Binding(nameof(RightButton2Command), source: this));

			LeftButton1.SetBinding(Button.FontFamilyProperty, new Binding(nameof(LeftButton1FontFamily), source: this));
			LeftButton2.SetBinding(Button.FontFamilyProperty, new Binding(nameof(LeftButton2FontFamily), source: this));
			CenterButton.SetBinding(Button.FontFamilyProperty, new Binding(nameof(CenterButtonFontFamily), source: this));
			RightButton1.SetBinding(Button.FontFamilyProperty, new Binding(nameof(RightButton1FontFamily), source: this));
			RightButton2.SetBinding(Button.FontFamilyProperty, new Binding(nameof(RightButton2FontFamily), source: this));

			LeftButton1.SetBinding(Button.TextProperty, new Binding(nameof(LeftButton1Text), source: this));
			LeftButton2.SetBinding(Button.TextProperty, new Binding(nameof(LeftButton2Text), source: this));
			CenterButton.SetBinding(Button.TextProperty, new Binding(nameof(CenterButtonText), source: this));
			RightButton1.SetBinding(Button.TextProperty, new Binding(nameof(RightButton1Text), source: this));
			RightButton2.SetBinding(Button.TextProperty, new Binding(nameof(RightButton2Text), source: this));
		}

		#region Properties

		/// <summary>
		/// See <see cref="LeftButton1Command"/>
		/// </summary>
		public static readonly BindableProperty LeftButton1CommandProperty =
			BindableProperty.Create(nameof(LeftButton1Command), typeof(ICommand), typeof(MainTabBarView), default(ICommand));

		/// <summary>
		/// The command to bind to for the first button.
		/// </summary>
		public ICommand LeftButton1Command
		{
			get => (ICommand)this.GetValue(LeftButton1CommandProperty);
			set => this.SetValue(LeftButton1CommandProperty, value);
		}

		/// <summary>
		/// See <see cref="LeftButton2Command"/>
		/// </summary>
		public static readonly BindableProperty LeftButton2CommandProperty =
			BindableProperty.Create(nameof(LeftButton2Command), typeof(ICommand), typeof(MainTabBarView), default(ICommand));

		/// <summary>
		/// The command to bind to for the first button.
		/// </summary>
		public ICommand LeftButton2Command
		{
			get => (ICommand)this.GetValue(LeftButton2CommandProperty);
			set => this.SetValue(LeftButton2CommandProperty, value);
		}

		/// <summary>
		/// See <see cref="CenterButtonCommand"/>
		/// </summary>
		public static readonly BindableProperty CenterButtonCommandProperty =
			BindableProperty.Create(nameof(CenterButtonCommand), typeof(ICommand), typeof(MainTabBarView), default(ICommand));

		/// <summary>
		/// The command to bind to for the second button.
		/// </summary>
		public ICommand CenterButtonCommand
		{
			get => (ICommand)this.GetValue(CenterButtonCommandProperty);
			set => this.SetValue(CenterButtonCommandProperty, value);
		}

		/// <summary>
		/// See <see cref="RightButton1Command"/>
		/// </summary>
		public static readonly BindableProperty RightButton1CommandProperty =
			BindableProperty.Create(nameof(RightButton1Command), typeof(ICommand), typeof(MainTabBarView), default(ICommand));

		/// <summary>
		/// The command to bind to for the third button.
		/// </summary>
		public ICommand RightButton1Command
		{
			get => (ICommand)this.GetValue(RightButton1CommandProperty);
			set => this.SetValue(RightButton1CommandProperty, value);
		}

		/// <summary>
		/// See <see cref="RightButton2Command"/>
		/// </summary>
		public static readonly BindableProperty RightButton2CommandProperty =
			BindableProperty.Create(nameof(RightButton2Command), typeof(ICommand), typeof(MainTabBarView), default(ICommand));

		/// <summary>
		/// The command to bind to for the third button.
		/// </summary>
		public ICommand RightButton2Command
		{
			get => (ICommand)this.GetValue(RightButton2CommandProperty);
			set => this.SetValue(RightButton2CommandProperty, value);
		}

		/// <summary>
		/// See <see cref="LeftButton1FontFamily"/>
		/// </summary>
		public static readonly BindableProperty LeftButton1FontFamilyProperty =
			BindableProperty.Create(nameof(LeftButton1FontFamily), typeof(string), typeof(MainTabBarView), default(string));

		/// <summary>
		/// The font family to use for text on the first button
		/// </summary>
		public string LeftButton1FontFamily
		{
			get => (string)this.GetValue(LeftButton1FontFamilyProperty);
			set => this.SetValue(LeftButton1FontFamilyProperty, value);
		}

		/// <summary>
		/// See <see cref="LeftButton2FontFamily"/>
		/// </summary>
		public static readonly BindableProperty LeftButton2FontFamilyProperty =
			BindableProperty.Create(nameof(LeftButton2FontFamily), typeof(string), typeof(MainTabBarView), default(string));

		/// <summary>
		/// The font family to use for text on the first button
		/// </summary>
		public string LeftButton2FontFamily
		{
			get => (string)this.GetValue(LeftButton2FontFamilyProperty);
			set => this.SetValue(LeftButton2FontFamilyProperty, value);
		}

		/// <summary>
		/// See <see cref="CenterButtonFontFamily"/>
		/// </summary>
		public static readonly BindableProperty CenterButtonFontFamilyProperty =
			BindableProperty.Create(nameof(CenterButtonFontFamily), typeof(string), typeof(MainTabBarView), default(string));

		/// <summary>
		/// The font family to use for text on the second button
		/// </summary>
		public string CenterButtonFontFamily
		{
			get => (string)this.GetValue(CenterButtonFontFamilyProperty);
			set => this.SetValue(CenterButtonFontFamilyProperty, value);
		}

		/// <summary>
		/// See <see cref="RightButton1FontFamily"/>
		/// </summary>
		public static readonly BindableProperty RightButton1FontFamilyProperty =
			BindableProperty.Create(nameof(RightButton1FontFamily), typeof(string), typeof(MainTabBarView), default(string));

		/// <summary>
		/// The font family to use for text on the third button
		/// </summary>
		public string RightButton1FontFamily
		{
			get => (string)this.GetValue(RightButton1FontFamilyProperty);
			set => this.SetValue(RightButton1FontFamilyProperty, value);
		}

		/// <summary>
		/// See <see cref="RightButton2FontFamily"/>
		/// </summary>
		public static readonly BindableProperty RightButton2FontFamilyProperty =
			BindableProperty.Create(nameof(RightButton2FontFamily), typeof(string), typeof(MainTabBarView), default(string));

		/// <summary>
		/// The font family to use for text on the third button
		/// </summary>
		public string RightButton2FontFamily
		{
			get => (string)this.GetValue(RightButton2FontFamilyProperty);
			set => this.SetValue(RightButton2FontFamilyProperty, value);
		}

		/// <summary>
		/// See <see cref="LeftButton1Text"/>
		/// </summary>
		public static readonly BindableProperty LeftButton1TextProperty =
			BindableProperty.Create(nameof(LeftButton1Text), typeof(string), typeof(MainTabBarView), default(string));

		/// <summary>
		/// The text to use for text on the first button
		/// </summary>
		public string LeftButton1Text
		{
			get => (string)this.GetValue(LeftButton1TextProperty);
			set => this.SetValue(LeftButton1TextProperty, value);
		}

		/// <summary>
		/// See <see cref="LeftButton2Text"/>
		/// </summary>
		public static readonly BindableProperty LeftButton2TextProperty =
			BindableProperty.Create(nameof(LeftButton2Text), typeof(string), typeof(MainTabBarView), default(string));

		/// <summary>
		/// The text to use for text on the first button
		/// </summary>
		public string LeftButton2Text
		{
			get => (string)this.GetValue(LeftButton2TextProperty);
			set => this.SetValue(LeftButton2TextProperty, value);
		}

		/// <summary>
		/// See <see cref="CenterButtonText"/>
		/// </summary>
		public static readonly BindableProperty CenterButtonTextProperty =
			BindableProperty.Create(nameof(CenterButtonText), typeof(string), typeof(MainTabBarView), default(string));

		/// <summary>
		/// The text to use for text on the second button
		/// </summary>
		public string CenterButtonText
		{
			get => (string)this.GetValue(CenterButtonTextProperty);
			set => this.SetValue(CenterButtonTextProperty, value);
		}

		/// <summary>
		/// See <see cref="RightButton1Text"/>
		/// </summary>
		public static readonly BindableProperty RightButton1TextProperty =
			BindableProperty.Create(nameof(RightButton1Text), typeof(string), typeof(MainTabBarView), default(string));

		/// <summary>
		/// The text to use for text on the third button
		/// </summary>
		public string RightButton1Text
		{
			get => (string)this.GetValue(RightButton1TextProperty);
			set => this.SetValue(RightButton1TextProperty, value);
		}

		/// <summary>
		/// See <see cref="RightButton2Text"/>
		/// </summary>
		public static readonly BindableProperty RightButton2TextProperty =
			BindableProperty.Create(nameof(RightButton2Text), typeof(string), typeof(MainTabBarView), default(string));

		/// <summary>
		/// The text to use for text on the third button
		/// </summary>
		public string RightButton2Text
		{
			get => (string)this.GetValue(RightButton2TextProperty);
			set => this.SetValue(RightButton2TextProperty, value);
		}

		#endregion

		/// <summary>
		/// Call this method to show the toolbar content.
		/// </summary>
		public async Task Show()
		{
			if (!this.isShowing)
			{
				this.ToolBarContent.CancelAnimations();
				this.CenterButton.CancelAnimations();
				this.isShowing = true;
				Task translateToolBarTask = this.ToolBarContent.TranslateTo(0, 0, DurationInMs, Easing.SinIn);
				Task translateMiddleButtonTask = this.CenterButton.TranslateTo(0, 0, DurationInMs * 2, Easing.SinIn);
				try
				{
					await Task.WhenAll(translateMiddleButtonTask, translateToolBarTask);
				}
				catch (TaskCanceledException)
				{
					// Ok, do nothing
				}
				catch (Exception e)
				{
					this.logService.LogException(e);
				}
			}
		}

		/// <summary>
		/// Call this method to hide the toolbar content.
		/// </summary>
		public async Task Hide()
		{
			if (this.isShowing)
			{
				this.isShowing = false;
				this.CenterButton.CancelAnimations();
				this.ToolBarContent.CancelAnimations();
				Task translateMiddleButtonTask = this.CenterButton.TranslateTo(0, 30, DurationInMs, Easing.SinOut);
				Task translateToolBarTask = this.ToolBarContent.TranslateTo(0, this.MainToolBar.Height, DurationInMs, Easing.SinOut);
				try
				{
					await Task.WhenAll(translateToolBarTask, translateMiddleButtonTask);
				}
				catch (TaskCanceledException)
				{
					// Ok, do nothing
				}
				catch (Exception e)
				{
					this.logService.LogException(e);
				}
			}
		}
	}
}