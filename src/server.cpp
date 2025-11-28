#include "server.hpp"
#include <iostream>
#include "control_message.hpp"

VoiceServer::VoiceServer(boost::asio::io_context &io_context, ServerConfig config)
    : io_context_(io_context),
      socket_(io_context, udp::endpoint(udp::v4(), config.port)),
      recv_buffer_(1024),
      running_(false),
      audio_stream_manager_(socket_),
      config_(config),
      pool_(4) // 线程池大小
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

                dispatchMessage(client_id, data);
                // 线程池
                // boost::asio::post(pool_, [this, client_id, data = std::move(data)]() mutable
                //                   { dispatchMessage(client_id, std::move(data)); });
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
// ===================== 消息解析 =====================
MessageType VoiceServer::parseMessageTypes(const std::vector<char> &data)
{
    std::string_view msg(data.data(), data.size());
    if (msg._Starts_with("handshake"))
        return MessageType::Handshake;
    if (msg._Starts_with("heartbeat"))
        return MessageType::Heartbeat;
    if(msg._Starts_with("new_channel"))
        return MessageType::NewChannel;
    if (msg == "GetUsers")
        return MessageType::GetUsers;
    if (msg == "get_channels")
        return MessageType::GetChannels;
    if (msg == "receive")
        return MessageType::Receive;
    if (data[0] == 0x001)
        return MessageType::SpeakingStart;
    if (data[0] == 0x002)
        return MessageType::SpeakingStop;
    return MessageType::Audio;
}
void VoiceServer::dispatchMessage(const std::string &client_id, const std::vector<char> &data)
{
    MessageType type = parseMessageTypes(data);
    std::string_view msg(data.data(), data.size());

    switch (type)
    {
    case MessageType::Handshake:
        handleHandshake(client_id, msg);
        break;
    case MessageType::Heartbeat:
        handleHeartbeat(client_id, msg);
        break;
    case MessageType::NewChannel:
        handleNewChannel(client_id, msg);
        break;
    case MessageType::GetUsers:
        handleGetUsers(client_id);
        break;
    case MessageType::Receive:
        handleReceive(client_id);
        break;
    case MessageType::SpeakingStart:
        setSpeaking(client_id, true);
        break;
    case MessageType::GetChannels:
        handleGetChannels(client_id);
        break;
    case MessageType::SpeakingStop:
        setSpeaking(client_id, false);
        break;
    case MessageType::Audio:
        handleAudio(client_id, data);
        break;
    default:
        break;
    }
}

void VoiceServer::handleHandshake(const std::string &client_id, std::string_view msg)
{
    // TODO: 注册客户端逻辑
    auto parts = splitByChar(std::string(msg.substr(10)), ':');
    int recievePorint = std::stoi(std::string(parts[0])); // 提取接收端口号
    std::string username = parts.size() > 1 ? parts[1] : "Unknown"; // 提取用户名
    //查询是否已存在该username的session

    auto username_it = std::find_if(client_sessions_.begin(), client_sessions_.end(),
                                    [&username](const auto &pair)
                                    { return pair.second->User_Name == username; });
    if (username_it != client_sessions_.end())
    {
        std::cerr << "Username already taken: " << username << std::endl;
        // 通知客户端用户名已被占用
        ControlMessageSender control_sender(socket_);
        control_sender.sendReject(remote_endpoint_, "Username already taken. Choose another one.");
        return; // Skip adding this client
    }
    auto it = client_sessions_.find(client_id);
    if (it == client_sessions_.end())
    {
        if (client_sessions_.size() >= static_cast<size_t>(config_.max_connections))
        {
            std::cerr << "Max connections reached. Rejecting client: " << client_id << std::endl;

            // 通知客户端连接已满
            ControlMessageSender control_sender(socket_);
            control_sender.sendReject(remote_endpoint_, "Server is full. Try again later.");
            return; // Skip adding this client
        }
        auto session = std::make_shared<ClientSession>(remote_endpoint_);
        session->SetRecievePoint(recievePorint);
        client_sessions_[client_id] = session;
        socket_.send_to(boost::asio::buffer("copy"), session->recievePoint);
    }
    else
    {
        it->second->updateLastActive();
        it->second->SetRecievePoint(recievePorint);
        socket_.send_to(boost::asio::buffer("copy"), it->second->recievePoint);
    }
    std::cout << "New client connected: " << client_id << std::endl;
    std::cout << "Total connected clients: " << client_sessions_.size() << " / 100" << std::endl;
    std::string reply = "handshake_ack";
    socket_.send_to(boost::asio::buffer(reply), remote_endpoint_);
    std::cout << "Handshake received from " << client_id << ",  sent ack. Recieve Port: " << recievePorint << std::endl;
}

