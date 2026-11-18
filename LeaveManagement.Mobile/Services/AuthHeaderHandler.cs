using System.Net;
using System.Net.Http.Headers;
using LeaveManagement.Mobile.Helpers;

namespace LeaveManagement.Mobile.Services;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly SessionService _session;

    public AuthHeaderHandler(SessionService session) => _session = session;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_session.IsLoggedIn)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.Token);

        var response = await base.SendAsync(request, cancellationToken);

        var isLoginCall = request.RequestUri?.AbsolutePath
            .EndsWith(Constants.LoginPath, StringComparison.OrdinalIgnoreCase) == true;

        if (response.StatusCode == HttpStatusCode.Unauthorized && !isLoginCall)
        {
            _session.Clear();
            MainThread.BeginInvokeOnMainThread(() => App.GoToLogin());
        }

        return response;
    }
}