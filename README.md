# SpeakServer - UDP Voice Communication Server

A lightweight UDP server implementation for walkie-talkie style voice communication. Built with C++ and Boost.Asio for efficient network handling.

## Features

- UDP-based real-time voice transmission 
- Support for multiple concurrent clients
- Client heartbeat monitoring
- Configurable server settings
- Auto cleanup of inactive sessions
- Control message system for client management

## Prerequisites

- C++17 compatible compiler
- Boost libraries
- CMake 3.10 or higher
- Visual Studio 2022 (for Windows builds)

## Configuration

Server settings can be configured in [`config/server_config.json`](config/server_config.json):

```json
{
  "server": {
    "Port": 9000,           // Main server port
    "heartbeat_Port": 9003, // Heartbeat monitoring port
    "MaxConnections": 100   // Maximum concurrent connections
  },
  "client": {
    "RecievePort": 9001,    // Client audio receive port
    "ControlPort": 9002     // Client control message port
  }
}
```
## Build Server

```cmake

mkdir build
cd build
cmake ..
cmake --build . --config Release