void VoiceServer::handleHeartbeat(const std::string &client_id, std::string_view msg)
{
    auto parts = splitByChar(std::string(msg.substr(10)), ':');
    std::string username = parts[0];                               // 提取用户名
    std::string channel = parts.size() > 1 ? parts[1] : "Lobby"; // 提取频道名，默认为"Lobby"
    auto it = client_sessions_.find(client_id);
    if (it != client_sessions_.end())
    {
        it->second->updateHeartbeat();
        it->second->updateLastActive();
        it->second->SetUserName(username);
        it->second->SetChannel(channel);
        audio_stream_manager_.UpdateClients(it->second->User_Name);
        // std::cout << "Heartbeat received from User:" << username << "," << client_id << std::endl;
        //  socket_.send_to(boost::asio::buffer("copy"),remote_endpoint_);
    }
    else
    {
        auto session = std::make_shared<ClientSession>(remote_endpoint_);
        client_sessions_[client_id] = session;
        session->SetUserName(username);
        std::cout << "New client connected via heartbeat: User:" << username << "," << client_id << std::endl;
    }
}
void VoiceServer::handleNewChannel(const std::string &client_id, std::string_view msg)
{
    auto parts = splitByChar(std::string(msg), ':');
    if (parts.empty())
        return;
    std::string new_channel = parts[1]; // 提取新频道名
    auto it = client_sessions_.find(client_id);
    if (it != client_sessions_.end())
    {
        it->second->SetChannel(new_channel);
        std::cout << "Client " << client_id << " switched to channel: " << new_channel << std::endl;
    }
}
void VoiceServer::handleGetUsers(const std::string &client_id)
{
    // TODO: 返回同频道用户列表
    auto it = client_sessions_.find(client_id);
    if (it == client_sessions_.end())
        return;
    std::string data = getDistinctUserNames(client_sessions_, it->second->GetChannel());
    //std::cout << "Users :" << data << std::endl;
    socket_.send_to(boost::asio::buffer(data) , remote_endpoint_);
}
void VoiceServer::handleGetChannels(const std::string &client_id)
{
    std::string data = getDistinctChannels(client_sessions_);
    socket_.send_to(boost::asio::buffer(data), remote_endpoint_);
}
void VoiceServer::handleReceive(const std::string &client_id)
{
    // TODO: 发送音频数据
    std::cout << "Get Client receive" << std::endl;
    auto it = client_sessions_.find(client_id);
    if (it != client_sessions_.end())
    {
        socket_.send_to(boost::asio::buffer("copy"), it->second->recievePoint);
    }
}

void VoiceServer::setSpeaking(const std::string &client_id, bool speaking)
{
    auto it = client_sessions_.find(client_id);
    it->second->IsSpeaking = speaking;
}

void VoiceServer::handleAudio(const std::string &client_id, const std::vector<char> &data)
{
    auto it = client_sessions_.find(client_id);
    if (it == client_sessions_.end())
        return;
    if (it->second->IsMute)
        return;
    audio_stream_manager_.receiveAudio(data, it->second->User_Name, it->second->recievePoint, it->second->GetChannel());
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
std::unordered_map<std::string, std::shared_ptr<ClientSession>> VoiceServer::GetSessions()
{
    return client_sessions_;
}