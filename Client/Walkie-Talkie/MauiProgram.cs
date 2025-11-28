using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Walkie_Talkie.Models;
using Walkie_Talkie.Services;

#if WINDOWS
using Walkie_Talkie.Platforms.WindowsService;
#elif ANDROID
using Walkie_Talkie.Platforms.AndroidService;
#elif IOS
using Walkie_Talkie.Platforms.iOSService;
#endif

namespace Walkie_Talkie
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif
#if WINDOWS
builder.Services.AddSingleton<IAudioUDPService, WindowsUDPService>();
#elif ANDROID
            builder.Services.AddSingleton<IAudioUDPService, AndroidAudioUDPService>();
#elif IOS
builder.Services.AddSingleton<IAudioUDPService, IOSUDPService>();
#endif
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddSingleton<AppShell>();

            return builder.Build();
        }
    }
}
