using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeaveManagement.Mobile.Helpers;
using LeaveManagement.Mobile.Models;

namespace LeaveManagement.Mobile.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly SessionService _session;

    public AuthService(HttpClient http, SessionService session)
    {
        _http = http;
        _session = session;
    }

    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(
                Constants.LoginPath,
                new LoginRequest { Username = username, Password = password });

            if (response.IsSuccessStatusCode)
            {
                var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (login is null || string.IsNullOrWhiteSpace(login.Token))
                    return new LoginResult(false, "The server returned an unexpected response.");

                await _session.SaveAsync(login);
                return new LoginResult(true);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.BadRequest => new LoginResult(false, "Enter a valid username and password."),
                HttpStatusCode.Unauthorized => new LoginResult(false, "Username or password is incorrect."),
                HttpStatusCode.Forbidden => new LoginResult(false, "This account is not allowed to sign in. Contact your administrator."),
                _ => new LoginResult(false, $"Sign-in failed (error {(int)response.StatusCode}). Try again.")
            };
        }
        catch (HttpRequestException)
        {
            return new LoginResult(false, "Can't reach the server. Check that the API is running and the URL is correct.");
        }
        catch (TaskCanceledException)
        {
            return new LoginResult(false, "The server took too long to respond. Try again.");
        }
    }

    public void Logout() => _session.Clear();
}

public class ApiService
{
    private readonly HttpClient _http;
    private readonly SessionService _session;

    public ApiService(HttpClient http, SessionService session)
    {
        _http = http;
        _session = session;
    }

    public Task<ApiResult<DashboardSummaryDto>> GetDashboardSummaryAsync() =>
        SendAsync<DashboardSummaryDto>(HttpMethod.Get, "api/dashboard/leave-summary");

    public Task<ApiResult<List<EmployeeDto>>> GetEmployeesAsync() =>
        SendAsync<List<EmployeeDto>>(HttpMethod.Get, "api/employee");

    public Task<ApiResult<EmployeeDto>> GetMyDetailsAsync() =>
        SendAsync<EmployeeDto>(HttpMethod.Get, "api/employee/me");

    public Task<ApiResult<List<LeaveTypeDto>>> GetLeaveTypesAsync() =>
        SendAsync<List<LeaveTypeDto>>(HttpMethod.Get, "api/leavetypes/getall");

    public Task<ApiResult<List<LeaveBalanceDto>>> GetLeaveBalanceAsync(int employeeId) =>
        SendAsync<List<LeaveBalanceDto>>(HttpMethod.Get, $"api/employees/{employeeId}/leave-balance");

    public Task<ApiResult<List<LeaveRequestDto>>> GetLeavesAsync(int? employeeId = null) =>
        SendAsync<List<LeaveRequestDto>>(HttpMethod.Get, employeeId is null ? "api/leaves" : $"api/leaves?employeeid={employeeId}");

    public Task<ApiResult<LeaveRequestDto>> ApplyLeaveAsync(ApplyLeaveRequest request) =>
        SendAsync<LeaveRequestDto>(HttpMethod.Post, "api/leaves", request);

    public Task<ApiResult<LeaveRequestDto>> ApproveLeaveAsync(LeaveActionRequest request) =>
        SendAsync<LeaveRequestDto>(HttpMethod.Put, $"api/leaves/{request.Id}/approve", request);

    public Task<ApiResult<LeaveRequestDto>> RejectLeaveAsync(LeaveActionRequest request) =>
        SendAsync<LeaveRequestDto>(HttpMethod.Put, $"api/leaves/{request.Id}/reject", request);

    public Task<ApiResult<List<EmployeeDto>>> GetAllUsersAsync() =>
        SendAsync<List<EmployeeDto>>(HttpMethod.Get, "api/admin/users");

    public Task<ApiResult> CreateAdminEmployeeAsync(CreateUserRequest request) =>
        SendNoContentAsync(HttpMethod.Post, "api/admin/employees", request);

    public Task<ApiResult> CreateManagerAsync(CreateUserRequest request) =>
        SendNoContentAsync(HttpMethod.Post, "api/admin/managers", request);

    public Task<ApiResult> ChangeUserStatusAsync(int employeeId, bool isActive) =>
        SendNoContentAsync(HttpMethod.Put, $"api/admin/users/{employeeId}/status?employeeId={employeeId}", new ChangeStatusRequest { IsActive = isActive });

