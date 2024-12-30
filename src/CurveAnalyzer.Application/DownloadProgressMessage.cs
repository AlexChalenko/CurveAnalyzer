using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CurveAnalyzer.Application
{
    public class DownloadProgressMessage : ValueChangedMessage<double>
    {
        public DownloadProgressMessage(double value) : base(value)
        {
        }
    }

    public class DownloadCompletedMessage()
    {

    }
}
