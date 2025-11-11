# SpeakServer - UDP Voice Communication Server & Client

A lightweight UDP server and client implementation for walkie-talkie style voice communication.

- Server built with C++ and Boost.Asio for efficient network handling.
- Client built with .NET MAUI for cross-platform user interface and audio handling.

---

## Features

### Server
- UDP-based real-time voice transmission
- Support for multiple concurrent clients
- Client heartbeat monitoring
- Configurable server settings
- Auto cleanup of inactive sessions
- Control message system for client management

### Client
- Cross-platform UI using .NET MAUI
- Real-time voice capture and playback
- Automatic server discovery and connection
- Voice data transmission over UDP
- Heartbeat and control message support via dedicated ports

---

## Prerequisites

### Server
- C++17 compatible compiler
- Boost libraries
- CMake 3.10 or higher
- Visual Studio 2022 (for Windows builds)

### Client
- .NET 7.0 SDK or higher
- .NET MAUI workload installed
- Visual Studio 2022 or Visual Studio 2022 for Mac with MAUI support

---

## Configuration

Server settings are stored in [`config/config.json`](config/config.json).

Make sure `config.json` is in the same folder as the executable.

**Note:** Client receive port config is removed from server config, as the client automatically manages its own ports.

```json
{
  "server": {
    "Port": 8000,           // Main server port
    "heartbeat_Port": 8003, // Heartbeat monitoring port
    "MaxConnections": 100   // Maximum concurrent connections
  },
  "client": {
    "RecievePort": 8001,    // Client audio receive port
    "ControlPort": 8002     // Client control message port
  }
}
```


## Build Instructions

Build Server:

```cmake

mkdir build
cd build
cmake ..
cmake --build . --config Release
```

Build Client:

Client code are stored in [`Client/`](config folder).

1.Open the SpeakClient.sln solution in Visual Studio 2022 (with .NET MAUI workload installed).
2.Select target platform (Windows, Android, iOS, macOS).
3.Build and run the client project.

## Client Code Overview (.NET MAUI)

Networking: Uses System.Net.Sockets.UdpClient for UDP communication.
Audio: Utilizes platform-specific APIs or Xamarin.Essentials for audio capture and playback.
Heartbeat: Sends periodic heartbeat messages to server's heartbeat port.
Control: Listens to/control messages on control port.
UI: Minimalistic UI for connecting, speaking, and displaying connection status.

## Usage:

Start the server executable.
Launch the client application.
Configure the server IP and ports in the client UI if necessary.
Press "Connect" on the client to join the voice chat.
Speak and listen in real-time.

## Contribution:
Feel free to fork, submit issues or pull requests for new features and bug fixes.

## License:

MIT License
