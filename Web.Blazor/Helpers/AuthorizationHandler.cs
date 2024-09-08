using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Text;
using System.Web.Http;

namespace Web.Blazor.Helpers;

public sealed class AuthorizationHandler : IAuthorizationHandler
{
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        if (context.Resource is not DefaultHttpContext httpContext)
        {
            throw new HttpResponseException(HttpStatusCode.Unauthorized);
        }

        var authHeader = httpContext.Request.Headers.Authorization;

        if (string.IsNullOrEmpty(authHeader))
        {
            throw new HttpResponseException(HttpStatusCode.Unauthorized);
        }

        var encodedUsernamePassword = authHeader.ToString()["Basic ".Length..].Trim();
        var encoded = Encoding.GetEncoding("iso-8859-1").GetString(Convert.FromBase64String(encodedUsernamePassword));
        var pass = encoded.Split(':')[1];

        var apiPassword = Environment.GetEnvironmentVariable("ApiPass")!;

        if (!pass.Equals(apiPassword))
        {
            throw new HttpResponseException(HttpStatusCode.Unauthorized);
        }

        context.Succeed(context.PendingRequirements.First());

        return Task.CompletedTask;
    }
}