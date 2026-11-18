namespace LeaveManagement.Mobile.Models;

public class DashboardSummaryDto
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int PendingRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public int RejectedRequests { get; set; }
    public int TotalLeavesTaken { get; set; }
}

public class EmployeeDto
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime JoiningDate { get; set; }
    public bool IsActive { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class CreateUserRequest
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime JoiningDate { get; set; }
    public int? ManagerId { get; set; }
}

public class ChangeStatusRequest
{
    public bool IsActive { get; set; }
}

public class ChangeToEmployeeRequest
{
    public int ManagerId { get; set; }
}

public class LeaveTypeDto
{
    public int Leavetypeid { get; set; }
    public string Leavetypename { get; set; } = string.Empty;
    public int Maximumdays { get; set; }
    public bool IsActive { get; set; }
}

public class CreateLeaveTypeRequest
{
    public string Leavetypename { get; set; } = string.Empty;
    public int Maximumdays { get; set; }
}

public class UpdateLeaveTypeRequest
{
    public int Leavetypeid { get; set; }
    public string Leavetypename { get; set; } = string.Empty;
    public int Maximumdays { get; set; }
    public bool IsActive { get; set; }
}

public class LeaveBalanceDto
{
    public string LeaveType { get; set; } = string.Empty;
    public int TotalDays { get; set; }
    public int UsedDays { get; set; }
    public int RemainingDays { get; set; }
}

public class LeaveRequestDto
{
    public int Leaverequestid { get; set; }
    public int Employeeid { get; set; }
    public int Leavetypeid { get; set; }
    public DateTime Fromdate { get; set; }
    public DateTime Todate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Applieddate { get; set; }
    public string? Approvedby { get; set; }
    public string? Comments { get; set; }
}

public class ApplyLeaveRequest
{
    public int Employeeid { get; set; }
    public int Leavetypeid { get; set; }
    public DateTime Fromdate { get; set; }
    public DateTime Todate { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class LeaveActionRequest
{
    public int Id { get; set; }
    public string Approvedby { get; set; } = string.Empty;
    public string? Comments { get; set; }
}

public class ApiEnvelope<T>
{
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

public class ApiResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static ApiResult Ok(string message = "Success") =>
        new() { Success = true, Message = message };

    public static ApiResult Fail(string message) =>
        new() { Success = false, Message = message };
}

public class ApiResult<T> : ApiResult
{
    public T? Value { get; init; }

    public static ApiResult<T> Ok(T? value, string message = "Success") =>
        new() { Success = true, Message = message, Value = value };

    public new static ApiResult<T> Fail(string message) =>
        new() { Success = false, Message = message };
}
