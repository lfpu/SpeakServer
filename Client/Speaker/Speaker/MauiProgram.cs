using Microsoft.Extensions.Logging;
using Speaker.Models;
using Speaker.Services;
using CommunityToolkit.Maui;


#if WINDOWS
using Speaker.Windows;
#elif ANDROID
using Speaker.AndroidPlatform.Service;
#endif


namespace Speaker
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
#endif
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddSingleton<AppShell>();

            return builder.Build();
        }
    }
}
