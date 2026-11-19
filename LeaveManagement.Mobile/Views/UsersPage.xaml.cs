using LeaveManagement.Mobile.Models;
using LeaveManagement.Mobile.Services;

namespace LeaveManagement.Mobile.Views;

public partial class UsersPage : ContentPage
{
    private readonly SessionService _session;
    private readonly ApiService _api;
    private string _activeSubTab = "Create";
    private int? _selectedUserId;
    private bool _hasLoaded;

    public UsersPage()
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

    private async void OnRefreshClicked(object? sender, EventArgs e) => await LoadAsync();

    private void OnSignOutClicked(object? sender, EventArgs e)
    {
        _session.Clear();
        App.GoToLogin();
    }

    private async Task LoadAsync()
    {
        StatusLabel.Text = "Loading users...";
        UsersContent.Clear();

        if (!_session.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            StatusLabel.Text = "Users screen is available for Admin only.";
            return;
        }

        var result = await _api.GetAllUsersAsync();
        if (!result.Success || result.Value is null)
        {
            StatusLabel.Text = result.Message;
            return;
        }

        StatusLabel.Text = "Manage users";
        RenderSubNavigation();

        switch (_activeSubTab)
        {
            case "Employees":
                RenderUsersList(result.Value);
                break;
            case "Update":
                RenderUpdateSection(result.Value);
                break;
            case "Role":
                RenderRoleSection(result.Value);
                break;
            default:
                RenderCreateSection();
                break;
        }
    }

