#pragma once

#include <unordered_map>
#include <memory>
#include <thread>
#include <vector>
#include <mutex>
#include <boost/asio.hpp>
#include "audio_stream.hpp"
#include "client_session.hpp"
#include "config_loader.hpp"

using boost::asio::ip::udp;

class VoiceServer {
public:
    VoiceServer(boost::asio::io_context& io_context,ServerConfig config);
    void start();
    void stop();
    std::unordered_map<std::string, std::shared_ptr<ClientSession>> GetSessions();
private:



    void startReceive();
    bool handshake(const udp::endpoint& client_endpoint, const std::vector<char>& data, const std::string& client_id);
    void cleanupInactiveSessions();

    boost::asio::io_context& io_context_;
    udp::socket socket_;
    udp::endpoint remote_endpoint_;
    std::vector<char> recv_buffer_;
    bool running_;
    std::mutex session_mutex_;
    std::unordered_map<std::string, std::shared_ptr<ClientSession>> client_sessions_;
    AudioStreamManager audio_stream_manager_;
    ServerConfig config_;

};