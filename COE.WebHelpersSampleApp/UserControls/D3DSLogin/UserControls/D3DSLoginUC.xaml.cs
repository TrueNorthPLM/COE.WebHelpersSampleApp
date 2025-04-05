using COE.WebHelpersSampleApp.UserControls.D3DSLogin.Models;
using COE.WebHelpersSampleApp.UserControls.D3DSLogin.ViewModels;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace COE.WebHelpersSampleApp.UserControls.D3DSLogin.UserControls
{
    /// <summary>
    /// Interaction logic for D3DSLoginUC.xaml
    /// </summary>
    public partial class D3DSLoginUC : UserControl
    {
        private bool _isSettingPassword = false;
        public D3DSLoginUC()
        {
            InitializeComponent();
            PasswordEntryBox.PasswordChanged += OnPasswordChanged;
            PasswordTextBox.TextChanged += OnPasswordTextChanged;
            ShowPasswordCheckBox.Checked += OnShowPasswordChecked;
            ShowPasswordCheckBox.Unchecked += OnShowPasswordUnchecked;

            // Add handler for password property changes
            DependencyPropertyDescriptor
                .FromProperty(PasswordProperty, typeof(D3DSLoginUC))
                .AddValueChanged(this, OnPasswordPropertyChanged);

            // Add handler for RememberPassword property changes
            DependencyPropertyDescriptor
                .FromProperty(RememberPasswordProperty, typeof(D3DSLoginUC))
                .AddValueChanged(this, OnRememberPasswordChanged);

            // add handler for CanShowPassword property changes
            DependencyPropertyDescriptor
                .FromProperty(CanShowPasswordProperty, typeof(D3DSLoginUC))
                .AddValueChanged(this, OnCanShowPasswordChanged);
        }

        private void OnRememberPasswordChanged(object? sender, EventArgs e)
        {
            // Ensure the ViewModel is updated when the UserControl property changes
            if (DataContext is AbstractD3DSLoginViewModel viewModel)
            {
                viewModel.RememberPassword = RememberPassword;
            }
        }

        private void OnCanShowPasswordChanged(object? sender, EventArgs e)
        {
            if (!CanShowPassword && ShowPasswordCheckBox.IsChecked == true)
            {
                ShowPasswordCheckBox.IsChecked = false;
            }
        }

        private void OnPasswordPropertyChanged(object? sender, EventArgs e)
        {
            if (!_isSettingPassword && Password != null)
            {
                _isSettingPassword = true;
                try
                {
                    // Convert SecureString to string safely for the PasswordBox
                    IntPtr unmanagedString = IntPtr.Zero;
                    try
                    {
                        unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(Password);
                        PasswordEntryBox.Password = Marshal.PtrToStringUni(unmanagedString) ?? string.Empty;

                        if (ShowPasswordCheckBox.IsChecked == true)
                        {
                            PasswordTextBox.Text = PasswordEntryBox.Password;
                        }
                    }
                    finally
                    {
                        if (unmanagedString != IntPtr.Zero)
                            Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
                    }
                }
                finally
                {
                    _isSettingPassword = false;
                }
            }
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isSettingPassword)
            {
                _isSettingPassword = true;
                try
                {
                    // Create new SecureString from PasswordBox
                    var secureString = new SecureString();
                    foreach (char c in PasswordEntryBox.Password)
                    {
                        secureString.AppendChar(c);
                    }
                    secureString.MakeReadOnly();
                    Password = secureString;

                    // Enable show password button if password is empty
                    if (PasswordEntryBox.Password.Length == 0)
                    {
                        CanShowPassword = true;
                    }
                }
                finally
                {
                    _isSettingPassword = false;
                }
            }
        }

        private void OnPasswordTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isSettingPassword && ShowPasswordCheckBox.IsChecked == true)
            {
                _isSettingPassword = true;
                try
                {
                    PasswordEntryBox.Password = PasswordTextBox.Text;

                    // Create new SecureString from visible TextBox
                    var secureString = new SecureString();
                    foreach (char c in PasswordTextBox.Text)
                    {
                        secureString.AppendChar(c);
                    }
                    secureString.MakeReadOnly();
                    Password = secureString;

                    // Enable show password button if password is empty
                    if (PasswordTextBox.Text.Length == 0)
                    {
                        CanShowPassword = true;
                    }
                }
                finally
                {
                    _isSettingPassword = false;
                }
            }
        }

        private void OnShowPasswordChecked(object sender, RoutedEventArgs e)
        {
            PasswordTextBox.Text = PasswordEntryBox.Password;
        }

        private void OnShowPasswordUnchecked(object sender, RoutedEventArgs e)
        {
            PasswordEntryBox.Password = PasswordTextBox.Text;
        }

        public string UserName
        {
            get { return (string)GetValue(UserNameProperty); }
            set { SetValue(UserNameProperty, value); }
        }

        public static readonly DependencyProperty UserNameProperty =
            DependencyProperty.Register("UserName",
                typeof(string),
                typeof(D3DSLoginUC),
                new PropertyMetadata(string.Empty));

        public SecureString Password
        {
            get { return (SecureString)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register("Password",
                typeof(SecureString),
                typeof(D3DSLoginUC),
                new PropertyMetadata(null));

        public bool RememberPassword
        {
            get { return (bool)GetValue(RememberPasswordProperty); }
            set { SetValue(RememberPasswordProperty, value); }
        }

        // include TwoWay binding by default
        public static readonly DependencyProperty RememberPasswordProperty =
            DependencyProperty.Register("RememberPassword",
                typeof(bool),
                typeof(D3DSLoginUC),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool CanShowPassword
        {
            get { return (bool)GetValue(CanShowPasswordProperty); }
            set { SetValue(CanShowPasswordProperty, value); }
        }

        public static readonly DependencyProperty CanShowPasswordProperty =
            DependencyProperty.Register("CanShowPassword",
                typeof(bool),
                typeof(D3DSLoginUC),
                new PropertyMetadata(true));

        public ICommand LoginCommand
        {
            get { return (ICommand)GetValue(LoginCommandProperty); }
            set { SetValue(LoginCommandProperty, value); }
        }

        public static readonly DependencyProperty LoginCommandProperty =
            DependencyProperty.Register("LoginCommand",
                typeof(ICommand),
                typeof(D3DSLoginUC),
                new PropertyMetadata(null));

        public ICommand SelectSecurityContextCommand
        {
            get { return (ICommand)GetValue(SelectSecurityContextCommandProperty); }
            set { SetValue(SelectSecurityContextCommandProperty, value); }
        }

        public static readonly DependencyProperty SelectSecurityContextCommandProperty =
            DependencyProperty.Register("SelectSecurityContextCommand",
                typeof(ICommand),
                typeof(D3DSLoginUC),
                new PropertyMetadata(null));

        public string ErrorMessage
        {
            get { return (string)GetValue(ErrorMessageProperty); }
            set { SetValue(ErrorMessageProperty, value); }
        }

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register("ErrorMessage",
                typeof(string),
                typeof(D3DSLoginUC),
                new PropertyMetadata("Status : "));

        public string SelectedCollaborativeSpace
        {
            get { return (string)GetValue(SelectedCollaborativeSpaceProperty); }
            set { SetValue(SelectedCollaborativeSpaceProperty, value); }
        }

        public static readonly DependencyProperty SelectedCollaborativeSpaceProperty =
            DependencyProperty.Register("SelectedCollaborativeSpace",
                typeof(string),
                typeof(D3DSLoginUC),
                new PropertyMetadata(string.Empty));  // set default to something everyone has

        public string SelectedOrganization
        {
            get { return (string)GetValue(SelectedOrganizationProperty); }
            set { SetValue(SelectedOrganizationProperty, value); }
        }

        public static readonly DependencyProperty SelectedOrganizationProperty =
            DependencyProperty.Register("SelectedOrganization",
                typeof(string),
                typeof(D3DSLoginUC),
                new PropertyMetadata(string.Empty));  // set default to something everyone has

        public RoleInfo SelectedRole
        {
            get { return (RoleInfo)GetValue(SelectedRoleProperty); }
            set { SetValue(SelectedRoleProperty, value); }
        }

        public static readonly DependencyProperty SelectedRoleProperty =
            DependencyProperty.Register("SelectedRole",
                typeof(RoleInfo),
                typeof(D3DSLoginUC),
                new PropertyMetadata(null));  // Changed from string.Empty to null since RoleInfo is a reference type

        public IEnumerable<string> CollaborativeSpaces
        {
            get { return (IEnumerable<string>)GetValue(CollaborativeSpacesProperty); }
            set { SetValue(CollaborativeSpacesProperty, value); }
        }

        public static readonly DependencyProperty CollaborativeSpacesProperty =
            DependencyProperty.Register("CollaborativeSpaces",
                typeof(IEnumerable<string>),
                typeof(D3DSLoginUC),
                new PropertyMetadata(null));

        public IEnumerable<string> Organizations
        {
            get { return (IEnumerable<string>)GetValue(OrganizationsProperty); }
            set { SetValue(OrganizationsProperty, value); }
        }

        public static readonly DependencyProperty OrganizationsProperty =
            DependencyProperty.Register("Organizations",
                typeof(IEnumerable<string>),
                typeof(D3DSLoginUC),
                new PropertyMetadata(null));

        public IEnumerable<RoleInfo> Roles
        {
            get { return (IEnumerable<RoleInfo>)GetValue(RolesProperty); }
            set { SetValue(RolesProperty, value); }
        }

        public static readonly DependencyProperty RolesProperty =
            DependencyProperty.Register("Roles",
                typeof(IEnumerable<RoleInfo>),
                typeof(D3DSLoginUC),
                new PropertyMetadata(null));

        public bool IsLoggedIn
        {
            get { return (bool)GetValue(IsLoggedInProperty); }
            set { SetValue(IsLoggedInProperty, value); }
        }

        public static readonly DependencyProperty IsLoggedInProperty =
            DependencyProperty.Register("IsLoggedIn",
                typeof(bool),
                typeof(D3DSLoginUC),
                new PropertyMetadata(false));

        public bool IsLoggingIn
        {
            get { return (bool)GetValue(IsLoggingInProperty); }
            set { SetValue(IsLoggingInProperty, value); }
        }

        public static readonly DependencyProperty IsLoggingInProperty =
            DependencyProperty.Register("IsLoggingIn",
                typeof(bool),
                typeof(D3DSLoginUC),
                new PropertyMetadata(false));

        public bool CanLogIn
        {
            get { return (bool)GetValue(CanLogInProperty); }
            set { SetValue(CanLogInProperty, value); }
        }

        public static readonly DependencyProperty CanLogInProperty =
            DependencyProperty.Register("CanLogIn",
                typeof(bool),
                typeof(D3DSLoginUC),
                new PropertyMetadata(false));

        public bool ShowExitButton
        {
            get { return (bool)GetValue(ShowExitButtonProperty); }
            set { SetValue(ShowExitButtonProperty, value); }
        }

        public static readonly DependencyProperty ShowExitButtonProperty =
            DependencyProperty.Register("ShowExitButton",
                typeof(bool),
                typeof(D3DSLoginUC),
                new PropertyMetadata(false));

        private void SecurityContext_OK_Button_Click(object sender, RoutedEventArgs e)
        {
            var window = _findAncestorWindow();
            if (window != null)
            {
                if (window.DataContext is AbstractD3DSLoginViewModel viewModel)
                {
                    // verify that valid security context has been selected
                    if (!string.IsNullOrEmpty(viewModel.SelectedSecurityContext))
                    {
                        window.DialogResult = true;
                        window.Close();
                    }
                }
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            var window = _findAncestorWindow();
            if (window != null)
            {
                if (window.DataContext is AbstractD3DSLoginViewModel viewModel)
                {
                    window.DialogResult = false;
                    window.Close();
                }
            }
        }

        private Window _findAncestorWindow()
        {
            DependencyObject current = this;
            while (current != null && !(current is Window))
            {
                current = VisualTreeHelper.GetParent(current);
            }
            return current as Window;
        }

        private void D3DSLogin_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AbstractD3DSLoginViewModel viewModel)
            {
                RememberPassword = viewModel.RememberPassword;
                CanShowPassword = !viewModel.IsPasswordLoaded;
            }

            // Existing focus logic
            UIElement elementToBringIntoFocus;
            if (UsernameTextBox.Text.Length == 0)
                elementToBringIntoFocus = UsernameTextBox;
            else if (PasswordEntryBox.Password.Length == 0)
                elementToBringIntoFocus = PasswordEntryBox;
            else
                elementToBringIntoFocus = LoginButton;

            Dispatcher.BeginInvoke(new Action(() => elementToBringIntoFocus.Focus()), System.Windows.Threading.DispatcherPriority.Render);
        }
    }
}