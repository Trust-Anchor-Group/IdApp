using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.Services;

namespace XamarinApp.Views.Contracts
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NotaryMenuPage
    {
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

        public NotaryMenuPage()
        {
            this.BindingContext = this;
            InitializeComponent();
            this.navigationService = DependencyService.Resolve<INavigationService>();
        }

		private async void NewContract_Clicked(object sender, EventArgs e)
		{
            await this.navigationService.PushAsync(new NewContractPage(ContractTypesPerCategory));
        }

        private async void BackButton_Clicked(object sender, EventArgs e)
        {
            await this.navigationService.PopAsync();
        }

        protected override bool OnBackButtonPressed()
        {
            this.BackButton_Clicked(this.BackButton, EventArgs.Empty);
            return true;
        }
    }
}