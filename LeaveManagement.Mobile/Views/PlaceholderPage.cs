using LeaveManagement.Mobile.Helpers;
using LeaveManagement.Mobile.Services;

namespace LeaveManagement.Mobile.Views;

public class PlaceholderPage : ContentPage
{
    private sealed record InputDraft(string ControlType, object? Value, bool CanRestore);

    private static readonly Dictionary<string, List<InputDraft>> DraftsByScreen = new();
    private readonly HashSet<Element> _trackedInputControls = new();
    private readonly SessionService _session;
    private readonly ApiService _api;
    private readonly string _screen;
    private readonly Label _status;
    private readonly VerticalStackLayout _body;
    private Entry? _adminCodeEntry;
    private Entry? _adminNameEntry;
    private Entry? _adminEmailEntry;
    private Entry? _adminDepartmentEntry;
    private DatePicker? _adminJoinDatePicker;
    private Entry? _adminManagerIdEntry;
    private int? _selectedAdminUserId;
    private int? _selectedManagerEmployeeId;
    private int? _selectedLeaveTypeId;
    private Entry? _leaveTypeNameEntry;
    private Entry? _leaveTypeMaxDaysEntry;
    private string _usersSubTab = "Create";
    private string _leaveTypesSubTab = "Create";
    private string _managerEmployeesSubTab = "All";
    private bool _hasLoaded;
    private bool _isRestoringInputDraft;

    public PlaceholderPage(string title)
    {
        _session = App.Services.GetRequiredService<SessionService>();
        _api = App.Services.GetRequiredService<ApiService>();
        _screen = title;
        Title = title;

        var refresh = new Button { Text = "Refresh" };
        refresh.Clicked += async (_, _) => await LoadAsync();

        var signOut = new Button { Text = "Sign out" };
        signOut.Clicked += (_, _) =>
        {
            _session.Clear();
            App.GoToLogin();
        };

        _status = new Label();
        _body = new VerticalStackLayout { Spacing = 8 };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 18,
                Spacing = 10,
                Children =
                {
                    new Label { Text = title, FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new Label { Text = $"Signed in as {_session.Username} ({_session.Role})" },
                    new HorizontalStackLayout { Spacing = 8, Children = { refresh, signOut } },
                    _status,
                    _body
                }
            }
        };
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

    protected override void OnDisappearing()
    {
        SaveInputDraft();
        base.OnDisappearing();
    }

    private string DraftKey => $"{_session.EmployeeId}:{_screen}";

