using LeaveManagement.Mobile.Helpers;
using LeaveManagement.Mobile.Services;
using LeaveManagement.Mobile.ViewModels;
using LeaveManagement.Mobile.Views;
using Microsoft.Extensions.Logging;

namespace LeaveManagement.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        var apiBaseAddress = new Uri(Constants.BaseUrl, UriKind.Absolute);

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Session + HTTP
        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddTransient<AuthHeaderHandler>();

        builder.Services
            .AddHttpClient<AuthService>(client =>
            {
                client.BaseAddress = apiBaseAddress;
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthHeaderHandler>();

        builder.Services
            .AddHttpClient<ApiService>(client =>
            {
                client.BaseAddress = apiBaseAddress;
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthHeaderHandler>();

        // Login screen
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<LoginPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}