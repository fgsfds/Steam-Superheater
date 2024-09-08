using Microsoft.AspNetCore.Authorization;
using System.Text;

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

        var encodedUsernamePassword = authHeader.ToString()["Basic ".Length..].Trim();
        var encoded = Encoding.GetEncoding("iso-8859-1").GetString(Convert.FromBase64String(encodedUsernamePassword));
        var pass = encoded.Split(':')[1];

        var apiPassword = Environment.GetEnvironmentVariable("ApiPass")!;

        if (!pass.Equals(apiPassword))
        {
            return Task.CompletedTask;
        }

        context.Succeed(context.PendingRequirements.First());

        return Task.CompletedTask;
    }
}