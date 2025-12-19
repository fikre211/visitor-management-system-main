using Microsoft.AspNetCore.Mvc.Filters;

namespace GatePass.MS.ClientApp.Filters
{
    public class NoCacheFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var response = context.HttpContext.Response;
            response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            response.Headers["Pragma"] = "no-cache";
            response.Headers["Expires"] = "0";
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
