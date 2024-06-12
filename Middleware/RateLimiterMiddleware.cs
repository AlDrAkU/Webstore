using System.Threading.RateLimiting;

namespace Webstore.Middleware
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class RateLimiterMiddleware : IMiddleware
    {
        private static readonly RateLimiter _rateLimiter = new TokenBucketRateLimiter(new()
        {
            AutoReplenishment = true,
            TokenLimit = 10,
            TokensPerPeriod = 10,
            ReplenishmentPeriod = TimeSpan.FromSeconds(10)
        });
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            using var lease = _rateLimiter.AttemptAcquire();
            if (lease.IsAcquired)
            {
                await next(context);
            }
            else 
            {
                ReturnErrorToClient(context);
            }
        }

        private async Task ReturnErrorToClient(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.Append("Retry-After", "10");

            var response = new
            {
                status = StatusCodes.Status429TooManyRequests,
                message = "Too many requests"
            };

            await context.Response.WriteAsJsonAsync(response);
        }

    }
}