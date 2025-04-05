using Microsoft.Win32;

using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security;
using System.Windows.Input;

using Serilog;

using System.Windows.Controls;
using COE.WebHelpersSampleApp.UserControls.D3DSLogin.Messages;
using COE.WebHelpersSampleApp.UserControls.D3DSLogin.Models;
using COE.WebHelpersSampleApp.Helpers;
using COE.WebHelpersSampleApp.ViewModels;
using COE.WebHelpersSampleApp.Messaging;
using COE.WebHelpersSampleApp.Commands;

namespace COE.WebHelpersSampleApp.UserControls.D3DSLogin.ViewModels
{
    public interface IAbstractD3DSLoginViewModel : IViewModelBase, IDisposable
    {
        ObservableCollection<string> AvailableSecurityContexts { get; set; }
        bool CanLogIn { get; set; }
        ObservableCollection<string> CollaborativeSpaces { get; set; }
        string ErrorMessage { get; set; }
        bool IsLoggedIn { get; set; }
        bool IsLoggingIn { get; set; }
        bool IsCompassSpinning { get; set; }
        bool IsPasswordLoaded { get; }
        bool IsWindowVisible { get; set; }
        ICommand LoginCommand { get; }
        ObservableCollection<string> Organizations { get; set; }
        PassportLoginHelper? PassportLoginHelper { get; }
        SecureString? Password { get; set; }
        string PreferredSecurityContext { get; set; }
        bool RememberPassword { get; set; }
        ObservableCollection<RoleInfo> Roles { get; set; }
        string SelectedCollaborativeSpace { get; set; }
        string SelectedOrganization { get; set; }
        RoleInfo SelectedRole { get; set; }
        string? SelectedSecurityContext { get; }
        ICommand SelectSecurityContextCommand { get; }
        string UserName { get; set; }

        void Logout();
    }

    public abstract class AbstractD3DSLoginViewModel : ViewModelBase, IAbstractD3DSLoginViewModel
    {
        private readonly ILogger _logger;
        private readonly IViewModelMessenger _messenger;

        private bool _loadingFromRegistry = false;
        public PassportLoginHelper? PassportLoginHelper { get; private set; } = null;

