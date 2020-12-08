using System;
using System.Globalization;

namespace XamarinApp.Models
{
    public class ContractModel
    {
        public ContractModel(string contractId, DateTime created, string name)
        {
            this.ContractId = contractId;
            this.Created = created.ToString(CultureInfo.CurrentUICulture);
            this.Name = name;
        }

        public string ContractId { get; }
        public string Created { get; }
        public string Name { get; }
    }
}