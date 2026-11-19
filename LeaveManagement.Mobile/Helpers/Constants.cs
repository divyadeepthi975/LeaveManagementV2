namespace LeaveManagement.Mobile.Helpers;

public static class Constants
{
#if WINDOWS
    public const string BaseUrl = "http://localhost:5018";
#elif ANDROID
    public const string BaseUrl = "http://10.0.2.2:5018";
#else
    public const string BaseUrl = "http://localhost:5018";
#endif
    public const string LoginPath = "api/login/login";

    public const string RoleAdmin = "Admin";
    public const string RoleManager = "Manager";
    public const string RoleEmployee = "Employee";
}