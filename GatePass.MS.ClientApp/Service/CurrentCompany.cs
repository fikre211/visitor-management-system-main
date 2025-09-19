using GatePass.MS.Domain;

namespace GatePass.MS.ClientApp.Service
{
    // ICurrentCompany.cs
    public interface ICurrentCompany
    {
        Company Value { get; set; }
    }

    // CurrentCompany.cs
    public class CurrentCompany : ICurrentCompany
    {
        public Company Value { get; set; }
    }

}
