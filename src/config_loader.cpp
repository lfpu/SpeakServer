#include "config_loader.hpp"
#include <fstream>
#include <iostream>
#include <nlohmann/json.hpp>

using json = nlohmann::json;

ServerConfig loadServerConfig(const std::string& filepath) {
    ServerConfig config{9000,9003, 100,9001,9002}; // 默认值

    try {
        std::ifstream file(filepath);
        if (!file.is_open()) {
            std::cerr << "Failed to open config file: " << filepath << std::endl;
            return config;
        }

        json j;
        file >> j;

        config.port = j["server"]["Port"].get<unsigned short>();
        config.heartbeat_port = j["server"]["heartbeat_Port"].get<unsigned short>();
        config.max_connections = j["server"]["MaxConnections"].get<int>();
        config.client_recieve_port = j["client"]["RecievePort"].get<unsigned short>();
        config.client_control_port = j["client"]["ControlPort"].get<unsigned short>();
    } catch (const std::exception& e) {
        std::cerr << "Error parsing config: " << e.what() << std::endl;
    }

    return config;
}