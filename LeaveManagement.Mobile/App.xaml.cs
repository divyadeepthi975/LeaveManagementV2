using LeaveManagement.Mobile.Helpers;
using LeaveManagement.Mobile.Views;

namespace LeaveManagement.Mobile;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = default!;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        Services = services;
        GoToLogin();
    }


    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);
        window.Width = 440;
        window.Height = 820;
        window.Title = "Leave Management";
        return window;
    }

    public static void GoToLogin()
    {
        if (Current is null) return;
        Current.MainPage = Services.GetRequiredService<LoginPage>();
    }

    public static void GoToRoleShell(string role)
    {
        if (Current is null) return;
        Current.MainPage = RoleShellFactory.Create(role);
    }
}