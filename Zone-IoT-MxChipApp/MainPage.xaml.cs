using Zone_IoT_MxChipApp.ViewModels;

namespace Zone_IoT_MxChipApp
{

    public partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();

            BindingContext = new MainPageViewModel(App.ServiceProvider, millisecondsInterval:2000);
        }
    }

}
