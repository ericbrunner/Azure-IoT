using System;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using Zone.IoT.WpfUserControlLibrary;
using Zone.IoT.WpfUserControlLibrary.Events;
using Color = System.Drawing.Color;

namespace Zone.IoT.App.WinFormsWpfHost
{
    public partial class Form1 : Form
    {
        readonly Random _rand = new Random();
        private ElementHost _ctrHost;
        private IotDashboard _wpfAddressCtrl;


        public Form1()
        {
            InitializeComponent();

            Program.TelemetryClient.TrackPageView(nameof(Form1));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            #region Init WPF Host

            _ctrHost = new ElementHost {Dock = DockStyle.Fill};
            panel1.Controls.Add(_ctrHost);

            #region Create 'n Init WPF UserControl (child to host)

            _wpfAddressCtrl = new IotDashboard();
            _wpfAddressCtrl.SendButtonClicked += _wpfAddressCtrl_SendButtonClicked;
            //wpfAddressCtrl.InitializeComponent();

            #endregion

            _ctrHost.Child = _wpfAddressCtrl;

            #endregion
        }

        private void _wpfAddressCtrl_SendButtonClicked(object sender, WpfEventArgs e)
        {
            wpfEventsLabel.Text = e.EventName;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(@"Hello from WinForms", @"Winforms Dialog");
        }


        private async void button2_Click(object sender, EventArgs e)
        {
            Color rootColor = Color.FromArgb(_rand.Next(0, 256), _rand.Next(0, 256), _rand.Next(0, 256));

            System.Windows.Media.Color color =
                System.Windows.Media.Color.FromArgb(rootColor.A, rootColor.R, rootColor.G, rootColor.B);

            await _wpfAddressCtrl.SetSendButtonColorAsync(new SolidColorBrush(color));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            throw new Exception("Unhandled Ex");
        }
    }
}