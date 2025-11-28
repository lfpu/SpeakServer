using CommunityToolkit.Maui.Views;

namespace Walkie_Talkie.Pages;

public partial class ChannelPopup : Popup
{
	public string? ChannelNameText{ get;private set; }
    public ChannelPopup()
	{
		InitializeComponent();
	}
	private async void OnAddClicked(object sender, EventArgs e)
	{
		try
		{
            if (!string.IsNullOrWhiteSpace(ChannelName.Text))
            {
                if (ChannelName.Text.Length > 10 || ChannelName.Text.Length < 2)
                throw new Exception("频道名称的字符长度必须大于2位小于10位");
                ChannelNameText = ChannelName.Text;
                await CloseAsync();
            }
            else
            {
                throw new Exception("频道名称不能为空");
            }
        }
		catch (Exception ex)
		{
            await Application.Current.MainPage.DisplayAlert("错误", ex.Message, "确定");
        }
    }
}