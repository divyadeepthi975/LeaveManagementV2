using LeaveManagement.Mobile.Views;

namespace LeaveManagement.Mobile.Helpers;

public static class RoleShellFactory
{
    public static Shell Create(string role)
    {
        var tabs = role.ToLowerInvariant() switch
        {
            "admin" => new[] { "Dashboard", "Profile", "Users", "Leave Types" },
            "manager" => new[] { "Dashboard", "Profile", "Employees", "Approvals", "Leave Types" },
            _ => new[] { "Profile", "Leave Balance", "Apply Leave", "My Leaves", "Leave Types" }
        };

        var tabBar = new TabBar();
        foreach (var title in tabs)
        {
            tabBar.Items.Add(new ShellContent
            {
                Title = title,
                Content = title == "Profile"
                    ? new ProfilePage()
                    : new PlaceholderPage(title)
            });
        }

        var shell = new Shell { FlyoutBehavior = FlyoutBehavior.Disabled };
        shell.Items.Add(tabBar);
        return shell;
    }
}