    private string DraftSection => _screen switch
    {
        "Users" => _usersSubTab,
        "Employees" when _session.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase) => _managerEmployeesSubTab,
        "Leave Types" => _leaveTypesSubTab,
        _ => "Default"
    };

    private async Task LoadAsync()
    {
        await LoadScreenContentAsync();
        TrackInputControls();
        RestoreInputDraft();
    }

    private void ClearInputDraft() => DraftsByScreen.Remove(GetDraftKey());

    private void SaveInputDraft(string? draftKey = null)
    {
        draftKey ??= GetDraftKey();
        DraftsByScreen[draftKey] = GetInputControls(Content)
            .Select(control => control switch
            {
                Entry entry => new InputDraft(entry.IsPassword ? "PasswordEntry" : nameof(Entry), entry.IsPassword ? null : entry.Text, !entry.IsPassword),
                DatePicker datePicker => new InputDraft(nameof(DatePicker), datePicker.Date, true),
                Picker picker => new InputDraft(nameof(Picker), picker.SelectedIndex, true),
                _ => new InputDraft(control.GetType().Name, null, false)
            })
            .ToList();
    }

    private void RestoreInputDraft()
    {
        if (!DraftsByScreen.TryGetValue(GetDraftKey(), out var draft)) return;

        var controls = GetInputControls(Content).ToList();
        _isRestoringInputDraft = true;
        try
        {
            for (var index = 0; index < Math.Min(controls.Count, draft.Count); index++)
            {
                if (!draft[index].CanRestore || draft[index].ControlType != GetInputControlType(controls[index])) continue;

                switch (controls[index])
                {
                    case Entry entry:
                        entry.Text = draft[index].Value as string;
                        break;
                    case DatePicker datePicker:
                        datePicker.Date = draft[index].Value is DateTime date ? date : null;
                        break;
                    case Picker picker when draft[index].Value is int selectedIndex
                        && selectedIndex >= -1
                        && selectedIndex < picker.Items.Count:
                        picker.SelectedIndex = selectedIndex;
                        break;
                }
            }
        }
        finally
        {
            _isRestoringInputDraft = false;
        }
    }

    private string GetDraftKey() => $"{DraftKey}:{DraftSection}";

    private void TrackInputControls()
    {
        foreach (var control in GetInputControls(Content))
        {
            if (!_trackedInputControls.Add(control)) continue;
            var draftKey = GetDraftKey();

            switch (control)
            {
                case Entry entry:
                    entry.TextChanged += (_, _) => { if (!_isRestoringInputDraft) SaveInputDraft(draftKey); };
                    break;
                case DatePicker datePicker:
                    datePicker.DateSelected += (_, _) => { if (!_isRestoringInputDraft) SaveInputDraft(draftKey); };
                    break;
                case Picker picker:
                    picker.SelectedIndexChanged += (_, _) => { if (!_isRestoringInputDraft) SaveInputDraft(draftKey); };
                    break;
            }
        }

    }

    private static string GetInputControlType(Element control) => control switch
    {
        Entry entry => entry.IsPassword ? "PasswordEntry" : nameof(Entry),
        DatePicker => nameof(DatePicker),
        Picker => nameof(Picker),
        _ => control.GetType().Name
    };

    private static IEnumerable<Element> GetInputControls(Element? root)
    {
        if (root is null) yield break;

        foreach (var element in GetElements(root))
        {
            if (element is Entry or DatePicker or Picker)
                yield return element;
        }
    }

    private static IEnumerable<Element> GetElements(Element element)
    {
        yield return element;

        foreach (var child in GetChildren(element))
        {
            foreach (var descendant in GetElements(child))
                yield return descendant;
        }
    }

    private static IEnumerable<Element> GetChildren(Element element)
    {
        if (element is ContentPage page && page.Content is Element pageContent)
        {
            yield return pageContent;
        }
        else if (element is ScrollView scrollView && scrollView.Content is Element scrollContent)
        {
            yield return scrollContent;
        }
        else if (element is ContentView contentView && contentView.Content is Element viewContent)
        {
            yield return viewContent;
        }
        else if (element is Border border && border.Content is Element borderContent)
        {
            yield return borderContent;
        }
        else if (element is Layout layout)
        {
            foreach (var child in layout.Children)
            {
                if (child is Element childElement)
                    yield return childElement;
            }
        }
    }

    private async Task LoadScreenContentAsync()
    {
        _status.Text = "Loading...";
        _body.Clear();

        if (_screen == "Dashboard")
        {
            var result = await _api.GetDashboardSummaryAsync();
            if (!result.Success || result.Value is null) { _status.Text = result.Message; return; }
            _status.Text = string.Empty;
            var isManager = _session.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase);
            _body.Add(new Label { Text = $"{(isManager ? "Team Employees" : "Total Employees")}: {result.Value.TotalEmployees}" });
            _body.Add(new Label { Text = $"{(isManager ? "Active Team Employees" : "Active Employees")}: {result.Value.ActiveEmployees}" });
            _body.Add(new Label { Text = $"{(isManager ? "Team Pending Requests" : "Pending Requests")}: {result.Value.PendingRequests}" });
            _body.Add(new Label { Text = $"{(isManager ? "Approved by You" : "Approved Requests")}: {result.Value.ApprovedRequests}" });
            _body.Add(new Label { Text = $"{(isManager ? "Rejected by You" : "Rejected Requests")}: {result.Value.RejectedRequests}" });
            _body.Add(new Label { Text = $"{(isManager ? "Team Approved Leave Days" : "Total Leaves Taken")}: {result.Value.TotalLeavesTaken}" });
            return;
        }

        if (_screen == "Employees")
        {
            var result = await _api.GetEmployeesAsync();
            if (!result.Success || result.Value is null) { _status.Text = result.Message; return; }

            if (_session.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
            {
                _status.Text = "Manage employees";
                RenderManagerEmployeesSubNav();

                if (_managerEmployeesSubTab == "Create")
                {
                    await RenderManagerEmployeeCreateSectionAsync();
                    return;
                }

                if (_managerEmployeesSubTab == "All")
                {
                    RenderManagerEmployeeListSection(result.Value);
                    return;
                }

                if (_managerEmployeesSubTab == "Update")
                {
                    await RenderManagerEmployeeUpdateSectionAsync(result.Value);
                    return;
                }

                if (_managerEmployeesSubTab == "Status")
                {
                    RenderManagerEmployeeStatusSection(result.Value);
                    return;
                }

                return;
            }

            _status.Text = $"{result.Value.Count} records";
            foreach (var item in result.Value)
                _body.Add(new Label { Text = $"{item.EmployeeId} - {item.Name} ({item.Role}) - {item.Department}" });

            return;
        }

        if (_screen == "Users")
        {
            if (!_session.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                _status.Text = "Users screen is available for Admin only.";
                return;
            }

            var result = await _api.GetAllUsersAsync();
            if (!result.Success || result.Value is null) { _status.Text = result.Message; return; }

            _status.Text = "Manage users";
            RenderUsersSubNav();

            if (_usersSubTab == "Create")
            {
                await RenderAdminUserActionsAsync();
                return;
            }

            if (_usersSubTab == "Employees")
            {
                RenderAdminUsersListWithStatus(result.Value);
                return;
            }

            if (_usersSubTab == "Update")
            {
                await RenderAdminUserUpdatePageAsync(result.Value);
                return;
            }

            if (_usersSubTab == "Update")
            {
                await RenderAdminUserUpdatePageAsync(result.Value);
                return;
            }

            if (_usersSubTab == "Role")
            {
                RenderAdminUserRoleSection(result.Value);
                return;
            }

            return;
        }

        if (_screen == "Leave Types")
        {
            var result = await _api.GetLeaveTypesAsync();
            if (!result.Success || result.Value is null) { _status.Text = result.Message; return; }

            if (!_session.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                _status.Text = $"There are {result.Value.Count} leave types. You can see below and can select any one of them which are Active";
                foreach (var item in result.Value)
                    _body.Add(new Label { Text = $"{item.Leavetypeid} - {item.Leavetypename} | Max: {item.Maximumdays} | Active: {item.IsActive}" });
                return;
            }

            _status.Text = "Manage leave types";
            RenderLeaveTypesSubNav();

            if (_leaveTypesSubTab == "Create")
            {
                await RenderAdminLeaveTypeCreateSectionAsync();
                return;
            }

            if (_leaveTypesSubTab == "All")
            {
                RenderLeaveTypeListSection(result.Value);
                return;
            }

            if (_leaveTypesSubTab == "Update")
            {
                await RenderAdminLeaveTypeUpdateSectionAsync(result.Value);
                return;
            }

            if (_leaveTypesSubTab == "Status")
            {
                RenderLeaveTypeStatusSection(result.Value);
                return;
            }

            if (_leaveTypesSubTab == "Update")
            {
                await RenderAdminLeaveTypeUpdateSectionAsync(result.Value);
                return;
            }

            if (_leaveTypesSubTab == "Status")
            {
                RenderLeaveTypeStatusSection(result.Value);
                return;
            }

            return;
        }

        if (_screen == "Leave Balance")
        {
            var employeeId = _session.EmployeeId;
            var result = await _api.GetLeaveBalanceAsync(employeeId);
            if (!result.Success || result.Value is null) { _status.Text = result.Message; return; }

            if (result.Value.Count == 0)
            {
                _status.Text = "Leave balance is not available for your account yet.";
                return;
            }

            _status.Text = $"Balance for employee with Employee ID - {employeeId}";
            foreach (var item in result.Value)
                _body.Add(new Label { Text = $"{item.LeaveType}: {item.RemainingDays}/{item.TotalDays} days remaining" });
            return;
        }

        if (_screen == "My Leaves")
        {
            var result = await _api.GetLeavesAsync(_session.EmployeeId);
            //if (!result.Success || result.Value is null) { _status.Text = result.Message; return; }

            if (result.Value.Count == 0)
            {
                _status.Text = "Leaves are not applied by you yet.";
                return;
            }

            _status.Text = $"You applied {result.Value.Count} leave requests";
            RenderLeaveItems(result.Value, false);
            return;
        }

        if (_screen == "Leaves")
        {
            var result = await _api.GetLeavesAsync();
            if (!result.Success || result.Value is null) { _status.Text = result.Message; return; }
            _status.Text = $"{result.Value.Count} leave requests";
            RenderLeaveItems(result.Value, false);
            return;
        }

        if (_screen == "Approvals")
        {
            var result = await _api.GetLeavesAsync();
            if (!result.Success || result.Value is null) { _status.Text = result.Message; return; }
            var pending = result.Value.Where(x => x.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase)).ToList();
            _status.Text = $"{pending.Count} pending requests";
            RenderLeaveItems(pending, true);
            return;
        }

        if (_screen == "Apply Leave")
        {
            _status.Text = "Submit a leave request";
            await RenderApplyLeaveFormAsync();
            return;
        }

        _status.Text = "Screen mapped but no content available.";
    }

    private Button CreateSubTabButton(string text, bool selected, Func<Task> onClick)
    {
        var normalColor = Colors.Gray;
        var selectedColor = Color.FromArgb("#2563EB");

        var button = new Button
        {
            Text = text,
            FontSize = 12,
            Padding = new Thickness(4, 0),
            HeightRequest = 26,
            BackgroundColor = Colors.Transparent,
            BorderWidth = 0,
            CornerRadius = 0,
            TextColor = selected ? selectedColor : normalColor
        };

        VisualStateManager.SetVisualStateGroups(button, new VisualStateGroupList
        {
            new VisualStateGroup
            {
                Name = "CommonStates",
                States =
                {
                    new VisualState
                    {
                        Name = "Normal",
                        Setters =
                        {
                            new Setter { Property = Button.TextColorProperty, Value = selected ? selectedColor : normalColor }
                        }
                    },
                    new VisualState
                    {
                        Name = "PointerOver",
                        Setters =
                        {
                            new Setter { Property = Button.TextColorProperty, Value = selectedColor }
                        }
                    },
                    new VisualState
                    {
                        Name = "Pressed",
                        Setters =
                        {
                            new Setter { Property = Button.TextColorProperty, Value = selectedColor }
                        }
                    }
                }
            }
        });

        button.Clicked += async (_, _) => await onClick();
        return button;
    }

    private void RenderUsersSubNav()
    {
        _body.Add(new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            Content = new HorizontalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    CreateSubTabButton("Create", _usersSubTab == "Create", async () => { _usersSubTab = "Create"; await LoadAsync(); }),
                    CreateSubTabButton("Get all users", _usersSubTab == "Employees", async () => { _usersSubTab = "Employees"; await LoadAsync(); }),
                    CreateSubTabButton("Update", _usersSubTab == "Update", async () => { _usersSubTab = "Update"; await LoadAsync(); }),
                    CreateSubTabButton("Role", _usersSubTab == "Role", async () => { _usersSubTab = "Role"; await LoadAsync(); })
                }
            }
        });

        _body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
    }

    private void RenderManagerEmployeesSubNav()
    {
        _body.Add(new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            Content = new HorizontalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    CreateSubTabButton("All", _managerEmployeesSubTab == "All", async () => { _managerEmployeesSubTab = "All"; await LoadAsync(); }),
                    CreateSubTabButton("Create", _managerEmployeesSubTab == "Create", async () => { _managerEmployeesSubTab = "Create"; await LoadAsync(); }),
                    CreateSubTabButton("Update", _managerEmployeesSubTab == "Update", async () => { _managerEmployeesSubTab = "Update"; await LoadAsync(); }),
                    CreateSubTabButton("Status", _managerEmployeesSubTab == "Status", async () => { _managerEmployeesSubTab = "Status"; await LoadAsync(); })
                }
            }
        });

        _body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
    }

    private void RenderLeaveTypesSubNav()
    {
        _body.Add(new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            Content = new HorizontalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    CreateSubTabButton("Create", _leaveTypesSubTab == "Create", async () => { _leaveTypesSubTab = "Create"; await LoadAsync(); }),
                    CreateSubTabButton("All", _leaveTypesSubTab == "All", async () => { _leaveTypesSubTab = "All"; await LoadAsync(); }),
                    CreateSubTabButton("Update", _leaveTypesSubTab == "Update", async () => { _leaveTypesSubTab = "Update"; await LoadAsync(); }),
                    CreateSubTabButton("Status", _leaveTypesSubTab == "Status", async () => { _leaveTypesSubTab = "Status"; await LoadAsync(); })
                }
            }
        });

        _body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
    }

    private async Task RenderAdminUserActionsAsync()
    {
        _body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        _body.Add(new Label { Text = "Create user", FontAttributes = FontAttributes.Bold });

        var code = new Entry { Placeholder = "Employee code" };
        var name = new Entry { Placeholder = "Name" };
        var email = new Entry { Placeholder = "Email" };
        var password = new Entry { Placeholder = "Password", IsPassword = true };
        var department = new Entry { Placeholder = "Department" };
        var managerId = new Entry { Placeholder = "Manager id", Keyboard = Keyboard.Numeric };
        var joinDate = new DatePicker { Date = DateTime.Today };

        var createEmployee = new Button { Text = "Create Employee", FontSize = 12, Padding = new Thickness(8, 2), HeightRequest = 30 };
        createEmployee.Clicked += async (_, _) =>
        {
            await CreateUserAsync(isManager: false, code.Text, name.Text, email.Text, password.Text, department.Text, managerId.Text, joinDate.Date ?? DateTime.Today);
        };

        var createManager = new Button { Text = "Create Manager", FontSize = 12, Padding = new Thickness(8, 2), HeightRequest = 30 };
        createManager.Clicked += async (_, _) =>
        {
            await CreateUserAsync(isManager: true, code.Text, name.Text, email.Text, password.Text, department.Text, managerId.Text, joinDate.Date ?? DateTime.Today);
        };

        _body.Add(code);
        _body.Add(name);
        _body.Add(email);
        _body.Add(password);
        _body.Add(department);
        _body.Add(managerId);
        _body.Add(joinDate);
        _body.Add(new HorizontalStackLayout { Spacing = 6, Children = { createEmployee, createManager } });
    }

    private async Task RenderAdminUserUpdatePageAsync(List<Models.EmployeeDto> users)
    {
        _body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        _body.Add(new Label { Text = "Select employee for update", FontAttributes = FontAttributes.Bold });

        _selectedAdminUserId = null;
        foreach (var user in users)
        {
            var row = new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = $"{user.EmployeeId} - {user.Name} ({user.Role})", VerticalOptions = LayoutOptions.Center },
                }
            };

            if (user.EmployeeId != _session.EmployeeId)
            {
                var edit = new Button { Text = "Update", FontSize = 12, Padding = new Thickness(8, 2), HeightRequest = 30 };
                edit.Clicked += (_, _) => PopulateAdminUserForm(user);
                row.Children.Add(edit);
            }

            _body.Add(row);
        }

        _body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        _body.Add(new Label { Text = "Update selected employee", FontAttributes = FontAttributes.Bold });

        _adminCodeEntry = new Entry { Placeholder = "Employee code" };
        _adminNameEntry = new Entry { Placeholder = "Name" };
        _adminEmailEntry = new Entry { Placeholder = "Email" };
        _adminDepartmentEntry = new Entry { Placeholder = "Department" };
        _adminManagerIdEntry = new Entry { Placeholder = "Manager id", Keyboard = Keyboard.Numeric };
        _adminJoinDatePicker = new DatePicker { Date = DateTime.Today };

        var updateUser = new Button { Text = "Update Employee", FontSize = 12, Padding = new Thickness(8, 2), HeightRequest = 30 };
        updateUser.Clicked += async (_, _) =>
        {
            if (_selectedAdminUserId is int userId)
            {
                await UpdateAdminUserAsync(userId, _adminCodeEntry.Text, _adminNameEntry.Text, _adminEmailEntry.Text, _adminDepartmentEntry.Text, _adminManagerIdEntry.Text, _adminJoinDatePicker.Date ?? DateTime.Today);
            }
            else
            {
                _status.Text = "Select an employee to update.";
            }
        };

        _body.Add(_adminCodeEntry);
        _body.Add(_adminNameEntry);
        _body.Add(_adminEmailEntry);
        _body.Add(_adminDepartmentEntry);
        _body.Add(_adminManagerIdEntry);
        _body.Add(_adminJoinDatePicker);
        _body.Add(updateUser);
    }

    private void RenderAdminUsersListWithStatus(List<Models.EmployeeDto> users)
    {
        _body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        _body.Add(new Label { Text = "All employees", FontAttributes = FontAttributes.Bold });

        foreach (var user in users)
        {
            var isActive = user.IsActive;
            var row = new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = $"{user.EmployeeId} - {user.EmployeeCode} | {user.Name} | {user.Email} | {user.Department} | Role:{user.Role} | Active:{user.IsActive} | ManagerId:{(user.ManagerId?.ToString() ?? "NA")} | Manager:{(string.IsNullOrWhiteSpace(user.ManagerName) ? "NA" : user.ManagerName)}", VerticalOptions = LayoutOptions.Center }
                }
            };

            if (user.EmployeeId != _session.EmployeeId)
            {
                var toggle = new Button
                {
                    Text = isActive ? "Deactivate" : "Activate",
                    FontSize = 12,
                    Padding = new Thickness(8, 2),
                    HeightRequest = 30,
                    TextColor = isActive ? Color.FromArgb("#D9534F") : Color.FromArgb("#2E7D32"),
                    BorderColor = isActive ? Color.FromArgb("#D9534F") : Color.FromArgb("#2E7D32")
                };

                toggle.Clicked += async (_, _) => await ChangeStatusAsync(user.EmployeeId, !isActive);
                row.Children.Add(toggle);
            }

            _body.Add(row);
        }
    }

    private void RenderAdminUserRoleSection(List<Models.EmployeeDto> users)
    {
        _body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        _body.Add(new Label { Text = "Change role", FontAttributes = FontAttributes.Bold });

        foreach (var user in users)
        {
            var row = new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = $"{user.EmployeeId} - {user.Name} | Role:{user.Role} | Active:{(user.IsActive ? "Yes" : "No")} | ManagerId:{(user.ManagerId?.ToString() ?? "NA")} | Manager:{(string.IsNullOrWhiteSpace(user.ManagerName) ? "NA" : user.ManagerName)}", VerticalOptions = LayoutOptions.Center }
                }
            };

            if (user.Role.Equals("Employee", StringComparison.OrdinalIgnoreCase))
            {
                var convertToManager = new Button { Text = "Convert to Manager", FontSize = 12, Padding = new Thickness(8, 2), HeightRequest = 30, IsEnabled = user.IsActive };
                convertToManager.Clicked += async (_, _) => await ChangeUserRoleAsync(user.EmployeeId, toManager: true);
                row.Children.Add(convertToManager);
            }
            else if (user.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
            {
                var managerIdEntry = new Entry
                {
                    Placeholder = "Manager ID to assign",
                    Keyboard = Keyboard.Numeric,
                    WidthRequest = 150
                };
                var convertToEmployee = new Button { Text = "Convert to Employee", FontSize = 12, Padding = new Thickness(8, 2), HeightRequest = 30, IsEnabled = user.IsActive };
                convertToEmployee.Clicked += async (_, _) => await ChangeUserRoleAsync(user.EmployeeId, toManager: false, managerIdEntry.Text);
                row.Children.Add(managerIdEntry);
                row.Children.Add(convertToEmployee);
            }

            _body.Add(row);
        }
    }

    private void PopulateAdminUserForm(Models.EmployeeDto user)
    {
        if (_adminCodeEntry is null ||
            _adminNameEntry is null ||
            _adminEmailEntry is null ||
            _adminDepartmentEntry is null ||
            _adminJoinDatePicker is null)
        {
            return;
        }

        _selectedAdminUserId = user.EmployeeId;
        _adminCodeEntry.Text = user.EmployeeCode;
        _adminNameEntry.Text = user.Name;
        _adminEmailEntry.Text = user.Email;
        _adminDepartmentEntry.Text = user.Department;
        if (_adminManagerIdEntry is not null)
        {
            _adminManagerIdEntry.Text = user.ManagerId?.ToString() ?? string.Empty;
        }
        _adminJoinDatePicker.Date = user.JoiningDate == default ? DateTime.Today : user.JoiningDate;
        _status.Text = $"Loaded employee {user.EmployeeId} for update.";
    }

    private async Task CreateUserAsync(bool isManager, string? code, string? name, string? email, string? password, string? department, string? managerIdText, DateTime joiningDate)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(department))
        {
            _status.Text = "Fill all user fields.";
            return;
        }

        if (string.IsNullOrWhiteSpace(managerIdText))
        {
            _status.Text = "Manager id is required.";
            return;
        }

        if (!int.TryParse(managerIdText, out var parsedManagerId))
        {
            _status.Text = "Manager id must be a valid number.";
            return;
        }

        var payload = new Models.CreateUserRequest
        {
            EmployeeCode = code,
            Name = name,
            Email = email,
            Password = password,
            Department = department,
            JoiningDate = joiningDate,
            ManagerId = parsedManagerId
        };

        var response = isManager
            ? await _api.CreateManagerAsync(payload)
            : await _api.CreateAdminEmployeeAsync(payload);

        _status.Text = response.Success
            ? (isManager ? "Manager created." : "Employee created.")
            : response.Message;
        if (response.Success) ClearInputDraft();
    }

    private async Task ChangeStatusAsync(string? userIdText, bool isActive)
    {
        if (!int.TryParse(userIdText, out var userId))
        {
            _status.Text = "Invalid user id.";
            return;
        }

        await ChangeStatusAsync(userId, isActive);
    }

    private async Task ChangeStatusAsync(int userId, bool isActive)
    {
        var response = await _api.ChangeUserStatusAsync(userId, isActive);
        if (response.Success)
        {
            var message = isActive ? "User activated." : "User deactivated.";
            _status.Text = message;
            await DisplayAlert("User Status", message, "OK");
            await LoadAsync();
            return;
        }

        _status.Text = response.Message;
    }

    private async Task ChangeStatusByManagerAsync(int employeeId, bool isActive)
    {
        var response = await _api.ChangeEmployeeStatusByManagerAsync(employeeId, isActive);
        if (response.Success)
        {
            var message = isActive ? "Employee activated." : "Employee deactivated.";
            _status.Text = message;
            await DisplayAlert("Employee Status", message, "OK");
            await LoadAsync();
            return;
        }

        _status.Text = response.Message;
    }

    private async Task ChangeUserRoleAsync(int userId, bool toManager, string? managerIdText = null)
    {
        if (!toManager)
        {
            if (string.IsNullOrWhiteSpace(managerIdText) || !int.TryParse(managerIdText, out var managerId))
            {
                _status.Text = "Enter valid manager id before changing role to Employee.";
                return;
            }

            var toEmployeeResponse = await _api.ChangeUserToEmployeeAsync(userId, managerId);
            if (!toEmployeeResponse.Success)
            {
                _status.Text = toEmployeeResponse.Message;
                return;
            }

            _status.Text = "Changed to Employee.";
            await DisplayAlert("Role", "Changed to Employee.", "OK");
            await LoadAsync();
            return;
        }

        var response = await _api.ChangeUserToManagerAsync(userId);

        if (!response.Success)
        {
            _status.Text = response.Message;
            return;
        }

        var message = toManager ? "Changed to Manager." : "Changed to Employee.";
        _status.Text = message;
        await DisplayAlert("Role", message, "OK");
        await LoadAsync();
    }

    private async Task UpdateAdminUserAsync(int userId, string? code, string? name, string? email, string? department, string? managerIdText, DateTime joiningDate)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(department))
        {
            _status.Text = "Fill employee code, name, email and department to update employee.";
            return;
        }

        if (string.IsNullOrWhiteSpace(managerIdText) || !int.TryParse(managerIdText, out var managerId))
        {
            _status.Text = "Enter a valid manager id to update employee.";
            return;
        }

        var existing = await _api.GetAdminEmployeeByIdAsync(userId);
        if (!existing.Success || existing.Value is null)
        {
            _status.Text = existing.Message;
            return;
        }

        var payload = new Models.EmployeeDto
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
        _status.Text = response.Success ? "Employee updated." : response.Message;
        if (response.Success)
        {
            ClearInputDraft();
            await LoadAsync();
        }
    }

    private async Task RenderManagerEmployeeCreateSectionAsync()
    {
        _body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        _body.Add(new Label { Text = "Create employee", FontAttributes = FontAttributes.Bold });

        var code = new Entry { Placeholder = "Employee code" };
        var name = new Entry { Placeholder = "Name" };
        var email = new Entry { Placeholder = "Email" };
        var password = new Entry { Placeholder = "Password", IsPassword = true };
        var department = new Entry { Placeholder = "Department" };
        var assignToMe = new CheckBox();
        var managerAssignment = new HorizontalStackLayout
        {
            Spacing = 6,
            Children =
            {
                assignToMe,
                new Label { Text = "Assign me as this employee's manager", VerticalOptions = LayoutOptions.Center }
            }
        };
        var joinDate = new DatePicker { Date = DateTime.Today };

        var create = new Button { Text = "Create Employee", FontSize = 12, Padding = new Thickness(8, 2), HeightRequest = 30 };
        create.Clicked += async (_, _) =>
        {
            await CreateManagerEmployeeAsync(code.Text, name.Text, email.Text, password.Text, department.Text, assignToMe.IsChecked, joinDate.Date ?? DateTime.Today);
        };

        _body.Add(code);
        _body.Add(name);
        _body.Add(email);
        _body.Add(password);
        _body.Add(department);
        _body.Add(managerAssignment);
        _body.Add(joinDate);
        _body.Add(create);
    }

    private void RenderManagerEmployeeListSection(List<Models.EmployeeDto> employees)
    {
        _status.Text = $"{employees.Count} employees";
        _body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        _body.Add(new Label { Text = "All employees", FontAttributes = FontAttributes.Bold });

        foreach (var item in employees)
        {
            _body.Add(new Label
            {
                Text = $"{item.EmployeeId} -{item.EmployeeCode} | {item.Name} | {item.Email} | {item.Department} | Role:{item.Role} | Active:{item.IsActive} | ManagerId:{(item.ManagerId?.ToString() ?? "NA")} | Manager:{(string.IsNullOrWhiteSpace(item.ManagerName) ? "NA" : item.ManagerName)}"
            });
        }
    }

    private async Task RenderManagerEmployeeUpdateSectionAsync(List<Models.EmployeeDto> employees)
    {
        _body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        _body.Add(new Label { Text = "Select employee for update", FontAttributes = FontAttributes.Bold });

        _selectedManagerEmployeeId = null;
        var codeEntry = new Entry { Placeholder = "Employee code" };
        var nameEntry = new Entry { Placeholder = "Name" };
        var emailEntry = new Entry { Placeholder = "Email" };
        var departmentEntry = new Entry { Placeholder = "Department" };
        var joinDate = new DatePicker { Date = DateTime.Today };

        foreach (var employee in employees)
        {
            var edit = new Button { Text = "Update", FontSize = 12, Padding = new Thickness(8, 2), HeightRequest = 30 };
            edit.Clicked += (_, _) =>
            {
                _selectedManagerEmployeeId = employee.EmployeeId;
                codeEntry.Text = employee.EmployeeCode;
                nameEntry.Text = employee.Name;
                emailEntry.Text = employee.Email;
                departmentEntry.Text = employee.Department;
                joinDate.Date = employee.JoiningDate == default ? DateTime.Today : employee.JoiningDate;
                _status.Text = $"Loaded employee {employee.EmployeeId} for update.";
            };

            _body.Add(new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = $"{employee.EmployeeId} - {employee.Name} ({employee.Role})", VerticalOptions = LayoutOptions.Center },
                    edit
                }
            });
        }

        _body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        _body.Add(new Label { Text = "Update selected employee", FontAttributes = FontAttributes.Bold });

        var update = new Button { Text = "Update Employee", FontSize = 12, Padding = new Thickness(8, 2), HeightRequest = 30 };
        update.Clicked += async (_, _) =>
        {
            if (_selectedManagerEmployeeId is int employeeId)
            {
                await UpdateManagerEmployeeAsync(employeeId, codeEntry.Text, nameEntry.Text, emailEntry.Text, departmentEntry.Text, joinDate.Date ?? DateTime.Today);
            }
            else
            {
                _status.Text = "Select an employee to update.";
            }
        };

        _body.Add(codeEntry);
        _body.Add(nameEntry);
        _body.Add(emailEntry);
        _body.Add(departmentEntry);
        _body.Add(joinDate);
        _body.Add(update);
    }

    private void RenderManagerEmployeeStatusSection(List<Models.EmployeeDto> employees)
    {
        _body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        _body.Add(new Label { Text = "Activate/deactivate employees", FontAttributes = FontAttributes.Bold });

        foreach (var employee in employees)
        {
            var activate = new Button
            {
                Text = "Activate",
                FontSize = 12,
                Padding = new Thickness(8, 2),
                HeightRequest = 30,
                TextColor = Color.FromArgb("#2E7D32"),
                BorderColor = Color.FromArgb("#2E7D32")
            };
            activate.Clicked += async (_, _) => await ChangeStatusByManagerAsync(employee.EmployeeId, true);

            var deactivate = new Button
            {
                Text = "Deactivate",
                FontSize = 12,
                Padding = new Thickness(8, 2),
                HeightRequest = 30,
                TextColor = Color.FromArgb("#D9534F"),
                BorderColor = Color.FromArgb("#D9534F")
            };
            deactivate.Clicked += async (_, _) => await ChangeStatusByManagerAsync(employee.EmployeeId, false);

            _body.Add(new HorizontalStackLayout
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

    private async Task CreateManagerEmployeeAsync(string? code, string? name, string? email, string? password, string? department, bool assignToCurrentManager, DateTime joiningDate)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(department))
        {
            _status.Text = "Fill employee code, name, email, password and department.";
            return;
        }

        if (!assignToCurrentManager)
        {
            _status.Text = "Confirm that you will manage this employee.";
            return;
        }

        if (_session.EmployeeId <= 0)
        {
            _status.Text = "Unable to identify the signed-in manager. Sign in again.";
            return;
        }

        var response = await _api.CreateEmployeeByManagerAsync(new Models.CreateUserRequest
        {
            EmployeeCode = code,
            Name = name,
            Email = email,
            Password = password,
            Department = department,
            JoiningDate = joiningDate,
            ManagerId = _session.EmployeeId
        });

        _status.Text = response.Success ? "Employee created." : response.Message;
        if (response.Success)
        {
            ClearInputDraft();
            await LoadAsync();
        }
    }

    private async Task UpdateManagerEmployeeAsync(int employeeId, string? code, string? name, string? email, string? department, DateTime joiningDate)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(department))
        {
            _status.Text = "Fill employee code, name, email and department to update employee.";
            return;
        }

        var existing = await _api.GetEmployeeByIdAsync(employeeId);
        if (!existing.Success || existing.Value is null)
        {
            _status.Text = existing.Message;
            return;
        }

        var payload = new Models.EmployeeDto
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
        _status.Text = response.Success ? "Employee updated." : response.Message;
        if (response.Success)
        {
            ClearInputDraft();
            await LoadAsync();
        }
    }

    private async Task RenderAdminLeaveTypeCreateSectionAsync()
    {
        _body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        _body.Add(new Label { Text = "Create leave type", FontAttributes = FontAttributes.Bold });

        _leaveTypeNameEntry = new Entry { Placeholder = "Leave type name" };
        _leaveTypeMaxDaysEntry = new Entry { Placeholder = "Maximum days", Keyboard = Keyboard.Numeric };

        var create = new Button { Text = "Create", FontSize = 12, Padding = new Thickness(8, 2), HeightRequest = 30 };
        create.Clicked += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_leaveTypeNameEntry.Text) || !int.TryParse(_leaveTypeMaxDaysEntry.Text, out var maximumDays))
            {
                _status.Text = "Enter valid leave type name and maximum days.";
                return;
            }

            var response = await _api.CreateLeaveTypeAsync(new Models.CreateLeaveTypeRequest
            {
                Leavetypename = _leaveTypeNameEntry.Text,
                Maximumdays = maximumDays
            });

            _status.Text = response.Success ? "Leave type created." : response.Message;
            if (response.Success)
            {
                ClearInputDraft();
                await LoadAsync();
            }
        };

        _body.Add(_leaveTypeNameEntry);
        _body.Add(_leaveTypeMaxDaysEntry);
        _body.Add(create);
    }

    private void RenderLeaveTypeListSection(List<Models.LeaveTypeDto> leaveTypes)
    {
        _body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        _body.Add(new Label { Text = "All leave types", FontAttributes = FontAttributes.Bold });

        foreach (var item in leaveTypes)
        {
            _body.Add(new Label { Text = $"{item.Leavetypeid} - {item.Leavetypename} | Max: {item.Maximumdays} | Active: {item.IsActive}" });
        }
    }

    private async Task RenderAdminLeaveTypeUpdateSectionAsync(List<Models.LeaveTypeDto> leaveTypes)
    {
        _body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        _body.Add(new Label { Text = "Select leave type for update", FontAttributes = FontAttributes.Bold });

        foreach (var item in leaveTypes)
        {
            var edit = new Button { Text = "Update", FontSize = 12, Padding = new Thickness(8, 2), HeightRequest = 30 };
            edit.Clicked += (_, _) => PopulateLeaveTypeForm(item);
            _body.Add(new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = $"{item.Leavetypeid} - {item.Leavetypename}", VerticalOptions = LayoutOptions.Center },
                    edit
                }
            });
        }

        _body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        _body.Add(new Label { Text = "Update selected leave type", FontAttributes = FontAttributes.Bold });

        _selectedLeaveTypeId = null;
        _leaveTypeNameEntry = new Entry { Placeholder = "Leave type name" };
        _leaveTypeMaxDaysEntry = new Entry { Placeholder = "Maximum days", Keyboard = Keyboard.Numeric };

        var update = new Button { Text = "Update", FontSize = 12, Padding = new Thickness(8, 2), HeightRequest = 30 };
        update.Clicked += async (_, _) =>
        {
            if (_selectedLeaveTypeId is not int id ||
                string.IsNullOrWhiteSpace(_leaveTypeNameEntry.Text) ||
                !int.TryParse(_leaveTypeMaxDaysEntry.Text, out var maximumDays))
            {
                _status.Text = "Select a leave type and enter a valid name and maximum days.";
                return;
            }

            var existing = leaveTypes.FirstOrDefault(x => x.Leavetypeid == id);

            var response = await _api.UpdateLeaveTypeAsync(new Models.UpdateLeaveTypeRequest
            {
                Leavetypeid = id,
                Leavetypename = _leaveTypeNameEntry.Text,
                Maximumdays = maximumDays,
                IsActive = existing?.IsActive ?? true
            });

            _status.Text = response.Success ? "Leave type updated." : response.Message;
            if (response.Success)
            {
                ClearInputDraft();
                await LoadAsync();
            }
        };

        _body.Add(_leaveTypeNameEntry);
        _body.Add(_leaveTypeMaxDaysEntry);
        _body.Add(update);
    }

    private void RenderLeaveTypeStatusSection(List<Models.LeaveTypeDto> leaveTypes)
    {
        _body.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        _body.Add(new Label { Text = "Activate/deactivate leave types", FontAttributes = FontAttributes.Bold });

        foreach (var item in leaveTypes)
        {
            var activate = new Button
            {
                Text = "Activate",
                FontSize = 12,
                Padding = new Thickness(8, 2),
                HeightRequest = 30,
                TextColor = Color.FromArgb("#2E7D32"),
                BorderColor = Color.FromArgb("#2E7D32")
            };
            activate.Clicked += async (_, _) => await ChangeLeaveTypeStatusAsync(item.Leavetypeid, true);

            var deactivate = new Button
            {
                Text = "Deactivate",
                FontSize = 12,
                Padding = new Thickness(8, 2),
                HeightRequest = 30,
                TextColor = Color.FromArgb("#D9534F"),
                BorderColor = Color.FromArgb("#D9534F")
            };
            deactivate.Clicked += async (_, _) => await ChangeLeaveTypeStatusAsync(item.Leavetypeid, false);

            _body.Add(new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = $"{item.Leavetypeid} - {item.Leavetypename} | Active:{item.IsActive}", VerticalOptions = LayoutOptions.Center },
                    activate,
                    deactivate
                }
            });
        }
    }

    private void PopulateLeaveTypeForm(Models.LeaveTypeDto leaveType)
    {
        if (_leaveTypeNameEntry is null || _leaveTypeMaxDaysEntry is null)
        {
            return;
        }

        _selectedLeaveTypeId = leaveType.Leavetypeid;
        _leaveTypeNameEntry.Text = leaveType.Leavetypename;
        _leaveTypeMaxDaysEntry.Text = leaveType.Maximumdays.ToString();
        _status.Text = $"Loaded leave type {leaveType.Leavetypeid} for update.";
    }

    private async Task ChangeLeaveTypeStatusAsync(int leaveTypeId, bool isActive)
    {
        var response = isActive
            ? await _api.ActivateLeaveTypeAsync(leaveTypeId)
            : await _api.DeactivateLeaveTypeAsync(leaveTypeId);

        if (!response.Success)
        {
            _status.Text = response.Message;
            return;
        }

        var message = isActive ? "Leave type activated." : "Leave type deactivated.";
        _status.Text = message;
        await DisplayAlert("Leave Type Status", message, "OK");
        await LoadAsync();
    }

    private async Task RenderApplyLeaveFormAsync()
    {
        var leaveTypesResult = await _api.GetLeaveTypesAsync();
        if (!leaveTypesResult.Success || leaveTypesResult.Value is null)
        {
            _status.Text = leaveTypesResult.Message;
            return;
        }

        var activeLeaveTypes = leaveTypesResult.Value
            .Where(x => x.IsActive)
            .ToList();

        if (activeLeaveTypes.Count == 0)
        {
            _status.Text = "No active leave types are available right now.";
            return;
        }

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
                _status.Text = "Please select a leave type.";
                return;
            }

            var selectedLeaveTypeId = activeLeaveTypes[leaveTypePicker.SelectedIndex].Leavetypeid;

            var response = await _api.ApplyLeaveAsync(new Models.ApplyLeaveRequest
            {
                Employeeid = _session.EmployeeId,
                Leavetypeid = selectedLeaveTypeId,
                Fromdate = fromPicker.Date ?? DateTime.Today,
                Todate = toPicker.Date ?? DateTime.Today,
                Reason = reasonEntry.Text ?? string.Empty
            });

            _status.Text = response.Success ? "Leave request submitted." : response.Message;
            if (response.Success) ClearInputDraft();
        };

        _body.Add(new Label { Text = "Leave Type" });
        _body.Add(leaveTypePicker);
        _body.Add(new Label { Text = "From date" });
        _body.Add(fromPicker);
        _body.Add(new Label { Text = "To date" });
        _body.Add(toPicker);
        _body.Add(new Label { Text = "Reason" });
        _body.Add(reasonEntry);
        _body.Add(submit);
    }

    private void RenderLeaveItems(List<Models.LeaveRequestDto> items, bool allowApproval)
    {
        foreach (var item in items)
        {
            var managerMessage = string.Empty;
            if (!item.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                var byText = string.IsNullOrWhiteSpace(item.Approvedby) ? "Manager" : item.Approvedby;
                var commentsText = string.IsNullOrWhiteSpace(item.Comments) ? "No reason provided." : item.Comments;
                managerMessage = $" | {byText}: {commentsText}";
            }

            _body.Add(new Label
            {
                Text = $"#{item.Leaverequestid} Emp:{item.Employeeid} Type:{item.Leavetypeid} {item.Fromdate:yyyy-MM-dd} to {item.Todate:yyyy-MM-dd} | {item.Status} | Reason: {item.Reason}{managerMessage}"
            });

            if (!allowApproval)
            {
                continue;
            }

            var approve = new Button { Text = $"Approve #{item.Leaverequestid}" };
            approve.Clicked += async (_, _) => await ApproveOrRejectAsync(item.Leaverequestid, true);

            var reject = new Button { Text = $"Reject #{item.Leaverequestid}" };
            reject.Clicked += async (_, _) => await ApproveOrRejectAsync(item.Leaverequestid, false);

            _body.Add(new HorizontalStackLayout { Spacing = 8, Children = { approve, reject } });
        }
    }

    private async Task ApproveOrRejectAsync(int requestId, bool approve)
    {
        var comments = await DisplayPromptAsync(
            approve ? "Approve leave" : "Reject leave",
            "Comments (optional)");

        var action = new Models.LeaveActionRequest
        {
            Id = requestId,
            Approvedby = _session.Username,
            Comments = comments
        };

        var response = approve
            ? await _api.ApproveLeaveAsync(action)
            : await _api.RejectLeaveAsync(action);

        _status.Text = response.Success
            ? (approve ? "Leave approved." : "Leave rejected.")
            : response.Message;

        await LoadAsync();
    }
}
