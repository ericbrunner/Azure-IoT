using System;
using System.Windows;
using System.Windows.Controls;
using Zone.IoT.App.ViewModels;

namespace Zone.IoT.App
{
    /// <inheritdoc cref="MainWindow" />
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            App.TelemetryClient.TrackPageView(nameof(MainWindow));
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            throw new Exception("Unhandled ex.");
        }
    }
}