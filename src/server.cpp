#include "server.hpp"
#include <iostream>
#include "control_message.hpp"

VoiceServer::VoiceServer(boost::asio::io_context &io_context, ServerConfig config)
    : io_context_(io_context),
      socket_(io_context, udp::endpoint(udp::v4(), config.port)),
      recv_buffer_(1024),
      running_(false),
      audio_stream_manager_(socket_),
      config_(config)
{
}

void VoiceServer::start()
{
    running_ = true;
    startReceive(); // 开始异步接收
    std::cout << "Voice server started on port " << socket_.local_endpoint().port()
              << " with max connections: " << config_.max_connections << std::endl;
    // 启动一个单独的线程来清理不活跃的会话
    std::thread([this]()
                {
        while (running_) {
            std::this_thread::sleep_for(std::chrono::seconds(10));
            cleanupInactiveSessions();
            audio_stream_manager_.unregisterInactiveClients();
        } })
        .detach();
}

void VoiceServer::stop()
{
    running_ = false;
    socket_.close();
    std::cout << "Voice server stopped." << std::endl;
}

void VoiceServer::startReceive()
{
    socket_.async_receive_from(
        boost::asio::buffer(recv_buffer_), remote_endpoint_,
        [this](boost::system::error_code error, std::size_t bytes_recvd)
        {
            if (!error && bytes_recvd > 0)
            {
                std::vector<char> data(recv_buffer_.begin(), recv_buffer_.begin() + bytes_recvd);
                std::string client_id = remote_endpoint_.address().to_string() + ":" + std::to_string(remote_endpoint_.port());
                bool res = handshake(remote_endpoint_, data, client_id);
                if (!res)
                {
                    startReceive();
                    return;
                }
            }
            else if (error)
            {
                std::cerr << "Receive error: " << error.message() << std::endl;
            }

            cleanupInactiveSessions();
            audio_stream_manager_.unregisterInactiveClients();

            if (running_)
                startReceive(); // 继续接收
        });
}

void VoiceServer::receiveLoop()
{
    while (running_)
    {
        boost::system::error_code error;
        size_t len = socket_.receive_from(boost::asio::buffer(recv_buffer_), remote_endpoint_, 0, error);
        if (!error && len > 0)
        {
            std::vector<char> data(recv_buffer_.begin(), recv_buffer_.begin() + len);
            std::string client_id = remote_endpoint_.address().to_string() + ":" + std::to_string(remote_endpoint_.port());
            bool res = handshake(remote_endpoint_, data, client_id);
            if (!res)
                continue;
        }
        else if (error)
        {
            std::cerr << "Receive error: " << error.message() << std::endl;
        }

        cleanupInactiveSessions();
        audio_stream_manager_.unregisterInactiveClients();
    }
}
bool VoiceServer::handshake(const udp::endpoint &client_endpoint, const std::vector<char> &data, const std::string &client_id)
{

    // 👇 新增：处理 handshake 消息
    std::string msg(data.begin(), data.end());
    if (msg == "handshake")
    {
        auto it = client_sessions_.find(client_id);
        if (it == client_sessions_.end())
        {
            if (client_sessions_.size() >= static_cast<size_t>(config_.max_connections))
            {
                std::cerr << "Max connections reached. Rejecting client: " << client_id << std::endl;

                // 通知客户端连接已满
                ControlMessageSender control_sender(socket_);
                control_sender.sendReject(remote_endpoint_, "Server is full. Try again later.");
                return false; // Skip adding this client
            }
            auto session = std::make_shared<ClientSession>(remote_endpoint_);
            client_sessions_[client_id] = session;
        }
        else
        {
            it->second->updateLastActive();
        }
        std::cout << "New client connected: " << client_id << std::endl;
        std::cout << "Total connected clients: " << client_sessions_.size() << " / 100" << std::endl;
        std::string reply = "handshake_ack";
        socket_.send_to(boost::asio::buffer(reply), remote_endpoint_);
        std::cout << "Handshake received from " << client_id << ", sent ack." << std::endl;
        return false; // 不处理音频数据
    }
    else if (msg._Starts_with("heartbeat"))
    {
        std::string username = msg.substr(10); // 提取用户名
        auto it = client_sessions_.find(client_id);
        if (it != client_sessions_.end())
        {
            it->second->updateHeartbeat();
            it->second->updateLastActive();
            it->second->SetUserName(username);
            std::cout << "Heartbeat received from User:"<<username<<"," << client_id << std::endl;
        }
        else
        {
            auto session = std::make_shared<ClientSession>(remote_endpoint_);
            client_sessions_[client_id] = session;
            session->SetUserName(username);
            std::cout << "New client connected via heartbeat: User:"<<username<<"," << client_id << std::endl;
        }
        return false; // 不处理音频数据
    }

    // {
    //     std::lock_guard<std::mutex> lock(session_mutex_);
    // }

    audio_stream_manager_.receiveAudio(data, remote_endpoint_, config_.client_recieve_port);
    return true;
}
void VoiceServer::cleanupInactiveSessions()
{
    std::lock_guard<std::mutex> lock(session_mutex_);
    for (auto it = client_sessions_.begin(); it != client_sessions_.end();)
    {
        if (!it->second->isActive())
        {
            std::cout << "Removing inactive client: " << it->first << std::endl;
            it = client_sessions_.erase(it);
        }
        else
        {
            ++it;
        }
    }
}