#pragma once

#include <string>
#include <boost/asio.hpp>
#include <nlohmann/json.hpp>

using boost::asio::ip::udp;

class ControlMessageSender {
public:
    ControlMessageSender(boost::asio::ip::udp::socket& socket);

    void sendReject(const udp::endpoint& client, const std::string& reason);
    void sendMute(const udp::endpoint& client,const std::string& reason);
    void sendJoinChannel(const udp::endpoint& client, const std::string& channel, bool success);
    void sendCustom(const udp::endpoint& client, const nlohmann::json& message);

private:
    boost::asio::ip::udp::socket& socket_;
};