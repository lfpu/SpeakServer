#include "utils.hpp"
#include <iostream>

boost::asio::const_buffer getDistinctUserNamesJson(
    const std::unordered_map<std::string, std::shared_ptr<ClientSession>> &client_sessions)
{
    std::set<std::string> seenNames; // 用 set 去重

    nlohmann::json j = nlohmann::json::array();

    for (const auto &[key, session] : client_sessions)
    {
        if (session && !session->User_Name.empty() && seenNames.insert(session->User_Name).second)
        {
            // 插入 JSON 对象
            j.push_back({{"UserName", session->User_Name},
                         {"State", session->IsSpeaking ? 2 : 1},
                         {"IsSpeaking",session->IsSpeaking},
                         {"IsMuted",session->IsMute}
                        });
        }
    }
    return boost::asio::buffer(j.dump());
}