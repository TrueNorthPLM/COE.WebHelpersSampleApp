namespace COE.WebHelpersSampleApp.Messaging
{
    public interface IViewModelMessenger : IMessenger
    {

    }

    public class ViewModelMessenger : Messenger, IViewModelMessenger
    {

    }
}
