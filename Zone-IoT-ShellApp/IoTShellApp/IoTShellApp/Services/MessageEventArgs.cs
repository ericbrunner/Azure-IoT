namespace IoTShellApp.Services
{
    public class MessageEventArgs : IMessageEventArgs
    {
        public MessageEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}
