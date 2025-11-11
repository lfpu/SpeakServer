using Speaker.Models;
using Speaker.Pages;
using Speaker.Services;

namespace Speaker
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
