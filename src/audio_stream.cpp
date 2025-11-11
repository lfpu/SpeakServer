#include "audio_stream.hpp"
#include <chrono>
#include <iostream>

AudioStreamManager::AudioStreamManager(boost::asio::ip::udp::socket &socket)
    : socket_(socket)
{
}
void AudioStreamManager::UpdateClients(std::string key)
{

    auto it = last_active_.find(key);
    if (it != last_active_.end())
    {
        it->second = std::chrono::steady_clock::now();
    }
}
std::string AudioStreamManager::extractSpeakTo(const std::vector<char> &data)
{
    if (data.size() < 8)
    {
        return ""; // 数据不足
    }
    // 取前 8 字节
    std::string custom(data.begin(), data.begin() + 8);
    // 去掉末尾的 '\0' 填充
    size_t pos = custom.find('\0');
    if (pos != std::string::npos)
    {
        custom = custom.substr(0, pos);
    }

    return custom;
}
void AudioStreamManager::receiveAudio(const std::vector<char> &data, std::string key, const udp::endpoint &receiver)
{
    // std::string key = sender.address().to_string() + ":" + std::to_string(sender.port());
    // 输出音频文件
    //  try
    //  {
    //      file.write(data);
    //  }
    //  catch (const std::exception &e)
    //  {
    //      std::cerr << e.what() << '\n';
    //  }

    {
        std::lock_guard<std::mutex> lock(mutex_);
        clients_[key] = receiver;
        last_active_[key] = std::chrono::steady_clock::now();
    }

    forwardAudio(data, key);
}

void AudioStreamManager::forwardAudio(const std::vector<char> &data, const std::string senderid)
{
    std::lock_guard<std::mutex> lock(mutex_);
    std::string user = extractSpeakTo(data);
    std::cerr << "speak to " << user << std::endl;
    std::vector<char> audio(data.begin() + 8, data.end());
    if (user == "all")
    {
        for (const auto &[key, endpoint] : clients_)
        {
            //if (key != senderid)
            //{
                boost::system::error_code ec;
                socket_.send_to(boost::asio::buffer(audio), endpoint, 0, ec);
                if (ec)
                {
                    std::cerr << "Failed to send audio to " << key << ": " << ec.message() << std::endl;
                }
                // else
                // {
                //     std::clog << "Send voice data to :" << endpoint.address() << ", receive port:" << endpoint.port() << std::endl;
                // }
            //}
        }
        // std::cout << "Forword to " << clients_.size() << "success." << std::endl;
    }
    else
    {
        auto it = clients_.find(user);
        if (it != clients_.end())
        {
            boost::system::error_code ec;
            socket_.send_to(boost::asio::buffer(audio), it->second, 0, ec);
            if (ec)
            {
                std::cerr << "Failed to send audio to " << user << ": " << ec.message() << std::endl;
            }
            std::cout << "Forword to  User: " << user << " success." << std::endl;
        }
    }
}

void AudioStreamManager::registerClient(const udp::endpoint &client)
{
    std::string key = client.address().to_string() + ":" + std::to_string(client.port());
    std::lock_guard<std::mutex> lock(mutex_);
    clients_[key] = client;
    last_active_[key] = std::chrono::steady_clock::now();
}

void AudioStreamManager::unregisterInactiveClients()
{
    std::lock_guard<std::mutex> lock(mutex_);
    auto now = std::chrono::steady_clock::now();
    for (auto it = last_active_.begin(); it != last_active_.end();)
    {
        if (std::chrono::duration_cast<std::chrono::seconds>(now - it->second).count() > 4)
        {
            std::cout << "Client " << it->first << " Unactive, removed." << std::endl;
            clients_.erase(it->first);
            it = last_active_.erase(it);
        }
        else
        {
            ++it;
        }
    }
}