        private string _username = string.Empty;
        public string UserName
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
                _updateCanLogIn();
            }
        }

        // Helper method to check if a SecureString is empty
        private bool IsSecureStringEmpty(SecureString? secureString)
        {
            return secureString == null || secureString.Length == 0;
        }

        private SecureString? _password = null;
        public SecureString? Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    // Reset IsPasswordLoaded if:
                    // 1. Password becomes empty, or
                    // 2. Password is changed manually (not from loading from registry)
                    if (!_loadingFromRegistry &&
                        (IsSecureStringEmpty(_password) || IsPasswordLoaded))
                    {
                        IsPasswordLoaded = false;
                    }
                    OnPropertyChanged();
                    _updateCanLogIn();
                }
            }
        }

        private bool _rememberPassword = false;
        public bool RememberPassword
        {
            get => _rememberPassword;
            set
            {
                _rememberPassword = value;
                OnPropertyChanged();
            }
        }

        private bool _isPasswordLoaded = false;
        public bool IsPasswordLoaded
        {
            get => _isPasswordLoaded;
            private set
            {
                _isPasswordLoaded = value;
                OnPropertyChanged();
            }
        }

        private string _errormessage = string.Empty;
        public string ErrorMessage
        {
            get => _errormessage;
            set
            {
                _errormessage = value;
                OnPropertyChanged();
            }
        }

        public bool _isWindowVisible = false;
        public bool IsWindowVisible
        {
            get => _isWindowVisible;
            set
            {
                _isWindowVisible = value;
                OnPropertyChanged();
            }
        }

        private const string REGISTRY_KEY = @"Software\TrueNorthPLM\D3DSLogin";
        private const string USERNAME_VALUE = "LastUsername";
        private const string PASSWORD_VALUE = "LastPassword";
        private const string REMEMBER_PASSWORD_VALUE = "RememberPassword";

        private void SaveUsername()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY))
                {
                    key.SetValue(USERNAME_VALUE, UserName);
                }
                _logger.Debug($"Username saved successfully");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save username: {ex.Message}");
            }
        }

        private void LoadSavedState()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY))
                {
                    if (key != null)
                    {
                        // Load username
                        var savedUsername = key.GetValue(USERNAME_VALUE) as string;
                        if (!string.IsNullOrEmpty(savedUsername))
                        {
                            UserName = savedUsername;
                            _logger.Debug("Username loaded successfully");
                        }

                        // Load RememberPassword state
                        var rememberPassword = key.GetValue(REMEMBER_PASSWORD_VALUE);
                        if (rememberPassword != null)
                        {
                            RememberPassword = Convert.ToBoolean(rememberPassword);
                            _logger.Debug("RememberPassword state loaded successfully");
                        }

                        // Only load password if RememberPassword is true
                        if (RememberPassword)
                        {
                            var encryptedPassword = key.GetValue(PASSWORD_VALUE) as byte[];
                            if (encryptedPassword != null && encryptedPassword.Length > 0)
                            {
                                Password = DecryptPassword(encryptedPassword);
                                IsPasswordLoaded = true;  // Set the flag when password is loaded
                                _logger.Debug("Password loaded successfully");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load saved state: {ex.Message}");
            }
        }

        private void SaveRememberPasswordState()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY))
                {
                    key.SetValue(REMEMBER_PASSWORD_VALUE, RememberPassword);
                    _logger.Debug("RememberPassword state saved successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save RememberPassword state: {ex.Message}");
            }
        }

        // Helper method to convert SecureString to encrypted bytes
        private byte[] EncryptPassword(SecureString password)
        {
            if (password == null || password.Length == 0)
                return Array.Empty<byte>();

            try
            {
                // Convert SecureString to encrypted bytes using DPAPI
                nint unmanagedString = nint.Zero;
                try
                {
                    unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(password);
                    byte[] passwordBytes = new byte[password.Length * 2];
                    Marshal.Copy(unmanagedString, passwordBytes, 0, passwordBytes.Length);

                    // Encrypt the password bytes using the current user's credentials
                    return ProtectedData.Protect(passwordBytes, null, DataProtectionScope.CurrentUser);
                }
                finally
                {
                    if (unmanagedString != nint.Zero)
                        Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to encrypt password: {ex.Message}");
                return Array.Empty<byte>();
            }
        }

        // Helper method to decrypt bytes back to SecureString
        private SecureString? DecryptPassword(byte[] encryptedData)
        {
            if (encryptedData == null || encryptedData.Length == 0)
                return null;

            _loadingFromRegistry = true;
            try
            {
                // Decrypt the data using DPAPI
                byte[] decryptedBytes = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);

                SecureString securePassword = new SecureString();

                // Convert decrypted bytes back to SecureString
                for (int i = 0; i < decryptedBytes.Length; i += 2)
                {
                    securePassword.AppendChar(BitConverter.ToChar(decryptedBytes, i));
                }

                securePassword.MakeReadOnly();

                // Clear the decrypted bytes from memory
                Array.Clear(decryptedBytes, 0, decryptedBytes.Length);

                return securePassword;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to decrypt password: {ex.Message}");
                return null;
            }
            finally
            {
                _loadingFromRegistry = false;
            }
        }

        private void SavePassword()
        {
            try
            {
                if (Password != null)
                {
                    using (var key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY))
                    {
                        byte[] encryptedPassword = EncryptPassword(Password);
                        key.SetValue(PASSWORD_VALUE, encryptedPassword, RegistryValueKind.Binary);
                    }
                    _logger.Debug("Password saved successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save password: {ex.Message}");
            }
        }

        public AbstractD3DSLoginViewModel(ILogger logger, IViewModelMessenger messenger)
        {
            _logger = logger;
            _messenger = messenger;

            // Load the saved state (includes username, password, and remember flag)
            LoadSavedState();

            // subscribe to the D3DS Login message
            _messenger.Subscribe<D3DSLoginMessage>(this, _loginCompleted);

            // subscribe to the UserCredentials message
            _messenger.Subscribe<UserCredentials>(this, OnUserCredentialsReceived);
        }

        private void OnUserCredentialsReceived(object? message)
        {
            // set the passport login helper
            if (message is UserCredentials userCredentials)
            {
                IsCompassSpinning = userCredentials.PassportLoginHelper == null;
            }
        }

        public virtual ICommand LoginCommand => new CommandsBase(_loginExecute);
        private async void _loginExecute(object? obj)
        {
            // show the spinner
            IsLoggingIn = true;

            // fire the login task on a separate thread so we don't lock the UI
            Task.Run(_loginAsync);
        }

        private async Task _loginAsync()
        {
            try
            {
                PassportLoginHelper passportLoginHelper = new PassportLoginHelper(UserName, Password);
                await passportLoginHelper.LoginAsync();

                // send the helper back to the UI thread for stuff to happen
                _messenger.Send(new D3DSLoginMessage(passportLoginHelper));
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                _messenger.Send(new D3DSLoginMessage(null, "Login failed! Check credentials."));
            }

        }

        private void _loginCompleted(object obj)
        {
            var message = (D3DSLoginMessage)obj;

            if (message.passportLoginHelper != null)
            {
                // Always save username and RememberPassword state
                SaveUsername();
                SaveRememberPasswordState();

                // Only save password if RememberPassword is checked
                if (RememberPassword)
                {
                    SavePassword();
                }
                else
                {
                    // Clear any previously saved password
                    try
                    {
                        using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true))
                        {
                            if (key != null && key.GetValue(PASSWORD_VALUE) != null)
                            {
                                key.DeleteValue(PASSWORD_VALUE);
                                _logger.Debug("Saved password cleared successfully");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Failed to clear saved password: {ex.Message}");
                    }
                }

                PassportLoginHelper = message.passportLoginHelper;
                PreferredSecurityContext = PassportLoginHelper.PreferredSecurityContext;

                ObservableCollection<string> availableSecurityContexts = new ObservableCollection<string>();
                foreach (string securityContext in PassportLoginHelper.AvailableSecurityContexts.OrderBy(s => s, StringComparer.Ordinal).ToList())
                {
                    availableSecurityContexts.Add(securityContext);
                }

                AvailableSecurityContexts = availableSecurityContexts;
                UpdateSelectedValues();

                // login successful if we have at least 1 security context
                if (AvailableSecurityContexts.Count > 0)
                {
                    // show the security context selector
                    IsLoggedIn = true;
                }
            }
            else
            {
                ErrorMessage = message.errorMessage;
            }

            // hide the spinner
            IsLoggingIn = false;
        }

        public virtual ICommand SelectSecurityContextCommand => new CommandsBase(_selectSecurityContextExecute);
        private async void _selectSecurityContextExecute(object? obj)
        {
            UpdateSelectedSecurityContext();
        }

        private ObservableCollection<string> _availableSecurityContexts = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableSecurityContexts
        {
            get => _availableSecurityContexts;
            set
            {
                _availableSecurityContexts = value;
                OnPropertyChanged();
                UpdateCollaborativeSpaces();
            }
        }

        private string _preferredSecurityContext = string.Empty;
        public string PreferredSecurityContext
        {
            get => _preferredSecurityContext;
            set
            {
                _preferredSecurityContext = value;
                OnPropertyChanged();

                // default SelectedSecurityContext to PreferredSecurityContext
                SelectedSecurityContext = value;

            }
        }

        private ObservableCollection<string> _collaborativeSpaces;
        public ObservableCollection<string> CollaborativeSpaces
        {
            get => _collaborativeSpaces;
            set
            {
                _collaborativeSpaces = value;
                OnPropertyChanged();
            }
        }

        private string _selectedCollaborativeSpace;
        public string SelectedCollaborativeSpace
        {
            get => _selectedCollaborativeSpace;
            set
            {
                _selectedCollaborativeSpace = value;
                OnPropertyChanged();
                UpdateOrganizations();
            }
        }

        private ObservableCollection<string> _organizations;
        public ObservableCollection<string> Organizations
        {
            get => _organizations;
            set
            {
                _organizations = value;
                OnPropertyChanged();
            }
        }

        private string _selectedOrganization;
        public string SelectedOrganization
        {
            get => _selectedOrganization;
            set
            {
                _selectedOrganization = value;
                OnPropertyChanged();
                UpdateRoles();
            }
        }

        private ObservableCollection<RoleInfo> _roles;
        public ObservableCollection<RoleInfo> Roles
        {
            get => _roles;
            set
            {
                _roles = value;
                OnPropertyChanged(nameof(Roles));
            }
        }

        private RoleInfo _selectedRole;
        public RoleInfo SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                OnPropertyChanged();
            }
        }

        public string? SelectedSecurityContext { get; private set; } = null;

        private bool _isCompassSpinning = true;
        public bool IsCompassSpinning
        {
            get => _isCompassSpinning;
            set
            {
                _isCompassSpinning = value;
                OnPropertyChanged();
            }
        }

        private bool _isLoggingIn = false;
        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set
            {
                _isLoggingIn = value;
                OnPropertyChanged();
            }
        }

        private bool _isLoggedIn = false;
        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set
            {
                _isLoggedIn = value;
                OnPropertyChanged();
            }
        }

        private bool _canLogIn = false;
        public bool CanLogIn
        {
            get => _canLogIn;
            set
            {
                _canLogIn = value;
                OnPropertyChanged();
            }
        }

        private void _updateCanLogIn()
        {
            if (string.IsNullOrEmpty(UserName) || Password == null || Password.Length == 0)
            {
                CanLogIn = false;
            }
            else
                CanLogIn = true;
        }


        private void UpdateCollaborativeSpaces()
        {
            CollaborativeSpaces = new ObservableCollection<string>(
                AvailableSecurityContexts.Select(s => s.Split('.').Last()).Distinct());
            UpdateSelectedValues();
        }

        private void UpdateOrganizations()
        {
            if (SelectedCollaborativeSpace != null)
            {
                Organizations = new ObservableCollection<string>(
                    AvailableSecurityContexts
                        .Where(s => s.EndsWith(SelectedCollaborativeSpace))
                        .Select(s => s.Split('.')[1])
                        .Distinct());

                SelectedOrganization = Organizations.FirstOrDefault();
            }
        }

        private void UpdateRoles()
        {
            if (SelectedCollaborativeSpace != null && SelectedOrganization != null)
            {
                var roleNames = AvailableSecurityContexts
                    .Where(s => s.EndsWith($"{SelectedOrganization}.{SelectedCollaborativeSpace}"))
                    .Select(s => s.Split('.')[0])
                    .Distinct();

                Roles = new ObservableCollection<RoleInfo>(
                    roleNames.Select(roleName => new RoleInfo(
                        roleName,
                        PassportLoginHelper.RoleNLSMappings.ContainsKey(roleName)
                            ? PassportLoginHelper.RoleNLSMappings[roleName]
                            : roleName // Fallback to role name if NLS not found
                    ))
                );

                SelectedRole = Roles.FirstOrDefault();
            }
        }

        private void UpdateSelectedValues()
        {
            if (PreferredSecurityContext != null && !IsLoggedIn)
            {
                var parts = PreferredSecurityContext.Split('.');
                string roleName = parts[0];
                SelectedRole = new RoleInfo(
                    roleName,
                    PassportLoginHelper.RoleNLSMappings.ContainsKey(roleName)
                        ? PassportLoginHelper.RoleNLSMappings[roleName]
                        : roleName
                );
                SelectedOrganization = parts[1];
                SelectedCollaborativeSpace = parts[2];
            }

            UpdateOrganizations();
            UpdateRoles();
        }

        protected void UpdateSelectedSecurityContext()
        {
            if (SelectedRole?.Name != null && !string.IsNullOrEmpty(SelectedOrganization) && !string.IsNullOrEmpty(SelectedCollaborativeSpace))
            {
                SelectedSecurityContext = $"{SelectedRole.Name}.{SelectedOrganization}.{SelectedCollaborativeSpace}";
                OnPropertyChanged(nameof(SelectedSecurityContext));
            }
            else
            {
                SelectedSecurityContext = null;
            }

            if (!string.IsNullOrEmpty(SelectedSecurityContext))
                PassportLoginHelper!.SelectedSecurityContext = SelectedSecurityContext;
        }

        public static void SetSecurePassword(PasswordBox passwordBox, SecureString securePassword)
        {
            passwordBox.Password = ConvertToUnsecureString(securePassword);
        }
        public static SecureString ConvertToSecureString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return new SecureString();

            SecureString secureString = new SecureString();
            foreach (char c in plainText)
            {
                secureString.AppendChar(c);
            }
            secureString.MakeReadOnly();
            return secureString;
        }

        private static string ConvertToUnsecureString(SecureString securePassword)
        {
            if (securePassword == null)
                return string.Empty;

            nint unmanagedString = nint.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        public void Logout()
        {
            IsLoggedIn = false;
            if(RememberPassword == false)
            {
                UserName = string.Empty;
                _password!.Dispose();
                _password = null;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
