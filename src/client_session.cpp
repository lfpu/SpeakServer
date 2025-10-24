#include "client_session.hpp"
#include <chrono>

ClientSession::ClientSession(const udp::endpoint &endpoint)
    : endpoint_(endpoint),
      last_active_(std::chrono::steady_clock::now())
{
    id_ = endpoint_.address().to_string() + ":" + std::to_string(endpoint_.port());
}

udp::endpoint ClientSession::getEndpoint() const
{
    std::lock_guard<std::mutex> lock(mutex_);
    return endpoint_;
}

std::string ClientSession::getId() const
{
    std::lock_guard<std::mutex> lock(mutex_);
    return id_;
}

void ClientSession::updateLastActive()
{
    std::lock_guard<std::mutex> lock(mutex_);
    last_active_ = std::chrono::steady_clock::now();
}

bool ClientSession::isActive() const
{
    std::lock_guard<std::mutex> lock(mutex_);
    auto now = std::chrono::steady_clock::now();
    return std::chrono::duration_cast<std::chrono::seconds>(now - last_active_).count() < 10;
}

void ClientSession::SetUserName(const std::string &name)
{
    std::lock_guard<std::mutex> lock(mutex_);
    User_Name = name;
}

void ClientSession::updateHeartbeat()
{
    last_active_ = std::chrono::steady_clock::now();
}

bool ClientSession::isTimedOut(std::chrono::steady_clock::time_point now, std::chrono::seconds timeout) const
{
    return (now - last_active_) > timeout;
}