    public Task<ApiResult> ChangeEmployeeStatusByManagerAsync(int employeeId, bool isActive) =>
        SendNoContentAsync(HttpMethod.Put, $"api/employee/{employeeId}/status", new ChangeStatusRequest { IsActive = isActive });

    public Task<ApiResult<EmployeeDto>> GetEmployeeByIdAsync(int employeeId) =>
        SendAsync<EmployeeDto>(HttpMethod.Get, $"api/employee/getbyid/{employeeId}");

    public Task<ApiResult> CreateEmployeeByManagerAsync(CreateUserRequest request) =>
        SendNoContentAsync(HttpMethod.Post, "api/employee/createemployee", request);

    public Task<ApiResult<EmployeeDto>> UpdateEmployeeAsync(int employeeId, EmployeeDto employee) =>
        SendAsync<EmployeeDto>(HttpMethod.Put, $"api/employee/updateemployee/{employeeId}", employee);

    public Task<ApiResult> DeleteEmployeeAsync(int employeeId) =>
        SendNoContentAsync(HttpMethod.Delete, $"api/employee/{employeeId}");

    public Task<ApiResult<LeaveTypeDto>> CreateLeaveTypeAsync(CreateLeaveTypeRequest request) =>
        SendAsync<LeaveTypeDto>(HttpMethod.Post, "api/leavetypes/create-leavetype", request);

    public Task<ApiResult<LeaveTypeDto>> UpdateLeaveTypeAsync(UpdateLeaveTypeRequest request) =>
        SendAsync<LeaveTypeDto>(HttpMethod.Put, $"api/leavetypes/update-leavetype/{request.Leavetypeid}", request);

    public Task<ApiResult> ActivateLeaveTypeAsync(int id) =>
        SendNoContentAsync(HttpMethod.Put, $"api/admin/leave-types/{id}/activate", new { });

    public Task<ApiResult> DeactivateLeaveTypeAsync(int id) =>
        SendNoContentAsync(HttpMethod.Put, $"api/admin/leave-types/{id}/deactivate", new { });

    public Task<ApiResult> DeleteLeaveTypeAsync(int id) =>
        SendNoContentAsync(HttpMethod.Delete, $"api/leavetypes/delete-leavetype/{id}");

    public Task<ApiResult<List<LeaveRequestDto>>> GetEmployeeLeavesAsync(int employeeId) =>
        SendAsync<List<LeaveRequestDto>>(HttpMethod.Get, $"api/employees/{employeeId}/leaves");

    public Task<ApiResult<LeaveRequestDto>> GetLeaveByIdAsync(int id) =>
        SendAsync<LeaveRequestDto>(HttpMethod.Get, $"api/leaves/{id}");

    public Task<ApiResult> ChangeUserToManagerAsync(int employeeId) =>
        SendNoContentAsync(HttpMethod.Put, $"api/admin/users/{employeeId}/change-to-manager?employeeId={employeeId}", new { });

    public Task<ApiResult> ChangeUserToEmployeeAsync(int employeeId, int managerId) =>
        SendNoContentAsync(
            HttpMethod.Put,
            $"api/admin/users/{employeeId}/change-to-employee?employeeId={employeeId}",
            new ChangeToEmployeeRequest { ManagerId = managerId });

    public Task<ApiResult<EmployeeDto>> GetAdminEmployeeByIdAsync(int employeeId) =>
        SendAsync<EmployeeDto>(HttpMethod.Get, $"api/admin/employees/{employeeId}?employeeId={employeeId}");
    public Task<ApiResult<EmployeeDto>> UpdateAdminEmployeeAsync(int employeeId, EmployeeDto employee) =>
        SendAsync<EmployeeDto>(HttpMethod.Put, $"api/admin/employees/{employeeId}?employeeId={employeeId}", employee);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (_session is not null && !string.IsNullOrWhiteSpace(_session.Token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.Token);
            }

            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<T>.Fail(await ReadErrorAsync(response));
            }

            if (typeof(T) == typeof(object) || response.Content.Headers.ContentLength == 0)
            {
                return ApiResult<T>.Ok(default);
            }

            var value = await response.Content.ReadFromJsonAsync<T>();
            return ApiResult<T>.Ok(value);
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Fail(ex.Message);
        }
    }

    private async Task<ApiResult> SendNoContentAsync(HttpMethod method, string path, object? body = null)
    {
        var result = await SendAsync<object>(method, path, body);
        return result.Success ? ApiResult.Ok() : ApiResult.Fail(result.Message);
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(body)
            ? $"Request failed: {(int)response.StatusCode} {response.ReasonPhrase}"
            : body;
    }
}
