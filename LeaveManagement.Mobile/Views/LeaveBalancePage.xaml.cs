using LeaveManagement.Mobile.Models;
using LeaveManagement.Mobile.Services;

namespace LeaveManagement.Mobile.Views;

public partial class LeaveBalancePage : ContentPage
{
    private readonly SessionService _session;
    private readonly ApiService _api;
    private bool _hasLoaded;

    public LeaveBalancePage()
    {
        InitializeComponent();
        _session = App.Services.GetRequiredService<SessionService>();
        _api = App.Services.GetRequiredService<ApiService>();
        SignedInLabel.Text = $"Signed in as {_session.Username} ({_session.Role})";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_hasLoaded)
        {
            _hasLoaded = true;
            await LoadAsync();
        }
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadAsync();
    }

    private void OnSignOutClicked(object? sender, EventArgs e)
    {
        _session.Clear();
        App.GoToLogin();
    }

    private async Task LoadAsync()
    {
        StatusLabel.Text = "Loading leave balance...";
        BalanceContent.Clear();

        var result = await _api.GetLeaveBalanceAsync(_session.EmployeeId);
        if (!result.Success || result.Value is null)
        {
            StatusLabel.Text = result.Message;
            return;
        }

        if (result.Value.Count == 0)
        {
            StatusLabel.Text = "Leave balance is not available for your account yet.";
            return;
        }

        StatusLabel.Text = $"Balance for employee with Employee ID - {_session.EmployeeId}";
        foreach (var item in result.Value)
        {
            BalanceContent.Add(new Border
            {
                Stroke = Color.FromArgb("#D0D0D0"),
                StrokeThickness = 1,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(12),
                Content = new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        new Label
                        {
                            Text = item.LeaveType,
                            FontAttributes = FontAttributes.Bold,
                            FontSize = 16
                        },
                        new Label
                        {
                            Text = $"{item.RemainingDays}/{item.TotalDays} days remaining"
                        },
                        new Label
                        {
                            Text = $"{item.UsedDays} days used",
                            TextColor = Colors.Gray,
                            FontSize = 12
                        }
                    }
                }
            });
        }
    }
}
