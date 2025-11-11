using Microsoft.Maui.Controls;
using Speaker.Pages;
using Speaker.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;


namespace Speaker.Models
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
            if (!string.IsNullOrWhiteSpace(Username))
            {
                if (Username.Length > 8 || Username.Length <= 2)
                {
                    await Application.Current.MainPage.DisplayAlertAsync("错误", "用户名的字符长度必须大于2位小于8位", "确定");
                    IsBusy = false;
                    return;
                }
            }
            else
            {
                await Application.Current.MainPage.DisplayAlertAsync("错误", "请输入用户名", "确定");
                IsBusy=false;
                return;
            }
            var IsConnect= await _audioUDPService.ConnectAsync();
            IsBusy = false;
            if (!IsConnect) { await Application.Current.MainPage.DisplayAlertAsync("错误", "连接服务器失败！", "确定"); return; }

            await Application.Current.MainPage.Navigation.PushAsync(new ChatPage(Username, _audioUDPService));

        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
