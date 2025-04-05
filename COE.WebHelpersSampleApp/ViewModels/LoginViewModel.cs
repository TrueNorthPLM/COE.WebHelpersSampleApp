using System.Windows.Input;
using COE.WebHelpersSampleApp.Commands;
using COE.WebHelpersSampleApp.Messaging;
using COE.WebHelpersSampleApp.UserControls.D3DSLogin.ViewModels;
using Serilog;

namespace COE.WebHelpersSampleApp.ViewModels
{
    public interface ILoginViewModel : IAbstractD3DSLoginViewModel
    {
        // for DI
    }
    public class LoginViewModel : AbstractD3DSLoginViewModel, ILoginViewModel
    {
        private readonly ILogger _logger;
        private readonly IViewModelMessenger _messenger;
        public LoginViewModel
            (ILogger logger,
            IViewModelMessenger messenger) : base(logger, messenger) 
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.ForContext<LoginViewModel>();

            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        public override ICommand SelectSecurityContextCommand => new CommandsBase((Action<object?>)_selectSecurityContextExecute);

        private void _selectSecurityContextExecute(object? obj)
        {
            // NEED TO CALL THIS TO UPDATE THE SECURITY CONTEXT!
            UpdateSelectedSecurityContext();

            _messenger.Send(new UserCredentials(this.PassportLoginHelper));
        }
    }
}
