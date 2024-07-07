using System;

namespace Zone.IoT.WpfUserControlLibrary.Events
{
    public class WpfEventArgs : EventArgs
    {
        public string EventName { get; set; }
    }

    public class Events
    {
        public delegate void SendButtonClickedDelegate(object sender, WpfEventArgs e);
    }
}