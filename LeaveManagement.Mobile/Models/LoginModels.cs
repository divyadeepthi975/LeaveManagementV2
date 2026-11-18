namespace LeaveManagement.Mobile.Models;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// Matches the API response:
// { "token": "...", "employeeid": 1, "username": "...", "role": "Admin" }
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public record LoginResult(bool Success, string? Error = null);