using COE.WebHelpersSampleApp.Messaging;
using COE.WebHelpersSampleApp.ViewModels;
using Serilog;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace COE.WebHelpersSampleApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger _logger;
    private readonly IViewModelMessenger _messenger;
    public MainWindow()
    {
        // create new serilog ILogger
        _logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        // create the messenger
        _messenger = new ViewModelMessenger();

        InitializeComponent();

        // set main window data context
        DataContext = new MainViewModel(_logger, _messenger);

        // set the data context for the login user control
        loginUC.DataContext = new LoginViewModel(_logger, _messenger);
    }
}