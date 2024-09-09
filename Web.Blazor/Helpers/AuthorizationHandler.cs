using Microsoft.AspNetCore.Authorization;
using System.Net;

namespace Web.Blazor.Helpers;

public sealed class AuthorizationHandler : IAuthorizationHandler
{
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        if (context.Resource is not DefaultHttpContext httpContext)
        {
            return Task.CompletedTask;
        }

        var authHeader = httpContext.Request.Headers.Authorization;

        if (string.IsNullOrEmpty(authHeader))
        {
            return Task.CompletedTask;
        }

        var pass = authHeader.ToString()[AuthenticationSchemes.Basic.ToString().Length..].Trim();

        var apiPassword = Environment.GetEnvironmentVariable("ApiPass")!;

        if (!pass.Equals(apiPassword))
        {
            return Task.CompletedTask;
        }

        context.Succeed(context.PendingRequirements.First());

        return Task.CompletedTask;
    }
}