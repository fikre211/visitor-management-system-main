using System.Threading.Tasks;
using GatePass.MS.ClientApp.Data;
using GatePass.MS.ClientApp.Service;
using GatePass.MS.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace GatePass.MS.ClientApp.Middleware
{
    public class CompanyResolutionMiddleware
    {
        private readonly RequestDelegate _next;
        public CompanyResolutionMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext ctx, ApplicationDbContext db, ICurrentCompany cc)
        {
            var rd = ctx.GetRouteData();
            if (rd.Values.TryGetValue("companyName", out var raw) && raw is string slug)
            {
                var company = await db.Company
                                      .FirstOrDefaultAsync(c => c.Slug == slug);
                if (company == null)
                {
                    ctx.Response.StatusCode = 404;
                    await ctx.Response.WriteAsync($"Company '{slug}' not found.");
                    return;
                }
                // stash it for controllers & views
                cc.Value = company;
                ctx.Items["Company"] = company;
            }
            await _next(ctx);
        }
    }
}
