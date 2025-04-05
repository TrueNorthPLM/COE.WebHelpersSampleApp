using COE.WebHelpersSampleApp.Helpers;

namespace COE.WebHelpersSampleApp.UserControls.D3DSLogin.Messages
{
    public record D3DSLoginMessage(PassportLoginHelper? passportLoginHelper, string errorMessage = "");
}
