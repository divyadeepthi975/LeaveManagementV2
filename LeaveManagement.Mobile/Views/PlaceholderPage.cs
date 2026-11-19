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

        if (_screen == "Leaves")
        {
            var result = await _api.GetLeavesAsync();
            if (!result.Success || result.Value is null) { _status.Text = result.Message; return; }
            _status.Text = $"{result.Value.Count} leave requests";
            RenderLeaveItems(result.Value, false);
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
