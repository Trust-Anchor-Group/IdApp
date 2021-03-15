using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views
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
            this.logService = DependencyService.Resolve<ILogService>();
            // Set default values here.
            OnPlatform<string> fontFamily = (OnPlatform<string>)Application.Current.Resources["FontAwesomeSolid"];
            this.Button1FontFamily = fontFamily;
            this.Button2FontFamily = fontFamily;
            this.Button3FontFamily = fontFamily;

            InitializeComponent();

            Button1.SetBinding(Button.CommandProperty, new Binding(nameof(Button1Command), source: this));
            Button2.SetBinding(Button.CommandProperty, new Binding(nameof(Button2Command), source: this));
            Button3.SetBinding(Button.CommandProperty, new Binding(nameof(Button3Command), source: this));

            Button1.SetBinding(Button.FontFamilyProperty, new Binding(nameof(Button1FontFamily), source: this));
            Button2.SetBinding(Button.FontFamilyProperty, new Binding(nameof(Button2FontFamily), source: this));
            Button3.SetBinding(Button.FontFamilyProperty, new Binding(nameof(Button3FontFamily), source: this));

            Button1.SetBinding(Button.TextProperty, new Binding(nameof(Button1Text), source: this));
            Button2.SetBinding(Button.TextProperty, new Binding(nameof(Button2Text), source: this));
            Button3.SetBinding(Button.TextProperty, new Binding(nameof(Button3Text), source: this));
        }

        #region Properties

        /// <summary>
        /// See <see cref="Button1Command"/>
        /// </summary>
        public static readonly BindableProperty Button1CommandProperty =
            BindableProperty.Create("Button1Command", typeof(ICommand), typeof(MainTabBarView), default(ICommand));

        /// <summary>
        /// The command to bind to for the first button.
        /// </summary>
        public ICommand Button1Command
        {
            get { return (ICommand)GetValue(Button1CommandProperty); }
            set { SetValue(Button1CommandProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button2Command"/>
        /// </summary>
        public static readonly BindableProperty Button2CommandProperty =
            BindableProperty.Create("Button2Command", typeof(ICommand), typeof(MainTabBarView), default(ICommand));

        /// <summary>
        /// The command to bind to for the second button.
        /// </summary>
        public ICommand Button2Command
        {
            get { return (ICommand)GetValue(Button2CommandProperty); }
            set { SetValue(Button2CommandProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button3Command"/>
        /// </summary>
        public static readonly BindableProperty Button3CommandProperty =
            BindableProperty.Create("Button3Command", typeof(ICommand), typeof(MainTabBarView), default(ICommand));

        /// <summary>
        /// The command to bind to for the third button.
        /// </summary>
        public ICommand Button3Command
        {
            get { return (ICommand)GetValue(Button3CommandProperty); }
            set { SetValue(Button3CommandProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button1FontFamily"/>
        /// </summary>
        public static readonly BindableProperty Button1FontFamilyProperty =
            BindableProperty.Create("Button1FontFamily", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The font family to use for text on the first button
        /// </summary>
        public string Button1FontFamily
        {
            get { return (string)GetValue(Button1FontFamilyProperty); }
            set { SetValue(Button1FontFamilyProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button2FontFamily"/>
        /// </summary>
        public static readonly BindableProperty Button2FontFamilyProperty =
            BindableProperty.Create("Button2FontFamily", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The font family to use for text on the second button
        /// </summary>
        public string Button2FontFamily
        {
            get { return (string)GetValue(Button2FontFamilyProperty); }
            set { SetValue(Button2FontFamilyProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button3FontFamily"/>
        /// </summary>
        public static readonly BindableProperty Button3FontFamilyProperty =
            BindableProperty.Create("Button3FontFamily", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The font family to use for text on the third button
        /// </summary>
        public string Button3FontFamily
        {
            get { return (string)GetValue(Button3FontFamilyProperty); }
            set { SetValue(Button3FontFamilyProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button1Text"/>
        /// </summary>
        public static readonly BindableProperty Button1TextProperty =
            BindableProperty.Create("Button1Text", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The text to use for text on the first button
        /// </summary>
        public string Button1Text
        {
            get { return (string)GetValue(Button1TextProperty); }
            set { SetValue(Button1TextProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button2Text"/>
        /// </summary>
        public static readonly BindableProperty Button2TextProperty =
            BindableProperty.Create("Button2Text", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The text to use for text on the second button
        /// </summary>
        public string Button2Text
        {
            get { return (string)GetValue(Button2TextProperty); }
            set { SetValue(Button2TextProperty, value); }
        }

        /// <summary>
        /// See <see cref="Button3Text"/>
        /// </summary>
        public static readonly BindableProperty Button3TextProperty =
            BindableProperty.Create("Button3Text", typeof(string), typeof(MainTabBarView), default(string));

        /// <summary>
        /// The text to use for text on the third button
        /// </summary>
        public string Button3Text
        {
            get { return (string)GetValue(Button3TextProperty); }
            set { SetValue(Button3TextProperty, value); }
        }

        #endregion

        /// <summary>
        /// Call this method to show the toolbar content.
        /// </summary>
        /// <returns></returns>
        public async Task Show()
        {
            if (!this.isShowing)
            {
                this.ToolBarContent.CancelAnimations();
                this.Button2.CancelAnimations();
                this.isShowing = true;
                Task translateToolBarTask = this.ToolBarContent.TranslateTo(0, 0, DurationInMs, Easing.SinIn);
                Task translateMiddleButtonTask = this.Button2.TranslateTo(0, 0, DurationInMs * 2, Easing.SinIn);
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
        /// <returns></returns>
        public async Task Hide()
        {
            if (this.isShowing)
            {
                this.isShowing = false;
                this.Button2.CancelAnimations();
                this.ToolBarContent.CancelAnimations();
                Task translateMiddleButtonTask = this.Button2.TranslateTo(0, 30, DurationInMs, Easing.SinOut);
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