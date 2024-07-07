namespace IoTShellApp.Models
{
    public class ChatMessage
    {
        private string _user;

        public string User
        {
            get => _user;
            set
            {
                if (value != null && _user == value)
                    return;

                _user = value;
            }
        }

        private string _message;

        public string Message
        {
            get => _message;
            set
            {
                if (value != null && _message == value)
                    return;

                _message = value;
            }
        }
    }
}