using CommunityToolkit.Maui.Views;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Walkie_Talkie.Services;


namespace Walkie_Talkie.Pages;

public partial class ChatPage : ContentPage
{
    public ObservableCollection<UserInfo> JoinedUsers { get; set; }
    private string User;
    private UserInfo CurrentUser { get; set; }
    private ObservableCollection<string> _channels = new ObservableCollection<string>();
    public ObservableCollection<string> Channels { get; set; } = new ObservableCollection<string>() { "Lobby" };
    public string SelectedChannel { get; set; } = "Lobby";
    public static readonly BindableProperty ItemSpanProperty =
        BindableProperty.Create(nameof(ItemSpan), typeof(int), typeof(ChatPage), 3);

    public int ItemSpan
    {
        get => (int)GetValue(ItemSpanProperty);
        set => SetValue(ItemSpanProperty, value);
    }

    public IAudioUDPService SpeakService { get; set; }

    private bool GetUserTask { get; set; } = true;


    public ChatPage(string username, IAudioUDPService service)
    {
        User = username;
        InitializeComponent();
        this.Title = $"Welcome {User}, let's chat";
        SizeChanged += ChatPage_SizeChanged;
        // 初始化用户列表
        JoinedUsers = new ObservableCollection<UserInfo>();

        CurrentUser = new UserInfo { UserName = User, State = UserStatus.Active };
        if (User.ToLower() == "admin") CurrentUser.SetAdmin();
        JoinedUsers.Add(CurrentUser);
        BindingContext = this;
        service.SetUser(User);
        SpeakService = service;
        SpeakService.Start();
        GetUsers();
        GetChannels();
    }

    private void GetUsers()
    {
        Task.Run(async () =>
        {
            await Task.Delay(1000);
            while (GetUserTask)
            {
                await Task.Delay(200);
                var users = await SpeakService.GetAllUsers();
                if (users == null || users.Count == 0) continue;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var offlines = new List<UserInfo>();
                    foreach (var item in JoinedUsers)
                    {
                        var exsit = users.FirstOrDefault(a => a.UserName == item.UserName);
                        if (exsit == null) offlines.Add(item);
                    }
                    foreach (var item in offlines)
                    {
                        JoinedUsers.Remove(item);
                    }
                    foreach (var user in users)
                    {
                        var joined = JoinedUsers.FirstOrDefault(a => a.UserName == user.UserName);
                        if (joined == null)
                        {
                            JoinedUsers.Add(new UserInfo() { UserName = user.UserName, State = user.State, IsMuted = user.IsMuted, IsSpeaking = user.IsSpeaking });
                        }
                        else
                        {
                            joined.State = joined.State == UserStatus.Private ? joined.State : user.State;
                            joined.IsSpeaking = user.IsSpeaking;
                            joined.IsMuted = user.IsMuted;
                        }
                    }

                });
            }
        });
    }
    private void GetChannels()
    {
        Task.Run(async () =>
        {
            await Task.Delay(1500);
            while (GetUserTask)
            {
                await Task.Delay(5000);
                var channels = await SpeakService.GetAllChannels();
                if (channels == null || channels.Count == 0) continue;
                // 处理频道列表
                try
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        for (int i = 0; i < Channels.Count; i++)
                        {
                            var exsit= channels.FirstOrDefault(a => a == Channels[i]);
                            if (exsit == null) { Channels.RemoveAt(i);continue; }
                        }
                        var subchannels =channels.Except(Channels).ToArray();
                        if(subchannels.Length==0) return;
                        foreach (var item in subchannels)
                        {
                            Channels.Add(item);
                        }
                    });
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }
        });
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
    }
    public void OnPickerSelectedIndexChanged(object sender, EventArgs e)
    {
        if (ChannelPicker.SelectedItem != null)
        {
            string selectedChannel = ChannelPicker.SelectedItem.ToString() ?? "Lobby";
            SpeakService?.SetChannel(selectedChannel);
            SelectedChannel= selectedChannel;
            CurrentChannel.Text= $" {SelectedChannel}";
            CurrentChannel.ClassId= SelectedChannel;
        }
    }
    private void ChatPage_SizeChanged(object? sender, EventArgs e)
    {

        double windowWidth = this.Width;
        double itemWidth = 100;
        double itemMargin = 10;
        double pagePadding = 10;

        double usableWidth = windowWidth - pagePadding;
        double totalItemWidth = itemWidth + itemMargin;
        int span = (int)(usableWidth / totalItemWidth);
        ItemSpan = Math.Max(span, 1);


    }
    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        //if (args.NavigationType == NavigationType.PopToRoot)
        //{
        //    GetUserTask = false;
        //    SpeakService.Dispose();
        //}
        base.OnNavigatingFrom(args);
    }
    protected override void OnDisappearing()
    {
        if (Application.Current?.MainPage?.Navigation?.NavigationStack?.Contains(this) == false)
        {
            GetUserTask = false;
            SpeakService.Dispose(); // 只有真正离开导航栈时才销毁
        }
        base.OnDisappearing();

    }
    private async void OnSpeakPressed(object sender, EventArgs e)
    {
        // 开始录音逻辑
        SpeakBtn.BackgroundColor = Colors.LightGreen;
        SpeakBtn.Text = "正在说话";
        await SpeakBtn.ScaleTo(1.2, 100); // 放大动画
        SpeakService.RecordAndSend();
    }

    private async void OnSpeakReleased(object sender, EventArgs e)
    {
        // 停止录音逻辑
        SpeakBtn.BackgroundColor = Color.FromArgb("#526B52");
        SpeakBtn.Text = "按住说话";
        await SpeakBtn.ScaleTo(1.0, 100); // 恢复原始大小
        SpeakService.StopRecordSend();
    }
    private async void OnChannelClicked(object sender, EventArgs e)
    {
        var prop = new ChannelPopup();
        await this.ShowPopupAsync(prop);
        if (!string.IsNullOrEmpty(prop.ChannelNameText))
        {
            //await SpeakService.AddChannel(prop.ChannelNameText);
            Channels.Add(prop.ChannelNameText);
            SpeakService.SetChannel(prop.ChannelNameText);
            SelectedChannel = prop.ChannelNameText;
            ChannelPicker.SelectedItem = prop.ChannelNameText;
        }
    }
    private async void OnBorderTapped(object sender, TappedEventArgs e)
    {
        if (sender is Border frame && frame.BindingContext is UserInfo user)
        {
            var popup = new ChatPopup();
            popup.IsAdmin = CurrentUser.Authority == Authority.Admin;
            popup.BindingContext = user;
            await this.ShowPopupAsync(popup);
            SpeakService.SpeakTo = "all";
            for (int i = 0; i < JoinedUsers.Count; i++)
            {
                if (JoinedUsers.ElementAt(i).State == UserStatus.Private)
                {
                    SpeakService.SpeakTo = JoinedUsers.ElementAt(i).UserName; break;
                }
            }
        }
    }

    private void OnPointerEntered(object sender, PointerEventArgs e)
    {
        if (sender is Border frame && frame.BindingContext is UserInfo user)
        {
            frame.ScaleTo(1.1, 100);
        }
    }
    private void OnPointerExited(object sender, PointerEventArgs e)
    {
        if (sender is Border frame && frame.BindingContext is UserInfo user)
        {
            frame.ScaleTo(1.0, 100);
        }
    }

}