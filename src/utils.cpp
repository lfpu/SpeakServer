#include "utils.hpp"
#include <iostream>

std::string getDistinctUserNames(
    const std::unordered_map<std::string, std::shared_ptr<ClientSession>> &client_sessions,const std::string& channel)
{
    std::set<std::string> seenNames; // 用 set 去重

    nlohmann::json j = nlohmann::json::array();

    for (const auto &[key, session] : client_sessions)
    {
        if(session->GetChannel()!=channel) continue;
        if (session && !session->User_Name.empty() && seenNames.insert(session->User_Name).second)
        {
            // 插入 JSON 对象
            j.push_back({{"UserName", session->User_Name},
                         {"State", session->IsSpeaking ? 2 : 1},
                         {"IsSpeaking", session->IsSpeaking},
                         {"IsMuted", session->IsMute}});
        }
    }
    std::string data = j.dump();
    return data;
}

std::string getDistinctChannels(const std::unordered_map<std::string, std::shared_ptr<ClientSession>> &client_sessions)
{
    std::set<std::string> seenChannels;
    nlohmann::json j = nlohmann::json::array();
    for (const auto &pair : client_sessions)
    {
        if (!pair.second->GetChannel().empty() && seenChannels.insert(pair.second->GetChannel()).second)
        {
            j.push_back(pair.second->GetChannel());
        }
    }
    std::string data = j.dump();
    //std::cout << "Distinct channels: " << j.dump() << std::endl;
    return data;
}

std::vector<std::string> splitByChar(const std::string &str, char delimiter)
{
    std::vector<std::string> tokens;
    std::string token;
    std::stringstream ss(str);

    while (std::getline(ss, token, delimiter))
    {
        if (!token.empty())
        { // 忽略空字符串
            tokens.push_back(token);
        }
    }
    return tokens;
}