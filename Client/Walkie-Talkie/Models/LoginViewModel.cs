using Microsoft.Maui.Controls;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Walkie_Talkie.Pages;
using Walkie_Talkie.Services;

namespace Walkie_Talkie.Models
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string _username;
        private bool _isBusy;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }
        private readonly IAudioUDPService _audioUDPService;
        public LoginViewModel(IAudioUDPService audioUDPService)
        {
            LoginCommand = new Command(async () => await LoginAsync());
            this._audioUDPService = audioUDPService;
        }

        private async Task LoginAsync()
        {
            IsBusy = true;
            await Task.Delay(100); // Simulate a short delay for better UX
            if (!string.IsNullOrWhiteSpace(Username))
            {
                if (Username.Length > 8 || Username.Length <= 2)
                {
                    await Application.Current.MainPage.DisplayAlert("错误", "用户名的字符长度必须大于2位小于8位", "确定");
                    IsBusy = false;
                    return;
                }
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("错误", "请输入用户名", "确定");
                IsBusy = false;
                return;
            }
            var serverMsg = await _audioUDPService.ConnectAsync(Username);
            if (!serverMsg.Connected)
            {
                await Application.Current.MainPage.DisplayAlert(serverMsg.Action, serverMsg.Reason, "确定");
                IsBusy = false;
                return;
            }
            IsBusy = false;
            await Application.Current.MainPage.Navigation.PushAsync(new ChatPage(Username, _audioUDPService));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
