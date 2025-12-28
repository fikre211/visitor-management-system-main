using GatePass.MS.ClientApp.Middleware;


using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace GatePass.MS.ClientApp.Middleware
{
    /// <summary>
    /// Prevents browser caching for dynamic pages so Back/Forward does not show stale auth state.
    /// </summary>
    public class NoCacheHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public NoCacheHeadersMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            // Set headers just before the response starts
            context.Response.OnStarting(() =>
            {
                // Skip typical static assets by extension to avoid interfering with CDN/caching of static files
                var path = context.Request.Path.Value ?? string.Empty;
                var lower = path.ToLowerInvariant();
                var isStatic = lower.EndsWith(".js") || lower.EndsWith(".css") || lower.EndsWith(".png")
                               || lower.EndsWith(".jpg") || lower.EndsWith(".jpeg") || lower.EndsWith(".svg")
                               || lower.Contains("/_framework/") || lower.Contains("/lib/") || lower.Contains("/_content/");

                if (!isStatic)
                {
                    context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
                    context.Response.Headers["Pragma"] = "no-cache";
                    context.Response.Headers["Expires"] = "-1";
                }

                return Task.CompletedTask;
            });

            await _next(context);
        }
    }

    public static class NoCacheHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseNoCacheHeaders(this IApplicationBuilder app) =>
            app.UseMiddleware<NoCacheHeadersMiddleware>();
    }
}