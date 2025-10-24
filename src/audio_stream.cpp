#include "audio_stream.hpp"
#include <chrono>
#include <iostream>

AudioStreamManager::AudioStreamManager(boost::asio::ip::udp::socket& socket)
    : socket_(socket) {}

void AudioStreamManager::receiveAudio(const std::vector<char>& data, const udp::endpoint& sender, const unsigned short target_port) {
    std::string key = sender.address().to_string() + ":" + std::to_string(sender.port());

    {
        std::lock_guard<std::mutex> lock(mutex_);
        clients_[key] = sender;
        last_active_[key] = std::chrono::steady_clock::now();
    }

    forwardAudio(data, sender, target_port);
}

void AudioStreamManager::forwardAudio(const std::vector<char>& data, const udp::endpoint& sender, const unsigned short target_port) {
    std::lock_guard<std::mutex> lock(mutex_);
    for (const auto& [key, endpoint] : clients_) {
        if (endpoint != sender) {
            //客户端的音频接受端口固定为9001
            udp::endpoint target_endpoint(endpoint.address(), target_port);
            boost::system::error_code ec;
            socket_.send_to(boost::asio::buffer(data), target_endpoint, 0, ec);
            if (ec) {
                std::cerr << "Failed to send audio to " << key << ": " << ec.message() << std::endl;
            }
        }
    }
    std::cout << "Forwarded audio from " << sender.address().to_string() << ":" << sender.port()
              << " to " << clients_.size() << " clients." << std::endl;
}

void AudioStreamManager::registerClient(const udp::endpoint& client) {
    std::string key = client.address().to_string() + ":" + std::to_string(client.port());
    std::lock_guard<std::mutex> lock(mutex_);
    clients_[key] = client;
    last_active_[key] = std::chrono::steady_clock::now();
}

void AudioStreamManager::unregisterInactiveClients() {
    std::lock_guard<std::mutex> lock(mutex_);
    auto now = std::chrono::steady_clock::now();
    for (auto it = last_active_.begin(); it != last_active_.end(); ) {
        if (std::chrono::duration_cast<std::chrono::seconds>(now - it->second).count() > 30) {
            clients_.erase(it->first);
            it = last_active_.erase(it);
        } else {
            ++it;
        }
    }
}