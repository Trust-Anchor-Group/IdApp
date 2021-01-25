using System;
using System.Collections.Generic;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.Services;
using XamarinApp.ViewModels;

namespace XamarinApp.Views
{
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