using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.Services;

namespace XamarinApp.Views.Contracts
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NotaryMenuPage : ContentPage, IBackButton
    {
        private readonly ITagService tagService;
        private readonly Page owner;

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

        public NotaryMenuPage(Page Owner)
        {
            InitializeComponent();
            this.tagService = DependencyService.Resolve<ITagService>();
            this.owner = Owner;
            this.BindingContext = this;
        }

        private void BackButton_Clicked(object sender, EventArgs e)
        {
            App.ShowPage(this.owner, true);
        }

		private void NewContract_Clicked(object sender, EventArgs e)
		{
            App.ShowPage(new NewContractPage(App.CurrentPage, ContractTypesPerCategory), false);
        }

        public bool BackClicked()
        {
            this.BackButton_Clicked(this, new EventArgs());
            return true;
        }

    }
}