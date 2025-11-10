#pragma once

#include <vector>
#include <unordered_map>
#include <mutex>
#include <boost/asio.hpp>
#include "audio_File.hpp"
#include "client_session.hpp"

using boost::asio::ip::udp;

class AudioStreamManager {
public:
    AudioStreamManager(boost::asio::ip::udp::socket& socket);

    void UpdateClients( std::string key);

    /// @brief 接收音频数据并更新客户端活跃时间。
    /// @param data 
    /// @param sender 
    void AudioStreamManager::receiveAudio(const std::vector<char> &data, std::string key, const udp::endpoint &receiver);
    /// @brief 转发音频数据到其他客户端。
    /// @param data
    /// @param sender
    void AudioStreamManager::forwardAudio(const std::vector<char> &data, const std::string senderid);
    /// @brief 注册新客户端。
    /// @param client
    void registerClient(const udp::endpoint& client);
    /// @brief 注销不活跃的客户端。
    void unregisterInactiveClients();

private:
    boost::asio::ip::udp::socket& socket_;
    std::unordered_map<std::string, udp::endpoint> clients_;
    std::unordered_map<std::string, std::chrono::steady_clock::time_point> last_active_;
    std::mutex mutex_;

    //AudioFile file;
    std::string extractSpeakTo(const std::vector<char>& data);
};