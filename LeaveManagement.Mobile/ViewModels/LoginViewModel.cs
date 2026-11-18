using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeaveManagement.Mobile.Services;

namespace LeaveManagement.Mobile.ViewModels;

public class LoginViewModel : ObservableObject
{
    private readonly AuthService _auth;
    private readonly SessionService _session;

    public LoginViewModel(AuthService auth, SessionService session)
    {
        _auth = auth;
        _session = session;
        LoginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);
    }

    public IAsyncRelayCommand LoginCommand { get; }

    private string _username = string.Empty;
    public string Username
    {
        get => _username;
        set
        {
            if (SetProperty(ref _username, value))
                LoginCommand.NotifyCanExecuteChanged();
        }
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
                LoginCommand.NotifyCanExecuteChanged();
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
                LoginCommand.NotifyCanExecuteChanged();
        }
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    private bool CanLogin() =>
        !IsBusy &&
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password);

    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            var result = await _auth.LoginAsync(Username.Trim(), Password);
            if (!result.Success)
            {
                ErrorMessage = result.Error ?? "Sign-in failed.";
                return;
            }

            Password = string.Empty;
            App.GoToRoleShell(_session.Role);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task TryRestoreSessionAsync()
    {
        await _session.LoadAsync();
        if (_session.IsLoggedIn)
            App.GoToRoleShell(_session.Role);
    }
}