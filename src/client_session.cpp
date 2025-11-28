#include "client_session.hpp"
#include <chrono>
#include <iostream>
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
    return std::chrono::duration_cast<std::chrono::seconds>(now - last_active_).count() < 2;
}

void ClientSession::SetUserName(const std::string &name)
{
    std::lock_guard<std::mutex> lock(mutex_);
    User_Name = name;
}

void ClientSession::SetRecievePoint(const unsigned short target_port){
    recievePoint= udp::endpoint (endpoint_.address(),boost::asio::ip::port_type(target_port));
}

void ClientSession::updateHeartbeat()
{
    last_active_ = std::chrono::steady_clock::now();
}
void ClientSession::updateLastSpeak(){
    last_speak =std::chrono::steady_clock::now();
    IsSpeaking=true;
}
void ClientSession::isTimedOut()
{
    IsSpeaking=(std::chrono::duration_cast<std::chrono::seconds>(std::chrono::steady_clock::now() - last_speak).count() < 2);
}

void ClientSession::SetChannel(const std::string &channel) {
    std::lock_guard<std::mutex> lock(mutex_);
    ChannelId = channel;
}
std::string ClientSession::GetChannel() const {
    std::lock_guard<std::mutex> lock(mutex_);
    return ChannelId;
}
