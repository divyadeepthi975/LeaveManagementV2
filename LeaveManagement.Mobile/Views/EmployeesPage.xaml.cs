using LeaveManagement.Mobile.Models;
using LeaveManagement.Mobile.Services;

namespace LeaveManagement.Mobile.Views;

public partial class EmployeesPage : ContentPage
{
    private readonly SessionService _session;
    private readonly ApiService _api;
    private string _activeSubTab = "All";
    private int? _selectedEmployeeId;
    private bool _hasLoaded;

    public EmployeesPage()
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
        StatusLabel.Text = "Loading employees...";
        EmployeesContent.Clear();

        var result = await _api.GetEmployeesAsync();
        if (!result.Success || result.Value is null)
        {
            StatusLabel.Text = result.Message;
            return;
        }

        StatusLabel.Text = "Manage employees";
        RenderSubNavigation();

        switch (_activeSubTab)
        {
            case "Create":
                RenderCreateSection();
                break;
            case "Update":
                RenderUpdateSection(result.Value);
                break;
            case "Status":
                RenderStatusSection(result.Value);
                break;
            default:
                RenderListSection(result.Value);
                break;
        }
    }

    private void RenderSubNavigation()
    {
        EmployeesContent.Add(new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            Content = new HorizontalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    CreateSubTabButton("All"),
                    CreateSubTabButton("Create"),
                    CreateSubTabButton("Update"),
                    CreateSubTabButton("Status")
                }
            }
        });
        EmployeesContent.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
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

    private void RenderListSection(List<EmployeeDto> employees)
    {
        StatusLabel.Text = $"{employees.Count} employees";
        EmployeesContent.Add(new Label { Text = "All employees", FontAttributes = FontAttributes.Bold });
        foreach (var employee in employees)
        {
            EmployeesContent.Add(new Label
            {
                Text = $"{employee.EmployeeId} - {employee.EmployeeCode} | {employee.Name} | {employee.Email} | {employee.Department} | Role:{employee.Role} | Active:{employee.IsActive} | ManagerId:{employee.ManagerId?.ToString() ?? "NA"} | Manager:{(string.IsNullOrWhiteSpace(employee.ManagerName) ? "NA" : employee.ManagerName)}"
            });
        }
    }

    private void RenderCreateSection()
    {
        EmployeesContent.Add(new Label { Text = "Create employee", FontAttributes = FontAttributes.Bold });

        var code = new Entry { Placeholder = "Employee code" };
        var name = new Entry { Placeholder = "Name" };
        var email = new Entry { Placeholder = "Email", Keyboard = Keyboard.Email };
        var password = new Entry { Placeholder = "Password", IsPassword = true };
        var department = new Entry { Placeholder = "Department" };
        var assignToMe = new CheckBox();
        var joinDate = new DatePicker { Date = DateTime.Today };
        var create = new Button { Text = "Create Employee" };

        create.Clicked += async (_, _) =>
        {
            await CreateEmployeeAsync(code.Text, name.Text, email.Text, password.Text,
                department.Text, assignToMe.IsChecked, joinDate.Date ?? DateTime.Today);
        };

        EmployeesContent.Add(code);
        EmployeesContent.Add(name);
        EmployeesContent.Add(email);
        EmployeesContent.Add(password);
        EmployeesContent.Add(department);
        EmployeesContent.Add(new HorizontalStackLayout
        {
            Spacing = 6,
            Children =
            {
                assignToMe,
                new Label { Text = "Assign me as this employee's manager", VerticalOptions = LayoutOptions.Center }
            }
        });
        EmployeesContent.Add(joinDate);
        EmployeesContent.Add(create);
    }

    private async Task CreateEmployeeAsync(string? code, string? name, string? email,
        string? password, string? department, bool assignToCurrentManager, DateTime joiningDate)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(department))
        {
            StatusLabel.Text = "Fill employee code, name, email, password and department.";
            return;
        }

        if (!assignToCurrentManager)
        {
            StatusLabel.Text = "Confirm that you will manage this employee.";
            return;
        }

        if (_session.EmployeeId <= 0)
        {
            StatusLabel.Text = "Unable to identify the signed-in manager. Sign in again.";
            return;
        }

        var response = await _api.CreateEmployeeByManagerAsync(new CreateUserRequest
        {
            EmployeeCode = code,
            Name = name,
            Email = email,
            Password = password,
            Department = department,
            JoiningDate = joiningDate,
            ManagerId = _session.EmployeeId
        });

        StatusLabel.Text = response.Success ? "Employee created successfully." : response.Message;
        if (response.Success)
        {
            await DisplayAlert("Success", "Employee created successfully.", "OK");
            await LoadAsync();
        }
    }

    private void RenderUpdateSection(List<EmployeeDto> employees)
    {
        EmployeesContent.Add(new Label { Text = "Select employee for update", FontAttributes = FontAttributes.Bold });
        _selectedEmployeeId = null;

        var codeEntry = new Entry { Placeholder = "Employee code" };
        var nameEntry = new Entry { Placeholder = "Name" };
        var emailEntry = new Entry { Placeholder = "Email", Keyboard = Keyboard.Email };
        var departmentEntry = new Entry { Placeholder = "Department" };
        var joinDate = new DatePicker { Date = DateTime.Today };

        foreach (var employee in employees)
        {
            var edit = new Button
            {
                Text = "Update",
                FontSize = 12,
                Padding = new Thickness(8, 2),
                HeightRequest = 34
            };
            edit.Clicked += (_, _) =>
            {
                _selectedEmployeeId = employee.EmployeeId;
                codeEntry.Text = employee.EmployeeCode;
                nameEntry.Text = employee.Name;
                emailEntry.Text = employee.Email;
                departmentEntry.Text = employee.Department;
                joinDate.Date = employee.JoiningDate == default ? DateTime.Today : employee.JoiningDate;
                StatusLabel.Text = $"Loaded employee {employee.EmployeeId} for update.";
            };

            EmployeesContent.Add(new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = $"{employee.EmployeeId} - {employee.Name} ({employee.Role})", VerticalOptions = LayoutOptions.Center },
                    edit
                }
            });
        }

        EmployeesContent.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        EmployeesContent.Add(new Label { Text = "Update selected employee", FontAttributes = FontAttributes.Bold });
        EmployeesContent.Add(codeEntry);
        EmployeesContent.Add(nameEntry);
        EmployeesContent.Add(emailEntry);
        EmployeesContent.Add(departmentEntry);
        EmployeesContent.Add(joinDate);

        var update = new Button { Text = "Update Employee" };
        update.Clicked += async (_, _) =>
        {
            if (_selectedEmployeeId is not int employeeId)
            {
                StatusLabel.Text = "Select an employee to update.";
                return;
            }

            await UpdateEmployeeAsync(employeeId, codeEntry.Text, nameEntry.Text,
                emailEntry.Text, departmentEntry.Text, joinDate.Date ?? DateTime.Today);
        };
        EmployeesContent.Add(update);
    }

    private async Task UpdateEmployeeAsync(int employeeId, string? code, string? name,
        string? email, string? department, DateTime joiningDate)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(department))
        {
            StatusLabel.Text = "Fill employee code, name, email and department.";
            return;
        }

        var existing = await _api.GetEmployeeByIdAsync(employeeId);
        if (!existing.Success || existing.Value is null)
        {
            StatusLabel.Text = existing.Message;
            return;
        }

        var payload = new EmployeeDto
        {
            EmployeeId = employeeId,
            EmployeeCode = code,
            Name = name,
            Email = email,
            Department = department,
            JoiningDate = joiningDate,
            IsActive = existing.Value.IsActive,
            Role = existing.Value.Role
        };

        var response = await _api.UpdateEmployeeAsync(employeeId, payload);
        StatusLabel.Text = response.Success ? "Employee updated." : response.Message;
        if (response.Success)
        {
            await LoadAsync();
        }
    }

    private void RenderStatusSection(List<EmployeeDto> employees)
    {
        EmployeesContent.Add(new Label { Text = "Activate/deactivate employees", FontAttributes = FontAttributes.Bold });
        foreach (var employee in employees)
        {
            var activate = new Button
            {
                Text = "Activate",
                FontSize = 12,
                Padding = new Thickness(8, 2),
                HeightRequest = 34,
                TextColor = Color.FromArgb("#2E7D32"),
                BorderColor = Color.FromArgb("#2E7D32")
            };
            activate.Clicked += async (_, _) => await ChangeStatusAsync(employee.EmployeeId, true);

            var deactivate = new Button
            {
                Text = "Deactivate",
                FontSize = 12,
                Padding = new Thickness(8, 2),
                HeightRequest = 34,
                TextColor = Color.FromArgb("#D9534F"),
                BorderColor = Color.FromArgb("#D9534F")
            };
            deactivate.Clicked += async (_, _) => await ChangeStatusAsync(employee.EmployeeId, false);

            EmployeesContent.Add(new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = $"{employee.EmployeeId} - {employee.Name} | Active: {employee.IsActive}", VerticalOptions = LayoutOptions.Center },
                    activate,
                    deactivate
                }
            });
        }
    }

    private async Task ChangeStatusAsync(int employeeId, bool isActive)
    {
        var response = await _api.ChangeEmployeeStatusByManagerAsync(employeeId, isActive);
        if (!response.Success)
        {
            StatusLabel.Text = response.Message;
            return;
        }

        var message = isActive ? "Employee activated." : "Employee deactivated.";
        StatusLabel.Text = message;
        await DisplayAlert("Employee Status", message, "OK");
        await LoadAsync();
    }
}