    private void RenderSubNavigation()
    {
        UsersContent.Add(new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            Content = new HorizontalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    CreateSubTabButton("Create"),
                    CreateSubTabButton("Get all users", "Employees"),
                    CreateSubTabButton("Update"),
                    CreateSubTabButton("Role")
                }
            }
        });
        UsersContent.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
    }

    private Button CreateSubTabButton(string text, string? key = null)
    {
        key ??= text;
        var selected = _activeSubTab == key;
        var button = new Button
        {
            Text = text,
            FontSize = 12,
            Padding = new Thickness(8, 2),
            HeightRequest = 34,
            BackgroundColor = selected ? Color.FromArgb("#512BD4") : Colors.Transparent,
            TextColor = selected ? Colors.White : Colors.Black,
            BorderColor = Color.FromArgb("#512BD4")
        };
        button.Clicked += async (_, _) =>
        {
            _activeSubTab = key;
            await LoadAsync();
        };
        return button;
    }

    private void RenderCreateSection()
    {
        UsersContent.Add(new Label { Text = "Create user", FontAttributes = FontAttributes.Bold });

        var code = new Entry { Placeholder = "Employee code" };
        var name = new Entry { Placeholder = "Name" };
        var email = new Entry { Placeholder = "Email", Keyboard = Keyboard.Email };
        var password = new Entry { Placeholder = "Password", IsPassword = true };
        var department = new Entry { Placeholder = "Department" };
        var managerId = new Entry { Placeholder = "Manager id", Keyboard = Keyboard.Numeric };
        var joinDate = new DatePicker { Date = DateTime.Today };

        var createEmployee = new Button { Text = "Create Employee" };
        createEmployee.Clicked += async (_, _) =>
            await CreateUserAsync(false, code.Text, name.Text, email.Text, password.Text,
                department.Text, managerId.Text, joinDate.Date ?? DateTime.Today);

        var createManager = new Button { Text = "Create Manager" };
        createManager.Clicked += async (_, _) =>
            await CreateUserAsync(true, code.Text, name.Text, email.Text, password.Text,
                department.Text, managerId.Text, joinDate.Date ?? DateTime.Today);

        UsersContent.Add(code);
        UsersContent.Add(name);
        UsersContent.Add(email);
        UsersContent.Add(password);
        UsersContent.Add(department);
        UsersContent.Add(managerId);
        UsersContent.Add(joinDate);
        UsersContent.Add(new HorizontalStackLayout
        {
            Spacing = 6,
            Children = { createEmployee, createManager }
        });
    }

    private async Task CreateUserAsync(bool isManager, string? code, string? name, string? email,
        string? password, string? department, string? managerIdText, DateTime joiningDate)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(department))
        {
            StatusLabel.Text = "Fill all user fields.";
            return;
        }

        if (!int.TryParse(managerIdText, out var managerId))
        {
            StatusLabel.Text = "Manager id is required and must be a valid number.";
            return;
        }

        var payload = new CreateUserRequest
        {
            EmployeeCode = code,
            Name = name,
            Email = email,
            Password = password,
            Department = department,
            JoiningDate = joiningDate,
            ManagerId = managerId
        };

        var response = isManager
            ? await _api.CreateManagerAsync(payload)
            : await _api.CreateAdminEmployeeAsync(payload);

        var successMessage = isManager ? "Manager created successfully." : "Employee created successfully.";
        StatusLabel.Text = response.Success ? successMessage : response.Message;
        if (response.Success)
        {
            await DisplayAlert("Success", successMessage, "OK");
            await LoadAsync();
        }
        else
        {
            await DisplayAlert("Creation failed", response.Message, "OK");
        }
    }

    private void RenderUsersList(List<EmployeeDto> users)
    {
        UsersContent.Add(new Label { Text = "All employees", FontAttributes = FontAttributes.Bold });
        foreach (var user in users)
        {
            var row = new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label
                    {
                        Text = $"{user.EmployeeId} - {user.EmployeeCode} | {user.Name} | {user.Email} | {user.Department} | Role:{user.Role} | Active:{user.IsActive} | ManagerId:{user.ManagerId?.ToString() ?? "NA"} | Manager:{(string.IsNullOrWhiteSpace(user.ManagerName) ? "NA" : user.ManagerName)}",
                        VerticalOptions = LayoutOptions.Center
                    }
                }
            };

            if (user.EmployeeId != _session.EmployeeId)
            {
                var isActive = user.IsActive;
                var toggle = new Button
                {
                    Text = isActive ? "Deactivate" : "Activate",
                    FontSize = 12,
                    Padding = new Thickness(8, 2),
                    HeightRequest = 34,
                    TextColor = isActive ? Color.FromArgb("#D9534F") : Color.FromArgb("#2E7D32"),
                    BorderColor = isActive ? Color.FromArgb("#D9534F") : Color.FromArgb("#2E7D32")
                };
                toggle.Clicked += async (_, _) => await ChangeStatusAsync(user.EmployeeId, !isActive);
                row.Children.Add(toggle);
            }
            UsersContent.Add(row);
        }
    }

    private void RenderUpdateSection(List<EmployeeDto> users)
    {
        UsersContent.Add(new Label { Text = "Select employee for update", FontAttributes = FontAttributes.Bold });
        _selectedUserId = null;

        foreach (var user in users)
        {
            var row = new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = $"{user.EmployeeId} - {user.Name} ({user.Role})", VerticalOptions = LayoutOptions.Center }
                }
            };
            if (user.EmployeeId != _session.EmployeeId)
            {
                var edit = new Button { Text = "Update", FontSize = 12, HeightRequest = 34 };
                edit.Clicked += (_, _) => PopulateUserForm(user);
                row.Children.Add(edit);
            }
            UsersContent.Add(row);
        }

        UsersContent.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        UsersContent.Add(new Label { Text = "Update selected employee", FontAttributes = FontAttributes.Bold });

        var code = new Entry { Placeholder = "Employee code" };
        var name = new Entry { Placeholder = "Name" };
        var email = new Entry { Placeholder = "Email", Keyboard = Keyboard.Email };
        var department = new Entry { Placeholder = "Department" };
        var managerId = new Entry { Placeholder = "Manager id", Keyboard = Keyboard.Numeric };
        var joinDate = new DatePicker { Date = DateTime.Today };
        PopulateFormControls = (user) =>
        {
            _selectedUserId = user.EmployeeId;
            code.Text = user.EmployeeCode;
            name.Text = user.Name;
            email.Text = user.Email;
            department.Text = user.Department;
            managerId.Text = user.ManagerId?.ToString() ?? string.Empty;
            joinDate.Date = user.JoiningDate == default ? DateTime.Today : user.JoiningDate;
            StatusLabel.Text = $"Loaded employee {user.EmployeeId} for update.";
        };

        var update = new Button { Text = "Update Employee" };
        update.Clicked += async (_, _) =>
        {
            if (_selectedUserId is not int userId)
            {
                StatusLabel.Text = "Select an employee to update.";
                return;
            }
            await UpdateUserAsync(userId, code.Text, name.Text, email.Text,
                department.Text, managerId.Text, joinDate.Date ?? DateTime.Today);
        };

        UsersContent.Add(code);
        UsersContent.Add(name);
        UsersContent.Add(email);
        UsersContent.Add(department);
        UsersContent.Add(managerId);
        UsersContent.Add(joinDate);
        UsersContent.Add(update);
    }

    private Action<EmployeeDto>? PopulateFormControls;

    private void PopulateUserForm(EmployeeDto user) => PopulateFormControls?.Invoke(user);

    private async Task UpdateUserAsync(int userId, string? code, string? name, string? email,
        string? department, string? managerIdText, DateTime joiningDate)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(department) ||
            !int.TryParse(managerIdText, out var managerId))
        {
            StatusLabel.Text = "Fill all fields and enter a valid manager id.";
            return;
        }

        var existing = await _api.GetAdminEmployeeByIdAsync(userId);
        if (!existing.Success || existing.Value is null)
        {
            StatusLabel.Text = existing.Message;
            return;
        }

        var payload = new EmployeeDto
        {
            EmployeeId = userId,
            EmployeeCode = code,
            Name = name,
            Email = email,
            Department = department,
            JoiningDate = joiningDate,
            IsActive = existing.Value.IsActive,
            ManagerId = managerId,
            Role = existing.Value.Role
        };

        var response = await _api.UpdateAdminEmployeeAsync(userId, payload);
        StatusLabel.Text = response.Success ? "Employee updated." : response.Message;
        if (response.Success)
        {
            await LoadAsync();
        }
    }

    private void RenderRoleSection(List<EmployeeDto> users)
    {
        UsersContent.Add(new Label { Text = "Change role", FontAttributes = FontAttributes.Bold });
        foreach (var user in users)
        {
            var row = new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label
                    {
                        Text = $"{user.EmployeeId} - {user.Name} | Role:{user.Role} | Active:{(user.IsActive ? "Yes" : "No")} | ManagerId:{user.ManagerId?.ToString() ?? "NA"}",
                        VerticalOptions = LayoutOptions.Center
                    }
                }
            };

            if (user.Role.Equals("Employee", StringComparison.OrdinalIgnoreCase))
            {
                var convert = new Button
                {
                    Text = "Convert to Manager",
                    FontSize = 12,
                    HeightRequest = 34,
                    IsEnabled = user.IsActive
                };
                convert.Clicked += async (_, _) => await ChangeRoleAsync(user.EmployeeId, true);
                row.Children.Add(convert);
            }
            else if (user.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
            {
                var managerId = new Entry { Placeholder = "Manager ID to assign", Keyboard = Keyboard.Numeric, WidthRequest = 150 };
                var convert = new Button
                {
                    Text = "Convert to Employee",
                    FontSize = 12,
                    HeightRequest = 34,
                    IsEnabled = user.IsActive
                };
                convert.Clicked += async (_, _) => await ChangeRoleAsync(user.EmployeeId, false, managerId.Text);
                row.Children.Add(managerId);
                row.Children.Add(convert);
            }

            UsersContent.Add(row);
        }
    }

    private async Task ChangeStatusAsync(int userId, bool isActive)
    {
        var response = await _api.ChangeUserStatusAsync(userId, isActive);
        if (!response.Success)
        {
            StatusLabel.Text = response.Message;
            return;
        }

        var message = isActive ? "User activated." : "User deactivated.";
        StatusLabel.Text = message;
        await DisplayAlert("User Status", message, "OK");
        await LoadAsync();
    }

    private async Task ChangeRoleAsync(int userId, bool toManager, string? managerIdText = null)
    {
        if (!toManager)
        {
            if (!int.TryParse(managerIdText, out var managerId))
            {
                StatusLabel.Text = "Enter valid manager id before changing role to Employee.";
                return;
            }

            var response = await _api.ChangeUserToEmployeeAsync(userId, managerId);
            if (!response.Success)
            {
                StatusLabel.Text = response.Message;
                return;
            }

            StatusLabel.Text = "Changed to Employee.";
            await DisplayAlert("Role", "Changed to Employee.", "OK");
            await LoadAsync();
            return;
        }

        var managerResponse = await _api.ChangeUserToManagerAsync(userId);
        if (!managerResponse.Success)
        {
            StatusLabel.Text = managerResponse.Message;
            return;
        }

        StatusLabel.Text = "Changed to Manager.";
        await DisplayAlert("Role", "Changed to Manager.", "OK");
        await LoadAsync();
    }
}
