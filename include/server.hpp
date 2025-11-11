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
#include "utils.hpp"

using boost::asio::ip::udp;
// ===================== 消息类型枚举 =====================
enum class MessageType
{
    Handshake,
    Heartbeat,
    GetUsers,
    Receive,
    SpeakingStart,
    SpeakingStop,
    Audio,
    Unknown
};

class VoiceServer
{
public:
    VoiceServer(boost::asio::io_context &io_context, ServerConfig config);
    void start();
    void stop();
    std::unordered_map<std::string, std::shared_ptr<ClientSession>> GetSessions();

private:
    void startReceive();
    void cleanupInactiveSessions();

    void handleHandshake(const std::string &client_id, std::string_view msg);
    void handleHeartbeat(const std::string &client_id, std::string_view msg);
    void handleGetUsers(const std::string &client_id);
    void handleReceive(const std::string &client_id);
    void setSpeaking(const std::string &client_id, bool speaking);
    void handleAudio(const std::string &client_id, const std::vector<char> &data);
    void dispatchMessage(const std::string &client_id, const std::vector<char> &data);
    //MessageType test(const std::vector<char> &data);
    MessageType parseMessageTypes(const std::vector<char> &data);

    boost::asio::io_context &io_context_;
    udp::socket socket_;
    udp::endpoint remote_endpoint_;
    std::vector<char> recv_buffer_;
    bool running_;
    std::mutex session_mutex_;
    std::unordered_map<std::string, std::shared_ptr<ClientSession>> client_sessions_;
    AudioStreamManager audio_stream_manager_;
    ServerConfig config_;

    boost::asio::thread_pool pool_;
};



