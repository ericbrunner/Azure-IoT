using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Zone.IoT.App.ViewModels;
using Zone.IoT.WpfUserControlLibrary.Events;

namespace Zone.IoT.WpfUserControlLibrary
{
    /// <summary>
    /// Interaction logic for IotDashboard.xaml
    /// </summary>
    public partial class IotDashboard : Grid
    {
        public event Events.Events.SendButtonClickedDelegate SendButtonClicked;
        private MainWindowViewModel _mainWindowViewModel;

        private MainWindowViewModel ViewModel =>
            _mainWindowViewModel ?? (_mainWindowViewModel = new MainWindowViewModel());

        public Task SetSendButtonColorAsync(SolidColorBrush color)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            try
            {
                ViewModel.SendButtonColor = color;
                tcs.SetResult(true);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }

            return tcs.Task;
        }

        public IotDashboard()
        {
            DataContext = ViewModel;
            ViewModel.SendButtonClicked += ViewModel_SendButtonClicked;
            InitializeComponent();
        }


        private void ViewModel_SendButtonClicked(object sender, WpfEventArgs e)
        {
            e.EventName = "Send to IoT clicked";
            SendButtonClicked?.Invoke(this, e);
        }

        private void Slider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ViewModel.TemperatureThreshold = e.NewValue;
        }

        private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                TextBox textbox = sender as TextBox;
                ViewModel.Interval = Convert.ToInt32(textbox?.Text);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Value Error");
            }
        }
    }
}