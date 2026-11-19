using LeaveManagement.Mobile.Models;
using LeaveManagement.Mobile.Services;

namespace LeaveManagement.Mobile.Views;

public partial class LeaveTypesPage : ContentPage
{
    private readonly SessionService _session;
    private readonly ApiService _api;
    private string _activeSubTab = "Create";
    private int? _selectedLeaveTypeId;
    private Entry? _leaveTypeNameEntry;
    private Entry? _leaveTypeMaxDaysEntry;
    private bool _hasLoaded;

    public LeaveTypesPage()
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
        StatusLabel.Text = "Loading leave types...";
        LeaveTypesContent.Clear();

        var result = await _api.GetLeaveTypesAsync();
        if (!result.Success || result.Value is null)
        {
            StatusLabel.Text = result.Message;
            return;
        }

        if (!_session.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            StatusLabel.Text =
                $"There are {result.Value.Count} leave types. Active leave types can be selected when applying.";
            RenderReadOnlyList(result.Value);
            return;
        }

        StatusLabel.Text = "Manage leave types";
        RenderSubNavigation();

        switch (_activeSubTab)
        {
            case "All":
                RenderList(result.Value);
                break;
            case "Update":
                RenderUpdateSection(result.Value);
                break;
            case "Status":
                RenderStatusSection(result.Value);
                break;
            default:
                RenderCreateSection();
                break;
        }
    }

    private void RenderSubNavigation()
    {
        LeaveTypesContent.Add(new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            Content = new HorizontalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    CreateSubTabButton("Create"),
                    CreateSubTabButton("All"),
                    CreateSubTabButton("Update"),
                    CreateSubTabButton("Status")
                }
            }
        });
        LeaveTypesContent.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
    }

    private Button CreateSubTabButton(string title)
    {
        var isActive = _activeSubTab == title;
        var button = new Button
        {
            Text = title,
            FontSize = 12,
            Padding = new Thickness(8, 2),
            HeightRequest = 34,
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#7254D8"),
            BorderColor = Color.FromArgb("#7254D8")
        };
        button.Clicked += async (_, _) =>
        {
            _activeSubTab = title;
            await LoadAsync();
        };
        return button;
    }

    private void RenderReadOnlyList(List<LeaveTypeDto> leaveTypes)
    {
        LeaveTypesContent.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        foreach (var item in leaveTypes)
        {
            LeaveTypesContent.Add(new Label
            {
                Text = $"{item.Leavetypeid} - {item.Leavetypename} | Max: {item.Maximumdays} | Active: {item.IsActive}"
            });
        }
    }

    private void RenderCreateSection()
    {
        LeaveTypesContent.Add(new Label { Text = "Create leave type", FontAttributes = FontAttributes.Bold });

        _leaveTypeNameEntry = new Entry { Placeholder = "Leave type name", AutomationId = "LeaveTypeNameEntry" };
        _leaveTypeMaxDaysEntry = new Entry
        {
            Placeholder = "Maximum days",
            Keyboard = Keyboard.Numeric,
            AutomationId = "LeaveTypeMaxDaysEntry"
        };

        var create = new Button { Text = "Create", AutomationId = "CreateLeaveTypeButton" };
        create.Clicked += async (_, _) => await CreateLeaveTypeAsync();

        LeaveTypesContent.Add(_leaveTypeNameEntry);
        LeaveTypesContent.Add(_leaveTypeMaxDaysEntry);
        LeaveTypesContent.Add(create);
    }

    private async Task CreateLeaveTypeAsync()
    {
        if (string.IsNullOrWhiteSpace(_leaveTypeNameEntry?.Text) ||
            !int.TryParse(_leaveTypeMaxDaysEntry?.Text, out var maximumDays) ||
            maximumDays <= 0)
        {
            StatusLabel.Text = "Enter a valid leave type name and maximum days.";
            return;
        }

        var response = await _api.CreateLeaveTypeAsync(new CreateLeaveTypeRequest
        {
            Leavetypename = _leaveTypeNameEntry.Text,
            Maximumdays = maximumDays
        });

        StatusLabel.Text = response.Success ? "Leave type created." : response.Message;
        if (response.Success)
        {
            await LoadAsync();
        }
    }

    private void RenderList(List<LeaveTypeDto> leaveTypes)
    {
        LeaveTypesContent.Add(new Label { Text = "All leave types", FontAttributes = FontAttributes.Bold });
        foreach (var item in leaveTypes)
        {
            LeaveTypesContent.Add(new Label
            {
                Text = $"{item.Leavetypeid} - {item.Leavetypename} | Max: {item.Maximumdays} | Active: {item.IsActive}"
            });
        }
    }

    private void RenderUpdateSection(List<LeaveTypeDto> leaveTypes)
    {
        LeaveTypesContent.Add(new Label { Text = "Select leave type for update", FontAttributes = FontAttributes.Bold });

        foreach (var item in leaveTypes)
        {
            var edit = new Button
            {
                Text = "Update",
                FontSize = 12,
                Padding = new Thickness(8, 2),
                HeightRequest = 34,
                AutomationId = $"SelectLeaveTypeUpdate{item.Leavetypeid}"
            };
            edit.Clicked += (_, _) => PopulateLeaveTypeForm(item);
            LeaveTypesContent.Add(new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = $"{item.Leavetypeid} - {item.Leavetypename}", VerticalOptions = LayoutOptions.Center },
                    edit
                }
            });
        }

        LeaveTypesContent.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
        LeaveTypesContent.Add(new Label { Text = "Update selected leave type", FontAttributes = FontAttributes.Bold });

        _selectedLeaveTypeId = null;
        _leaveTypeNameEntry = new Entry { Placeholder = "Leave type name", AutomationId = "LeaveTypeNameEntry" };
        _leaveTypeMaxDaysEntry = new Entry
        {
            Placeholder = "Maximum days",
            Keyboard = Keyboard.Numeric,
            AutomationId = "LeaveTypeMaxDaysEntry"
        };

        var update = new Button { Text = "Update", AutomationId = "UpdateLeaveTypeButton" };
        update.Clicked += async (_, _) => await UpdateLeaveTypeAsync(leaveTypes);

        LeaveTypesContent.Add(_leaveTypeNameEntry);
        LeaveTypesContent.Add(_leaveTypeMaxDaysEntry);
        LeaveTypesContent.Add(update);
    }

    private async Task UpdateLeaveTypeAsync(List<LeaveTypeDto> leaveTypes)
    {
        if (_selectedLeaveTypeId is not int id ||
            string.IsNullOrWhiteSpace(_leaveTypeNameEntry?.Text) ||
            !int.TryParse(_leaveTypeMaxDaysEntry?.Text, out var maximumDays) ||
            maximumDays <= 0)
        {
            StatusLabel.Text = "Select a leave type and enter a valid name and maximum days.";
            return;
        }

        var existing = leaveTypes.FirstOrDefault(x => x.Leavetypeid == id);
        var response = await _api.UpdateLeaveTypeAsync(new UpdateLeaveTypeRequest
        {
            Leavetypeid = id,
            Leavetypename = _leaveTypeNameEntry.Text,
            Maximumdays = maximumDays,
            IsActive = existing?.IsActive ?? true
        });

        StatusLabel.Text = response.Success ? "Leave type updated." : response.Message;
        if (response.Success)
        {
            await LoadAsync();
        }
    }

    private void RenderStatusSection(List<LeaveTypeDto> leaveTypes)
    {
        LeaveTypesContent.Add(new Label { Text = "Activate/deactivate leave types", FontAttributes = FontAttributes.Bold });

        foreach (var item in leaveTypes)
        {
            var activate = new Button
            {
                Text = "Activate",
                FontSize = 12,
                Padding = new Thickness(8, 2),
                HeightRequest = 34,
                TextColor = Color.FromArgb("#2E7D32"),
                BorderColor = Color.FromArgb("#2E7D32"),
                AutomationId = $"ActivateLeaveType{item.Leavetypeid}"
            };
            activate.Clicked += async (_, _) => await ChangeStatusAsync(item.Leavetypeid, true);

            var deactivate = new Button
            {
                Text = "Deactivate",
                FontSize = 12,
                Padding = new Thickness(8, 2),
                HeightRequest = 34,
                TextColor = Color.FromArgb("#D9534F"),
                BorderColor = Color.FromArgb("#D9534F"),
                AutomationId = $"DeactivateLeaveType{item.Leavetypeid}"
            };
            deactivate.Clicked += async (_, _) => await ChangeStatusAsync(item.Leavetypeid, false);

            LeaveTypesContent.Add(new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label
                    {
                        Text = $"{item.Leavetypeid} - {item.Leavetypename} | Active:{item.IsActive}",
                        VerticalOptions = LayoutOptions.Center
                    },
                    activate,
                    deactivate
                }
            });
        }
    }

    private void PopulateLeaveTypeForm(LeaveTypeDto leaveType)
    {
        if (_leaveTypeNameEntry is null || _leaveTypeMaxDaysEntry is null)
        {
            return;
        }

        _selectedLeaveTypeId = leaveType.Leavetypeid;
        _leaveTypeNameEntry.Text = leaveType.Leavetypename;
        _leaveTypeMaxDaysEntry.Text = leaveType.Maximumdays.ToString();
        StatusLabel.Text = $"Loaded leave type {leaveType.Leavetypeid} for update.";
    }

    private async Task ChangeStatusAsync(int leaveTypeId, bool isActive)
    {
        var response = isActive
            ? await _api.ActivateLeaveTypeAsync(leaveTypeId)
            : await _api.DeactivateLeaveTypeAsync(leaveTypeId);

        if (!response.Success)
        {
            StatusLabel.Text = response.Message;
            return;
        }

        var message = isActive ? "Leave type activated." : "Leave type deactivated.";
        StatusLabel.Text = message;
        await DisplayAlert("Leave Type Status", message, "OK");
        await LoadAsync();
    }
}
