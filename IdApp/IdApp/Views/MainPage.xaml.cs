using IdApp.Services;
using IdApp.ViewModels;
using System;
using System.Collections.Generic;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views
{
    /// <summary>
    /// A root, or main page, for the application. This is the starting point, from here you can navigate to other pages
    /// and take various actions.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage
    {
        private readonly ILogService logService;
        private readonly IUiDispatcher uiDispatcher;
        private readonly IContractOrchestratorService contractOrchestratorService;
        private readonly INavigationService navigationService;

        private static readonly SortedDictionary<string, SortedDictionary<string, string>> ContractTypesPerCategory =
            new SortedDictionary<string, SortedDictionary<string, string>>()
            {/*
                {
                    "Put Title of Contract Category here",
                    new SortedDictionary<string, string>()
                    {
                        { "Put Title of Contract Template here", "Put contract identity of template here." }
                    }
                }*/
            };

        /// <summary>
        /// Creates a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            this.uiDispatcher = DependencyService.Resolve<IUiDispatcher>();
            this.logService = DependencyService.Resolve<ILogService>();
            this.contractOrchestratorService = DependencyService.Resolve<IContractOrchestratorService>();
            this.navigationService = DependencyService.Resolve<INavigationService>();
        }

        private void IdCard_Tapped(object sender, EventArgs e)
        {
            this.IdCard.Flip();
        }
    }
}