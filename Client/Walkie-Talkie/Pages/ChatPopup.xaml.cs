using CommunityToolkit.Maui.Views;
using Walkie_Talkie.Services;

namespace Walkie_Talkie.Pages;

public partial class ChatPopup : Popup
{
    public bool IsAdmin { get; set; } = false;
    public ChatPopup()
    {
        InitializeComponent();

    }

    private async void OnMuteClicked(object sender, EventArgs e)
    {
        if (!IsAdmin)
        {
            await Application.Current.MainPage.DisplayAlert("错误", "你没有权限", "确定");
            return;
        }
        var user = (UserInfo)BindingContext;
        user.State = user.State == UserStatus.Mute ? UserStatus.Active : UserStatus.Mute;
        await CloseAsync();
    }

    private async void OnChatPrivateClicked(object sender, EventArgs e)
    {
        var user = (UserInfo)BindingContext;
        user.State = user.State == UserStatus.Private ? UserStatus.Active : UserStatus.Private;
        await CloseAsync();
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await CloseAsync();
    }

}