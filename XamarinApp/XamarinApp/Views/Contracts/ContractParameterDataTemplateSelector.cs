﻿using Xamarin.Forms;
using XamarinApp.Models;

namespace XamarinApp.Views.Contracts
{
    public class ContractParameterDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Parameter1 { get; set; }
        public DataTemplate Parameter2 { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            ParameterModel model = (ParameterModel)item;
            return Parameter1;
        }
    }
}