using LeaveManagement.Mobile.Models;
using LeaveManagement.Mobile.Services;

namespace LeaveManagement.Mobile.Views;

public partial class ApprovalsPage : ContentPage
{
    private readonly SessionService _session;
    private readonly ApiService _api;
    private string _activeSubTab = "Pending";
    private bool _hasLoaded;

    public ApprovalsPage()
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
        ApprovalsContent.Clear();

        var result = await _api.GetLeavesAsync();
        if (!result.Success || result.Value is null)
        {
            StatusLabel.Text = result.Message;
            return;
        }

        ApprovalsContent.Add(new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            Content = new HorizontalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    CreateSubTabButton("Pending"),
                    CreateSubTabButton("Approved"),
                    CreateSubTabButton("Rejected")
                }
            }
        });
        ApprovalsContent.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });

        var filtered = result.Value
            .Where(x => x.Status.Equals(_activeSubTab, StringComparison.OrdinalIgnoreCase))
            .ToList();

        StatusLabel.Text = $"{filtered.Count} {_activeSubTab.ToLowerInvariant()} requests";
        if (filtered.Count == 0)
        {
            ApprovalsContent.Add(new Label
            {
                Text = $"No {_activeSubTab.ToLowerInvariant()} leave requests.",
                TextColor = Colors.Gray
            });
            return;
        }

        foreach (var item in filtered)
        {
            ApprovalsContent.Add(new Label { Text = MyLeavesPage.FormatLeave(item) });
            if (!_activeSubTab.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var approve = new Button { Text = $"Approve #{item.Leaverequestid}" };
            approve.Clicked += async (_, _) => await ApproveOrRejectAsync(item.Leaverequestid, true);

            var reject = new Button { Text = $"Reject #{item.Leaverequestid}" };
            reject.Clicked += async (_, _) => await ApproveOrRejectAsync(item.Leaverequestid, false);

            ApprovalsContent.Add(new HorizontalStackLayout
            {
                Spacing = 8,
                Children = { approve, reject }
            });
        }
    }

    private Button CreateSubTabButton(string title)
    {
        var selected = _activeSubTab == title;
        var button = new Button
        {
            Text = title,
            FontSize = 12,
            Padding = new Thickness(8, 2),
            HeightRequest = 34,
            BackgroundColor = selected ? Color.FromArgb("#512BD4") : Colors.Transparent,
            TextColor = selected ? Colors.White : Colors.Black,
            BorderColor = Color.FromArgb("#512BD4")
        };
        button.Clicked += async (_, _) =>
        {
            _activeSubTab = title;
            await LoadAsync();
        };
        return button;
    }

    private async Task ApproveOrRejectAsync(int requestId, bool approve)
    {
        var comments = await DisplayPromptAsync(
            approve ? "Approve leave" : "Reject leave",
            "Comments (optional)");

        var action = new LeaveActionRequest
        {
            Id = requestId,
            Approvedby = _session.Username,
            Comments = comments
        };

        var response = approve
            ? await _api.ApproveLeaveAsync(action)
            : await _api.RejectLeaveAsync(action);

        StatusLabel.Text = response.Success
            ? (approve ? "Leave approved." : "Leave rejected.")
            : response.Message;

        await LoadAsync();
    }
}
