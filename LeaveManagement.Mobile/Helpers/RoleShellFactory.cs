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
            var content = title switch
            {
                "Profile" => (Page)new ProfilePage(),
                "Leave Types" => new LeaveTypesPage(),
                "Leave Balance" => new LeaveBalancePage(),
                "Dashboard" => new DashboardPage(),
                "Apply Leave" => new ApplyLeavePage(),
                "My Leaves" => new MyLeavesPage(),
                "Approvals" => new ApprovalsPage(),
                "Employees" => new EmployeesPage(),
                "Users" => new UsersPage(),
                "Leaves" => new LeavesPage(),
                _ => new ContentPage { Title = title, Content = new Label { Text = "Screen not available." } }
            };

            tabBar.Items.Add(new ShellContent
            {
                Title = title,
                Content = content
            });
        }

        var shell = new Shell { FlyoutBehavior = FlyoutBehavior.Disabled };
        shell.Items.Add(tabBar);
        return shell;
    }
}
