using Walkie_Talkie.Models;

namespace Walkie_Talkie
{
    public partial class LoginPage : ContentPage
    {
        //private readonly IAudioUDPService _audioUDPService;
        public LoginPage(LoginViewModel viewModel)
        {
            InitializeComponent();
            //_audioUDPService = audioUDPService;
            BindingContext = viewModel;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

    }
}
