

Windows publish:
      dotnet publish -c Release -f net9.0-windows10.0.19041.0 --self-contained true -r win-x64 /p:UseMonoRuntime=false

Android publish:
       dotnet publish -f net9.0-android -c Release

IOS publish:
       dotnet publish -f net9.0-ios -c Release