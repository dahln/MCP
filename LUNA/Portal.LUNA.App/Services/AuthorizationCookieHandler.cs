using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Portal.LUNA.App.Services;

public class AuthorizationCookieHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        return base.SendAsync(request, cancellationToken);
    }
}
