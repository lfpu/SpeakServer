#pragma once

#include <string>

struct ServerConfig {
    unsigned short port;
    unsigned short heartbeat_port;
    int max_connections;
    unsigned short client_recieve_port;
    unsigned short client_control_port;
};

ServerConfig loadServerConfig(const std::string& filepath);