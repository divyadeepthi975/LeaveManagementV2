using LeaveManagement.Mobile.Models;

namespace LeaveManagement.Mobile.Services;

public class SessionService
{
    private const string TokenKey = "auth_token";
    private const string EmployeeIdKey = "auth_employee_id";
    private const string UsernameKey = "auth_username";
    private const string RoleKey = "auth_role";

    public string? Token { get; private set; }
    public int EmployeeId { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;

    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(Token);

    public async Task LoadAsync()
    {
        Token = await GetAsync(TokenKey);
        Username = await GetAsync(UsernameKey) ?? string.Empty;
        Role = await GetAsync(RoleKey) ?? string.Empty;
        int.TryParse(await GetAsync(EmployeeIdKey), out var id);
        EmployeeId = id;
    }

    public async Task SaveAsync(LoginResponse login)
    {
        Token = login.Token;
        EmployeeId = login.EmployeeId;
        Username = login.Username;
        Role = login.Role;

        await SetAsync(TokenKey, login.Token);
        await SetAsync(EmployeeIdKey, login.EmployeeId.ToString());
        await SetAsync(UsernameKey, login.Username);
        await SetAsync(RoleKey, login.Role);
    }

    public void Clear()
    {
        Token = null;
        EmployeeId = 0;
        Username = string.Empty;
        Role = string.Empty;

        foreach (var key in new[] { TokenKey, EmployeeIdKey, UsernameKey, RoleKey })
        {
            try { SecureStorage.Default.Remove(key); } catch { }
            Preferences.Default.Remove(key);
        }
    }

    private static async Task<string?> GetAsync(string key)
    {
        try
        {
            var value = await SecureStorage.Default.GetAsync(key);
            if (value is not null) return value;
        }
        catch { }

        var pref = Preferences.Default.Get(key, string.Empty);
        return string.IsNullOrEmpty(pref) ? null : pref;
    }

    private static async Task SetAsync(string key, string value)
    {
        try
        {
            await SecureStorage.Default.SetAsync(key, value);
        }
        catch
        {
            Preferences.Default.Set(key, value);
        }
    }
}