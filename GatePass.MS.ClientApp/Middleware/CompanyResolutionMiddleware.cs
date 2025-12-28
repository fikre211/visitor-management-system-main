using GatePass.MS.ClientApp.Data;
using GatePass.MS.ClientApp.Service;
using GatePass.MS.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GatePass.MS.ClientApp.Middleware
{
    [ResponseCache(NoStore = true, Duration = 0, Location = ResponseCacheLocation.None)]
    public class CompanyResolutionMiddleware
    {
        private readonly RequestDelegate _next;
        public CompanyResolutionMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext ctx, ApplicationDbContext db, ICurrentCompany cc, UserManager<ApplicationUser> userManager)
        {
            var rd = ctx.GetRouteData();
            if (rd.Values.TryGetValue("companyName", out var raw) && raw is string requestedSlug)
            {
                var company = await db.Company.FirstOrDefaultAsync(c => c.Slug == requestedSlug);

                if (company == null)
                {
                    ctx.Response.StatusCode = 404;
                    await ctx.Response.WriteAsync($"Company '{requestedSlug}' not found.");
                    return;
                }

                cc.Value = company;
                ctx.Items["Company"] = company;

                if (ctx.User?.Identity?.IsAuthenticated == true)
                {
                    var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        // 1. UPDATE: Include Company in the query so we can get the correct Slug later
                        var user = await userManager.Users
                            .Include(u => u.Employee)
                                .ThenInclude(e => e.Department)
                                    .ThenInclude(d => d.Company)
                            .SingleOrDefaultAsync(u => u.Id == userId);

                        if (user != null)
                        {
                            var isSuperUser = string.Equals(user.UserName, "superUser@gmail.com", System.StringComparison.OrdinalIgnoreCase);
                            var isAdminRole = await userManager.IsInRoleAsync(user, "Admin");
                            var userCompanyId = user.Employee?.Department?.CompanyId;

                            // Check mismatch
                            if (userCompanyId.HasValue && userCompanyId.Value != company.Id && !isAdminRole && !isSuperUser)
                            {
                                // 2. UPDATE: Redirect instead of 403
                                // Get the user's correct company slug
                                var correctSlug = user.Employee?.Department?.Company?.Slug;

                                if (!string.IsNullOrEmpty(correctSlug))
                                {
                                    // Replace the requested (wrong) slug in the path with the correct one
                                    // Example: /wrong-corp/dashboard -> /right-corp/dashboard
                                    var currentPath = ctx.Request.Path.Value;
                                    var newPath = currentPath.Replace($"/{requestedSlug}", $"/{correctSlug}", System.StringComparison.OrdinalIgnoreCase);

                                    ctx.Response.Redirect(newPath);
                                    return;
                                }
                                else
                                {
                                    // Fallback if we somehow can't find their company slug
                                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                                    await ctx.Response.WriteAsync("Access denied: You belong to a company, but we could not resolve its URL.");
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            await _next(ctx);
        }
    }
}