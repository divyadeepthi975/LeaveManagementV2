using LeaveManagement.Mobile.Models;
using LeaveManagement.Mobile.Services;

namespace LeaveManagement.Mobile.Views;

public partial class MyLeavesPage : ContentPage
{
    private readonly SessionService _session;
    private readonly ApiService _api;
    private bool _hasLoaded;

    public MyLeavesPage()
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
        StatusLabel.Text = "Loading...";
        LeavesContent.Clear();

        var result = await _api.GetLeavesAsync(_session.EmployeeId);
        if (!result.Success || result.Value is null)
        {
            StatusLabel.Text = result.Message;
            return;
        }

        if (result.Value.Count == 0)
        {
            StatusLabel.Text = "Leaves are not applied by you yet.";
            return;
        }

        StatusLabel.Text = $"You applied {result.Value.Count} leave requests";
        foreach (var item in result.Value)
        {
            LeavesContent.Add(new Label { Text = FormatLeave(item) });
        }
    }

    internal static string FormatLeave(LeaveRequestDto item)
    {
        var managerMessage = string.Empty;
        if (!item.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
        {
            var byText = string.IsNullOrWhiteSpace(item.Approvedby) ? "Manager" : item.Approvedby;
            var commentsText = string.IsNullOrWhiteSpace(item.Comments) ? "No reason provided." : item.Comments;
            managerMessage = $" | {byText}: {commentsText}";
        }

        return $"#{item.Leaverequestid} Emp:{item.Employeeid} Type:{item.Leavetypeid} {item.Fromdate:yyyy-MM-dd} to {item.Todate:yyyy-MM-dd} | {item.Status} | Reason: {item.Reason}{managerMessage}";
    }
}
