using CommunityToolkit.Maui.Extensions;
using Speaker.Services;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace Speaker.Pages;

public partial class ChatPage : ContentPage
{
    public ObservableCollection<UserInfo> JoinedUsers { get; set; }
    private string User;
    private UserInfo CurrentUser { get; set; }

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

        // 随机生成几个用户
        //var random = new Random();
        //var names = new[] { "张三", "李四", "王五", "赵六", "周七" };
        //for (int i = 0; i < names.Length; i++)
        //{
        //    JoinedUsers.Add(new UserInfo { UserName = names[i], State = UserStatus.Active });
        //}
        CurrentUser = new UserInfo { UserName = User, State = UserStatus.Active };
        if (User.ToLower() == "admin") CurrentUser.SetAdmin();
        JoinedUsers.Add(CurrentUser);
        BindingContext = this;
        service.SetUser(User);
        SpeakService = service;
        SpeakService.Start();
        GetUsers();
    }
    private void GetUsers()
    {
        Task.Run(async () =>
        {
            await Task.Delay(2000);
            while (GetUserTask)
            {
                await Task.Delay(100);
                var users =await SpeakService.GetAllUsers();
                if (users == null || users.Count == 0) continue;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var offlines = new List<UserInfo>();
                    foreach (var item in JoinedUsers)
                    {
                        var exsit = users.FirstOrDefault(a => a.UserName == item.UserName);
                        if(exsit == null) offlines.Add(item);
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
                            JoinedUsers.Add(new UserInfo() { UserName = user.UserName, State = user.State,IsMuted=user.IsMuted,IsSpeaking=user.IsSpeaking });
                        }
                        else
                        {
                            joined.State =joined.State==UserStatus.Private?joined.State: user.State;
                            joined.IsSpeaking = user.IsSpeaking;
                            joined.IsMuted = user.IsMuted;
                        }
                    }

                });
            }
        });
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
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
        if (args.NavigationType == NavigationType.PopToRoot)
        {
            GetUserTask = false;
            SpeakService.Dispose();
        }
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
        await SpeakBtn.ScaleToAsync(1.2, 100); // 放大动画
        SpeakService.RecordAndSend();
    }

    private async void OnSpeakReleased(object sender, EventArgs e)
    {
        // 停止录音逻辑
        SpeakBtn.BackgroundColor = Color.FromArgb("#526B52");
        SpeakBtn.Text = "按住说话";
        await SpeakBtn.ScaleToAsync(1.0, 100); // 恢复原始大小
        SpeakService.StopRecordSend();
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
            frame.ScaleToAsync(1.1, 100);
        }
    }
    private void OnPointerExited(object sender, PointerEventArgs e)
    {
        if (sender is Border frame && frame.BindingContext is UserInfo user)
        {
            frame.ScaleToAsync(1.0, 100);
        }
    }

}