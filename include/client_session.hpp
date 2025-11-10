#pragma once

#include <boost/asio.hpp>
#include <vector>
#include <string>
#include <mutex>

using boost::asio::ip::udp;

class ClientSession {
public:
    ClientSession(const udp::endpoint& endpoint);
    /// @brief 获取客户端的UDP端点
    /// @return 端点信息
    udp::endpoint getEndpoint() const;
    /// @brief 获取客户端的唯一标识ID
    /// @return 客户端ID
    std::string getId() const;
    /// @brief 更新最后活跃时间
    void updateLastActive();
    /// @brief 查询用户是否活跃
    /// @return 
    bool isActive() const;

    void SetUserName(const std::string& name);
    void SetRecievePoint(const unsigned short target_port);
    void updateHeartbeat();
    void updateLastSpeak();

    void isTimedOut();

    std::string User_Name;
    udp::endpoint recievePoint;
    bool IsSpeaking =false;
    bool IsMute =false;
private:
    udp::endpoint endpoint_;
    std::string id_;
    std::chrono::steady_clock::time_point last_active_;
    std::chrono::steady_clock::time_point last_speak;
    mutable std::mutex mutex_;

};