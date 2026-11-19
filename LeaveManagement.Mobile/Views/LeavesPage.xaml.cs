using LeaveManagement.Mobile.Models;
using LeaveManagement.Mobile.Services;

namespace LeaveManagement.Mobile.Views;

public partial class LeavesPage : ContentPage
{
    private readonly SessionService _session;
    private readonly ApiService _api;
    private bool _hasLoaded;

    public LeavesPage()
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
        StatusLabel.Text = "Loading leaves...";
        LeavesContent.Clear();

        var result = await _api.GetLeavesAsync();
        if (!result.Success || result.Value is null)
        {
            StatusLabel.Text = result.Message;
            return;
        }

        StatusLabel.Text = $"{result.Value.Count} leave requests";
        if (result.Value.Count == 0)
        {
            LeavesContent.Add(new Label
            {
                Text = "No leave requests.",
                TextColor = Colors.Gray
            });
            return;
        }

        foreach (var item in result.Value)
        {
            var managerMessage = string.Empty;
            if (!item.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                var approvedBy = string.IsNullOrWhiteSpace(item.Approvedby)
                    ? "Manager"
                    : item.Approvedby;
                var comments = string.IsNullOrWhiteSpace(item.Comments)
                    ? "No reason provided."
                    : item.Comments;
                managerMessage = $" | {approvedBy}: {comments}";
            }

            LeavesContent.Add(new Label
            {
                Text = $"#{item.Leaverequestid} Emp:{item.Employeeid} Type:{item.Leavetypeid} " +
                       $"{item.Fromdate:yyyy-MM-dd} to {item.Todate:yyyy-MM-dd} | {item.Status} " +
                       $"| Reason: {item.Reason}{managerMessage}"
            });
        }
    }
}
