using LeaveManagement.Mobile.Models;
using LeaveManagement.Mobile.Services;

namespace LeaveManagement.Mobile.Views;

public partial class DashboardPage : ContentPage
{
    private readonly SessionService _session;
    private readonly ApiService _api;
    private bool _hasLoaded;

    public DashboardPage()
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
        StatusLabel.Text = "Loading dashboard...";
        DashboardContent.Clear();

        var result = await _api.GetDashboardSummaryAsync();
        if (!result.Success || result.Value is null)
        {
            StatusLabel.Text = result.Message;
            return;
        }

        StatusLabel.Text = string.Empty;
        var summary = result.Value;
        var isManager = _session.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase);

        DashboardContent.Add(new Label
        {
            Text = $"{(isManager ? "Team Employees" : "Total Employees")}: {summary.TotalEmployees}"
        });
        DashboardContent.Add(new Label
        {
            Text = $"{(isManager ? "Active Team Employees" : "Active Employees")}: {summary.ActiveEmployees}"
        });
        DashboardContent.Add(new Label
        {
            Text = $"{(isManager ? "Team Pending Requests" : "Pending Requests")}: {summary.PendingRequests}"
        });
        DashboardContent.Add(new Label
        {
            Text = $"{(isManager ? "Approved by You" : "Approved Requests")}: {summary.ApprovedRequests}"
        });
        DashboardContent.Add(new Label
        {
            Text = $"{(isManager ? "Rejected by You" : "Rejected Requests")}: {summary.RejectedRequests}"
        });
        DashboardContent.Add(new Label
        {
            Text = $"{(isManager ? "Team Approved Leave Days" : "Total Leaves Taken")}: {summary.TotalLeavesTaken}"
        });
    }
}
