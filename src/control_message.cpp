#include "control_message.hpp"
#include <iostream>

ControlMessageSender::ControlMessageSender(boost::asio::ip::udp::socket& socket)
    : socket_(socket) {}

void ControlMessageSender::sendReject(const udp::endpoint& client, const std::string& reason) {
    nlohmann::json msg = {
        {"type", "control"},
        {"action", "reject"},
        {"reason", reason}
    };
    sendCustom(client, msg);
}

void ControlMessageSender::sendMute(const udp::endpoint& client,const std::string& reason) {
    nlohmann::json msg = {
        {"type", "control"},
        {"action", "mute"},
        {"reason", reason}
    };
    sendCustom(client, msg);
}

void ControlMessageSender::sendJoinChannel(const udp::endpoint& client, const std::string& channel, bool success) {
    nlohmann::json msg = {
        {"type", "control"},
        {"action", "join_channel"},
        {"channel", channel},
        {"status", success ? "success" : "failed"}
    };
    sendCustom(client, msg);
}

void ControlMessageSender::sendCustom(const udp::endpoint& client, const nlohmann::json& message) {
    std::string serialized = message.dump();
    boost::system::error_code ec;
    socket_.send_to(boost::asio::buffer(serialized), client, 0, ec);
    if (ec) {
        std::cerr << "Failed to send control message: " << ec.message() << std::endl;
    }
}