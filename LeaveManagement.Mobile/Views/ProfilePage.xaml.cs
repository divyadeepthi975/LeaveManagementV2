using LeaveManagement.Mobile.Models;
using LeaveManagement.Mobile.Services;

namespace LeaveManagement.Mobile.Views;

public partial class ProfilePage : ContentPage
{
    private readonly SessionService _session;
    private readonly ApiService _api;
    private bool _showUpdateForm;
 
    private Entry? _nameEntry;
    private Entry? _emailEntry;
    private Entry? _departmentEntry;
    private DatePicker? _joiningDatePicker;

    public ProfilePage()
    {
        InitializeComponent();
        _session = App.Services.GetRequiredService<SessionService>();
        _api = App.Services.GetRequiredService<ApiService>();
        SignedInLabel.Text = $"Signed in as {_session.Username} ({_session.Role})";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadProfileAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        StatusLabel.Text = "Loading profile...";
        ProfileContent.Clear();

        var result = await _api.GetMyDetailsAsync();
        if (!result.Success || result.Value is null)
        {
            StatusLabel.Text = result.Message;
            return;
        }

        var current = result.Value;
        StatusLabel.Text = string.Empty;
        RenderProfile(current);
    }

    private void RenderProfile(EmployeeDto current)
    {
        ProfileContent.Clear();
        ProfileContent.Add(new Label{Text= $"EmployeeId:{current.EmployeeId}"});
        ProfileContent.Add(new Label{Text=$"EmployeeCode:{current.EmployeeCode}"});
        ProfileContent.Add(new Label { Text = $"Name: {current.Name}" });
        ProfileContent.Add(new Label { Text = $"Email: {current.Email}" });
        ProfileContent.Add(new Label { Text = $"Department: {current.Department}" });
        ProfileContent.Add(new Label { Text = $"Role: {current.Role}" });
        ProfileContent.Add(new Label { Text = $"Manager Id: {(current.ManagerId?.ToString() ?? "No Manager")}" });
        ProfileContent.Add(new Label { Text = $"Manager Name: {(!string.IsNullOrWhiteSpace(current.ManagerName) ? current.ManagerName : "No Manager")}" });

        if (_session.Role.Equals("Employee", StringComparison.OrdinalIgnoreCase) ||
            _session.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
        {
            var toggleUpdate = new Button
            {
                Text = _showUpdateForm ? "Hide Update Profile" : "Update Profile",
                FontSize = 12,
                Padding = new Thickness(8, 2),
                HeightRequest = 30
            };
            toggleUpdate.Clicked += (_, _) =>
            {
                _showUpdateForm = !_showUpdateForm;
                RenderProfile(current);
            };
            ProfileContent.Add(toggleUpdate);

            if (_showUpdateForm)
            {
                RenderUpdateForm(current);
            }
        }
    }

    private void RenderUpdateForm(EmployeeDto current)
    {
        ProfileContent.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        ProfileContent.Add(new Label { Text = "Update profile", FontAttributes = FontAttributes.Bold });

       
        _nameEntry = new Entry { Placeholder = "Name", Text = current.Name };
        _emailEntry = new Entry { Placeholder = "Email", Text = current.Email };
        _departmentEntry = new Entry { Placeholder = "Department", Text = current.Department };
        _joiningDatePicker = new DatePicker { Date = current.JoiningDate == default ? DateTime.Today : current.JoiningDate };

        var managerDetails = new Label
        {
            Text = $"Manager Id: {(current.ManagerId?.ToString() ?? "No Manager")} | Manager Name: {(!string.IsNullOrWhiteSpace(current.ManagerName) ? current.ManagerName : "Not assigned")}",
            FontSize = 12,
            TextColor = Colors.Gray
        };

        var save = new Button { Text = "Save Profile" };
        save.Clicked += async (_, _) => await SaveProfileAsync(current);

        ProfileContent.Add(_nameEntry);
        ProfileContent.Add(_emailEntry);
        ProfileContent.Add(_departmentEntry);
        ProfileContent.Add(managerDetails);
        ProfileContent.Add(_joiningDatePicker);
        ProfileContent.Add(save);
    }

    private async Task SaveProfileAsync(EmployeeDto current)
    {
        if (string.IsNullOrWhiteSpace(_nameEntry?.Text) ||
            string.IsNullOrWhiteSpace(_emailEntry?.Text) ||
            string.IsNullOrWhiteSpace(_departmentEntry?.Text) ||
            _joiningDatePicker is null)
        {
            StatusLabel.Text = "Fill all profile fields.";
            return;
        }

        var payload = new EmployeeDto
        {
            EmployeeId = _session.EmployeeId,
            EmployeeCode = current.EmployeeCode,
            Name = _nameEntry.Text,
            Email = _emailEntry.Text,
            Department = _departmentEntry.Text,
            JoiningDate = _joiningDatePicker.Date ?? DateTime.Today,
            IsActive = current.IsActive,
            ManagerId = current.ManagerId,
            ManagerName = current.ManagerName,
            Role = _session.Role
        };

        var response = await _api.UpdateEmployeeAsync(_session.EmployeeId, payload);
        StatusLabel.Text = response.Success ? "Profile updated." : response.Message;
        if (response.Success)
        {
            _showUpdateForm = false;
            await LoadProfileAsync();
        }
    }

    private void OnSignOutClicked(object? sender, EventArgs e)
    {
        _session.Clear();
        App.GoToLogin();
    }
}
