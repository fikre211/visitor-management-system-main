using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace GatePass.MS.ClientApp.Middleware
{

    public class IdentityPagesRouteConvention : IPageRouteModelConvention
    {
        private readonly string _companyRoutePrefix;

        public IdentityPagesRouteConvention(string companyRoutePrefix)
        {
            _companyRoutePrefix = companyRoutePrefix;
        }

        public void Apply(PageRouteModel model)
        {
            if (model.AreaName?.Equals("Identity", StringComparison.OrdinalIgnoreCase) == true)
            {
                foreach (var selector in model.Selectors.ToList())
                {
                    var originalTemplate = selector.AttributeRouteModel.Template;

                    // Insert {companyName} prefix before the route
                    var newTemplate = $"{_companyRoutePrefix}/{originalTemplate}";

                    model.Selectors.Add(new SelectorModel
                    {
                        AttributeRouteModel = new AttributeRouteModel
                        {
                            Template = newTemplate
                        }
                    });
                }
            }
        }
    }

}

