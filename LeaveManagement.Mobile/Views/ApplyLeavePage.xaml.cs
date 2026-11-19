using LeaveManagement.Mobile.Models;
using LeaveManagement.Mobile.Services;

namespace LeaveManagement.Mobile.Views;

public partial class ApplyLeavePage : ContentPage
{
    private readonly SessionService _session;
    private readonly ApiService _api;
    private bool _hasLoaded;

    public ApplyLeavePage()
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
        ApplyLeaveContent.Clear();

        var result = await _api.GetLeaveTypesAsync();
        if (!result.Success || result.Value is null)
        {
            StatusLabel.Text = result.Message;
            return;
        }

        var activeLeaveTypes = result.Value.Where(x => x.IsActive).ToList();
        if (activeLeaveTypes.Count == 0)
        {
            StatusLabel.Text = "No active leave types are available right now.";
            return;
        }

        StatusLabel.Text = "Submit a leave request";
        var leaveTypePicker = new Picker { Title = "Select leave type" };
        foreach (var leaveType in activeLeaveTypes)
        {
            leaveTypePicker.Items.Add($"{leaveType.Leavetypeid} - {leaveType.Leavetypename}");
        }

        var fromPicker = new DatePicker { Date = DateTime.Today.AddDays(1) };
        var toPicker = new DatePicker { Date = DateTime.Today.AddDays(2) };
        var reasonEntry = new Entry { Placeholder = "Reason" };
        var submit = new Button { Text = "Apply" };

        submit.Clicked += async (_, _) =>
        {
            if (leaveTypePicker.SelectedIndex < 0)
            {
                StatusLabel.Text = "Please select a leave type.";
                return;
            }

            var fromDate = fromPicker.Date ?? DateTime.Today;
            var toDate = toPicker.Date ?? DateTime.Today;
            if (toDate < fromDate)
            {
                StatusLabel.Text = "To date cannot be before from date.";
                return;
            }

            var selectedLeaveTypeId = activeLeaveTypes[leaveTypePicker.SelectedIndex].Leavetypeid;
            var response = await _api.ApplyLeaveAsync(new ApplyLeaveRequest
            {
                Employeeid = _session.EmployeeId,
                Leavetypeid = selectedLeaveTypeId,
                Fromdate = fromDate,
                Todate = toDate,
                Reason = reasonEntry.Text ?? string.Empty
            });

            StatusLabel.Text = response.Success ? "Leave request submitted." : response.Message;
        };

        ApplyLeaveContent.Add(new Label { Text = "Leave Type" });
        ApplyLeaveContent.Add(leaveTypePicker);
        ApplyLeaveContent.Add(new Label { Text = "From date" });
        ApplyLeaveContent.Add(fromPicker);
        ApplyLeaveContent.Add(new Label { Text = "To date" });
        ApplyLeaveContent.Add(toPicker);
        ApplyLeaveContent.Add(new Label { Text = "Reason" });
        ApplyLeaveContent.Add(reasonEntry);
        ApplyLeaveContent.Add(submit);
    }
}
